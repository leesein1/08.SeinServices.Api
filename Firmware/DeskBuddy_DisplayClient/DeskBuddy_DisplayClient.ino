#include <WiFi.h>
#include <Wire.h>
#include <U8g2lib.h>
#include <DeskBuddyCompactFont.h>
#include <WebSocketsClient.h>
#include <ArduinoJson.h>
#include <time.h>
#include "DeskBuddyEyes.h"
#include "Secrets.h"
#include "ServerRootCA.h"

// 기존 배선: SCL=22, SDA=21. SSD1315 OLED를 SSD1306 호환 모드로 사용한다.
// _F_는 전체 화면 128×64의 1비트 버퍼(1,024바이트)를 RAM에 두는 방식이다.
U8G2_SSD1306_128X64_NONAME_F_HW_I2C oled(U8G2_R0, U8X8_PIN_NONE, 22, 21);
WebSocketsClient socketClient; // 서버에 계속 연결해 답변 이벤트를 받는 클라이언트.

// ── 사용자가 조절할 화면/속도 설정 ──
constexpr uint8_t TEXT_TOP = 51;          // 위 0~50행은 눈, 아래 51~63행은 자막.
constexpr uint8_t TEXT_HEIGHT = 13;       // 조합형 한글의 높이. 화면 아래 약 20%를 차지한다.
constexpr uint16_t MAX_GLYPHS = 600;      // 서버의 답변 최대 길이에 맞춘 글자 수 제한.
constexpr size_t MAX_REPLY_BYTES = 2400;  // UTF-8 원문 길이 제한. 잘못된/과도한 데이터 차단용.
constexpr uint32_t SCROLL_STEP_MS = 65;   // 65ms마다 왼쪽으로 1픽셀. 큰 값일수록 느리다.
constexpr uint32_t START_HOLD_MS = 1200;  // 새 답변의 첫 부분을 잠깐 멈춰 보여주는 시간.
constexpr uint8_t REPEAT_COUNT = 2;       // 답변을 끝까지 스크롤할 횟수.
constexpr uint32_t WAITING_INTERVAL_MS = 1200; // 점 하나당 1.2초, .→..→... 한 바퀴는 3.6초.

DeskBuddyEyes face(oled, TEXT_TOP); // 같은 버퍼의 위쪽에만 눈 애니메이션을 그린다.

// 원문 String을 별도 보관하지 않고 글자 코드와 누적 가로 위치만 저장한다.
struct TickerGlyph {
  uint16_t code;     // Unicode 문자 코드. 한글은 초/중/종성 조합으로 그린다.
  uint16_t position; // 문장 맨 왼쪽부터 이 글자까지의 거리(픽셀).
};
TickerGlyph glyphs[MAX_GLYPHS]; // 최대 600자 × 4바이트. 반복 중 추가 힙 할당을 피한다.
uint16_t glyphCount = 0;       // 현재 답변에 포함된 글자 수.
uint16_t firstVisible = 0;     // 이미 화면 왼쪽으로 지나간 글자는 다시 검사하지 않는다.
uint16_t textWidth = 0;        // 답변 전체의 가로 길이(픽셀).
int16_t scrollX = 0;           // 문장의 시작 X좌표. 왼쪽으로 흘러갈 때 음수가 된다.
uint8_t completedPasses = 0;   // 현재까지 끝까지 재생한 횟수.
bool playing = false;         // true: 답변 재생, false: 대기 문구 표시.
bool holding = false;         // 새 답변을 처음 받았을 때의 1.2초 정지 상태.
bool displayReady = false;    // I2C에서 OLED를 발견하고 초기화했는지 여부.
bool socketStarted = false;   // NTP 동기화 후 보안 소켓 설정을 한 번 시작했는지 여부.
bool lastWifiConnected = false; // Wi-Fi 상태 변화를 시리얼에 한 번씩 출력하기 위한 이전 값.
bool needsRedraw = true;      // 아래 자막을 다시 그려야 하는지 여부.
bool frameDirty = false;      // 눈 또는 자막이 버퍼를 바꿨는지 여부. 전송을 한곳에서 모은다.
uint8_t waitingDots = 1;      // 대기 문구 뒤 점 개수: 1→2→3→1.
uint32_t lastWaitingAt = 0;   // 대기 점을 마지막으로 바꾼 시각(ms).
uint32_t lastScrollAt = 0;    // 자막을 마지막으로 1픽셀 움직인 시각(ms).
uint32_t holdStartedAt = 0;   // 새 답변의 시작 정지 구간이 시작된 시각(ms).
uint32_t lastWifiAttempt = 0; // Wi-Fi 재연결 요청 시각. 15초보다 자주 반복하지 않는다.
uint32_t lastClockNotice = 0; // NTP 대기 안내의 마지막 출력 시각.
char authHeader[160];        // 장치 인증 헤더. 키 값은 Secrets.h에서만 읽고 로그에는 출력하지 않는다.
uint8_t sentFrame[1024] = {}; // OLED로 마지막 전송한 화면. 현재 버퍼와 비교해 바뀐 타일만 보낸다.

// UTF-8을 한 글자씩 읽는다. 조합형 폰트 표를 안전하게 조회하도록 범위를 검사하며,
// 잘못된 바이트/지원하지 않는 보조 평면 문자(이모지 등)는 '?'로 처리한다.
uint32_t nextCodePoint(const char *&cursor) {
  const uint8_t first = static_cast<uint8_t>(*cursor++);
  if (first < 0x80) return first;
  uint8_t remaining;
  uint32_t code, minimum;
  if (first >= 0xC2 && first <= 0xDF) { remaining = 1; code = first & 0x1F; minimum = 0x80; }
  else if (first >= 0xE0 && first <= 0xEF) { remaining = 2; code = first & 0x0F; minimum = 0x800; }
  else if (first >= 0xF0 && first <= 0xF4) { remaining = 3; code = first & 0x07; minimum = 0x10000; }
  else return '?';
  for (uint8_t index = 0; index < remaining; ++index) {
    const uint8_t next = static_cast<uint8_t>(*cursor);
    if ((next & 0xC0) != 0x80) return '?';
    code = (code << 6) | (next & 0x3F);
    ++cursor;
  }
  if (code < minimum || code > 0xFFFF || (code >= 0xD800 && code <= 0xDFFF)) return '?';
  return code;
}

// 답변을 처음 받았을 때만 글자 위치를 계산한다. 새 답변은 이전 답변을 즉시 교체한다.
void startReply(const char *text) {
  glyphCount = textWidth = 0;
  firstVisible = 0;
  while (*text && glyphCount < MAX_GLYPHS) {
    uint32_t code = nextCodePoint(text);
    if (code == '\r') continue;
    if (code == '\n' || code == '\t') code = ' ';
    glyphs[glyphCount++] = {static_cast<uint16_t>(code), textWidth};
    textWidth += deskBuddyGlyphWidth(code);
  }
  scrollX = 0;
  completedPasses = 0;
  playing = glyphCount > 0;
  holding = playing;
  holdStartedAt = lastScrollAt = millis();
  needsRedraw = true;
  Serial.printf("Answer received: %u glyphs, %u pixels; ticker starts.\n", glyphCount, textWidth);
}

// WebSocket 콜백. 통신 자체는 라이브러리가 처리하고 답변 이벤트만 화면에 전달한다.
void receiveEvent(WStype_t type, uint8_t *payload, size_t length) {
  if (type == WStype_CONNECTED) Serial.println("WebSocket connected.");
  else if (type == WStype_DISCONNECTED) Serial.println("WebSocket disconnected; reconnecting automatically.");
  else if (type == WStype_TEXT && length <= 8192) {
    // 수정 가능한 수신 버퍼를 사용해 문자열을 복제하지 않고 JSON을 해석한다.
    // 콜백이 끝나기 전에 글자 위치를 저장하므로 수신 버퍼 포인터를 오래 보관하지 않는다.
    JsonDocument event;
    if (deserializeJson(event, payload, length)) return;
    if (strcmp(event["type"] | "", "chat.answer") != 0) return;
    const char *text = event["reply"] | "";
    const size_t bytes = strlen(text);
    if (bytes && bytes <= MAX_REPLY_BYTES) {
      startReply(text);
      face.setEmotion(event["emotion"] | "neutral"); // 서버의 감정에 맞춰 눈 모양을 바꾼다.
    }
  }
}

// 12×13 한글 또는 6×13 영문을 자막 영역에만 그린다. 화면 밖 X좌표는 잘라낸다.
void drawGlyph(int16_t x, uint32_t code) {
  uint8_t bitmap[20];
  const uint8_t width = deskBuddyGlyph(code, bitmap);
  for (uint8_t y = 0; y < TEXT_HEIGHT; ++y) {
    for (uint8_t column = 0; column < width; ++column) {
      const int16_t screenX = x + column;
      if (screenX < 0 || screenX >= 128) continue;
      const uint16_t bit = y * width + column;
      if (bitmap[bit / 8] & (0x80 >> (bit % 8))) oled.drawPixel(screenX, TEXT_TOP + y);
    }
  }
}

// 아래 13행만 지운 뒤 자막을 그린다. 위쪽 눈 그림과 원문 재생 상태는 건드리지 않는다.
void drawTextStrip() {
  oled.setDrawColor(0);
  oled.drawBox(0, TEXT_TOP, 128, TEXT_HEIGHT);
  oled.setDrawColor(1);
  if (playing) {
    for (uint16_t index = firstVisible; index < glyphCount; ++index) {
      const int16_t x = scrollX + glyphs[index].position;
      if (x >= 128) break;
      if (x + deskBuddyGlyphWidth(glyphs[index].code) > 0) drawGlyph(x, glyphs[index].code);
    }
  } else {
    const char *waitingTexts[] = {"대기중.", "대기중..", "대기중..."};
    const char *waiting = waitingTexts[waitingDots - 1];
    int16_t x = 0;
    while (*waiting) {
      const uint32_t code = nextCodePoint(waiting);
      drawGlyph(x, code);
      x += deskBuddyGlyphWidth(code);
    }
  }
  // 실제 전송은 눈 그림까지 완성한 후 flushChangedTiles()에서 수행한다.
  // 48~55행은 같은 타일이라 눈/글씨를 따로 전송하면 경계가 덮일 수 있다.
  frameDirty = true;
  needsRedraw = false;
}

// SSD1306의 전송 단위는 8×8 타일(8바이트)이다. 1KB를 비교하고 변경된 타일만 보낸다.
// 가만히 있는 눈은 전송하지 않고, 움직일 때도 대부분 눈 가장자리만 전송해 I2C 부담을 줄인다.
void flushChangedTiles() {
  const uint8_t *buffer = oled.getBufferPtr();
  for (uint8_t row = 0; row < 8; ++row) {
    uint8_t column = 0;
    while (column < 16) {
      size_t offset = row * 128 + column * 8;
      if (memcmp(buffer + offset, sentFrame + offset, 8) == 0) { ++column; continue; }
      const uint8_t start = column;
      do {
        ++column;
        offset = row * 128 + column * 8;
      } while (column < 16 && memcmp(buffer + offset, sentFrame + offset, 8) != 0);
      const size_t startOffset = row * 128 + start * 8;
      const size_t count = (column - start) * 8;
      oled.updateDisplayArea(start, row, column - start, 1);
      memcpy(sentFrame + startOffset, buffer + startOffset, count);
    }
  }
  frameDirty = false;
}

// millis() 시각 차이로 진행하므로 스크롤을 기다리느라 Wi-Fi나 눈 애니메이션을 멈추지 않는다.
void advanceTicker(uint32_t now) {
  if (!playing) return;
  if (holding) {
    if (now - holdStartedAt < START_HOLD_MS) return;
    holding = false;
    lastScrollAt = now;
  }
  if (now - lastScrollAt < SCROLL_STEP_MS) return;
  // 통신에 시간이 걸려도 글자를 건너뛰지 않고 한 프레임에 1픽셀씩 진행한다.
  lastScrollAt = now;
  --scrollX;
  while (firstVisible < glyphCount && scrollX + glyphs[firstVisible].position + deskBuddyGlyphWidth(glyphs[firstVisible].code) <= 0)
    ++firstVisible;
  if (scrollX + textWidth <= 0) {
    ++completedPasses;
    Serial.printf("Ticker pass %u/%u complete.\n", completedPasses, REPEAT_COUNT);
    if (completedPasses >= REPEAT_COUNT) {
      playing = false;
      glyphCount = textWidth = 0;
      waitingDots = 1;
      lastWaitingAt = now;
      face.setEmotion("neutral"); // 두 번 읽은 뒤 기본 얼굴로 돌아간다. 대기 중에도 깜빡임/시선 이동은 계속된다.
      Serial.println("Ticker finished; waiting.");
    } else { scrollX = 128; firstVisible = 0; }
  }
  needsRedraw = true;
}

// 전원 투입/리셋 때 한 번 실행한다. OLED→눈→Wi-Fi→시각→WSS 순서로 준비한다.
void setup() {
  Serial.begin(115200);
  Wire.begin(21, 22);
  for (uint8_t address : {0x3C, 0x3D}) {
    Wire.beginTransmission(address);
    if (Wire.endTransmission() == 0) {
      oled.setI2CAddress(address << 1);
      displayReady = true;
      break;
    }
  }
  if (!displayReady) { Serial.println("OLED not found. Check GND/3V3/SDA21/SCL22."); return; }
  oled.setBusClock(100000);
  oled.begin();
  oled.clearBuffer();
  oled.sendBuffer();
  face.begin();
  drawTextStrip();
  flushChangedTiles();
  WiFi.persistent(false);
  WiFi.mode(WIFI_STA);
  WiFi.setAutoReconnect(true);
  WiFi.begin(WIFI_SSID, WIFI_PASSWORD);
  configTime(0, 0, "time.google.com", "pool.ntp.org", "time.cloudflare.com");
  // 라이브러리가 줄바꿈을 붙이므로 여기에는 CRLF를 붙이지 않는다(중복되면 연결이 끊긴다).
  snprintf(authHeader, sizeof(authHeader), "X-Device-Key: %s", DEVICE_API_KEY);
  socketClient.onEvent(receiveEvent);
  socketClient.setReconnectInterval(5000);
  socketClient.enableHeartbeat(20000, 5000, 2);
  lastWifiAttempt = millis();
  Serial.println("Connecting WiFi; RoboEyes and compact Hangul ticker ready.");
}

// 소켓/눈/글씨를 짧게 처리하고 마지막에 변경분만 보낸다. 긴 대기 함수는 쓰지 않는다.
void loop() {
  if (!displayReady) { delay(100); return; }
  const uint32_t now = millis();
  const bool wifiConnected = WiFi.status() == WL_CONNECTED;
  if (wifiConnected != lastWifiConnected) {
    lastWifiConnected = wifiConnected;
    Serial.println(wifiConnected ? "WiFi connected." : "WiFi disconnected.");
  }
  if (!wifiConnected && now - lastWifiAttempt >= 15000) { lastWifiAttempt = now; WiFi.reconnect(); }
  if (wifiConnected && !socketStarted) {
    if (time(nullptr) >= 1700000000) {
      socketClient.beginSslWithCA("api.silee.net", 443, "/api/deskbuddy/device/ws", SERVER_ROOT_CA, "");
      socketClient.setExtraHeaders(authHeader);
      socketStarted = true;
    } else if (now - lastClockNotice >= 10000) {
      lastClockNotice = now;
      Serial.println("Waiting for NTP clock before verified TLS connection.");
    }
  }
  if (wifiConnected && socketStarted) socketClient.loop();
  if (face.update(millis(), playing)) frameDirty = true; // 답변 중에는 대기용 표정 변화가 끼어들지 않는다.
  advanceTicker(millis());
  const uint32_t waitingNow = millis();
  if (!playing && waitingNow - lastWaitingAt >= WAITING_INTERVAL_MS) {
    lastWaitingAt = waitingNow;
    waitingDots = waitingDots % 3 + 1;
    needsRedraw = true;
  }
  if (needsRedraw) drawTextStrip();
  if (frameDirty) flushChangedTiles();
  delay(2);
}
