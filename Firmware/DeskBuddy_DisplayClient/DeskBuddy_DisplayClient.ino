#include <WiFi.h>
#include <Wire.h>
#include <U8g2lib.h>
#include <DeskBuddyKoreanFont.h>
#include <WebSocketsClient.h>
#include <ArduinoJson.h>
#include <time.h>
#include "Secrets.h"
#include "ServerRootCA.h"

// Pins remain the same as the working display test.
U8G2_SSD1306_128X64_NONAME_F_HW_I2C oled(U8G2_R0, U8X8_PIN_NONE, 22, 21);
WebSocketsClient socketClient;
bool displayReady = false;
bool socketStarted = false;
bool socketConnected = false;
bool needsRedraw = true;
bool lastWifiConnected = false;
char authHeader[160];

// At most 600 Korean characters (3 UTF-8 bytes each), wrapped once on receipt.
constexpr size_t MAX_REPLY_BYTES = 2400;
constexpr size_t MAX_LINES = 150;
constexpr uint8_t LINES_PER_PAGE = 3;
constexpr uint32_t PAGE_INTERVAL_MS = 6000;
String replyText;
uint16_t lineStarts[MAX_LINES];
uint16_t lineEnds[MAX_LINES];
uint16_t lineCount = 0;
uint16_t page = 0;
uint32_t lastPageAt = 0;
uint32_t lastWifiAttempt = 0;
uint32_t lastClockNotice = 0;

// Step over complete UTF-8 code points so Korean is never split mid-byte.
size_t nextCodePoint(const String &text, size_t position) {
  uint8_t first = static_cast<uint8_t>(text[position]);
  size_t count = first < 0x80 ? 1 : (first < 0xE0 ? 2 : (first < 0xF0 ? 3 : 4));
  return min(position + count, text.length());
}

void wrapReply() {
  lineCount = 0;
  size_t position = 0;
  oled.setFont(u8g2_font_unifont_t_korean_NanumGothicCoding_16);
  while (position < replyText.length() && lineCount < MAX_LINES) {
    size_t start = position;
    size_t end = position;
    while (position < replyText.length()) {
      if (replyText[position] == '\n') {
        end = position++;
        break;
      }
      size_t next = nextCodePoint(replyText, position);
      String candidate = replyText.substring(start, next);
      if (oled.getUTF8Width(candidate.c_str()) > 128 && position > start) break;
      end = position = next;
    }
    lineStarts[lineCount] = start;
    lineEnds[lineCount++] = end;
  }
  page = 0;
  lastPageAt = millis();
  needsRedraw = true;
}

void receiveEvent(WStype_t type, uint8_t *payload, size_t length) {
  switch (type) {
    case WStype_CONNECTED:
      socketConnected = true;
      needsRedraw = true;
      Serial.println("WebSocket connected.");
      break;
    case WStype_DISCONNECTED:
      socketConnected = false;
      needsRedraw = true;
      Serial.println("WebSocket disconnected; reconnecting automatically.");
      break;
    case WStype_TEXT: {
      // Display events omit URLs and fit within this limit, including JSON escapes.
      if (length > 8192) return;
      JsonDocument event;
      if (deserializeJson(event, payload, length)) {
        Serial.println("Invalid WebSocket JSON.");
        return;
      }
      if (strcmp(event["type"] | "", "chat.answer") != 0) return;
      const char *text = event["reply"] | "";
      size_t size = strlen(text);
      if (size == 0 || size > MAX_REPLY_BYTES) return;
      replyText = text;
      replyText.replace("\r", "");
      replyText.replace("\t", " ");
      wrapReply();
      Serial.printf("Answer received: %u bytes, %u lines.\n", static_cast<unsigned>(size), lineCount);
      break;
    }
    default:
      break;
  }
}

void drawScreen() {
  if (!displayReady) return;
  oled.clearBuffer();
  oled.setFontPosTop();
  // A small ASCII status line leaves room for three Korean rows.
  oled.setFont(u8g2_font_5x7_tf);
  const char *status = WiFi.status() != WL_CONNECTED ? "WiFi..." : (socketConnected ? "ONLINE" : "Server...");
  oled.drawStr(0, 0, status);
  uint16_t pages = max<uint16_t>(1, (lineCount + LINES_PER_PAGE - 1) / LINES_PER_PAGE);
  char counter[16];
  snprintf(counter, sizeof(counter), "%u/%u", page + 1, pages);
  oled.drawStr(98, 0, counter);
  oled.setFont(u8g2_font_unifont_t_korean_NanumGothicCoding_16);
  if (lineCount == 0) {
    oled.drawUTF8(0, 13, "답변 대기 중");
    oled.drawUTF8(0, 30, "질문을 보내요");
  } else {
    for (uint8_t row = 0; row < LINES_PER_PAGE; ++row) {
      uint16_t index = page * LINES_PER_PAGE + row;
      if (index >= lineCount) break;
      String line = replyText.substring(lineStarts[index], lineEnds[index]);
      oled.drawUTF8(0, 10 + row * 18, line.c_str());
    }
  }
  oled.sendBuffer();
  needsRedraw = false;
}

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
  if (!displayReady) {
    Serial.println("OLED not found. Check GND/3V3/SDA21/SCL22.");
    return;
  }
  oled.setBusClock(100000);
  oled.begin();
  replyText.reserve(MAX_REPLY_BYTES);
  drawScreen();
  WiFi.persistent(false);
  WiFi.mode(WIFI_STA);
  WiFi.setAutoReconnect(true);
  WiFi.begin(WIFI_SSID, WIFI_PASSWORD);
  // Certificate validation needs a correct clock. Never disable TLS verification.
  configTime(0, 0, "time.google.com", "pool.ntp.org", "time.cloudflare.com");
  // The WebSockets library appends CRLF itself; an extra blank line corrupts the first frame.
  snprintf(authHeader, sizeof(authHeader), "X-Device-Key: %s", DEVICE_API_KEY);
  socketClient.onEvent(receiveEvent);
  socketClient.setReconnectInterval(5000);
  socketClient.enableHeartbeat(20000, 5000, 2);
  lastWifiAttempt = millis();
  Serial.println("Connecting WiFi; display replies will arrive over WSS.");
}

void loop() {
  if (!displayReady) { delay(100); return; }
  const uint32_t now = millis();
  const bool wifiConnected = WiFi.status() == WL_CONNECTED;
  if (wifiConnected != lastWifiConnected) {
    needsRedraw = true;
    lastWifiConnected = wifiConnected;
    if (wifiConnected) Serial.printf("WiFi connected. IP: %s\n", WiFi.localIP().toString().c_str());
    else { socketConnected = false; Serial.println("WiFi disconnected."); }
  }
  if (!wifiConnected && now - lastWifiAttempt >= 15000) {
    lastWifiAttempt = now;
    WiFi.reconnect();
  }
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
  if (lineCount > LINES_PER_PAGE && now - lastPageAt >= PAGE_INTERVAL_MS) {
    page = (page + 1) % ((lineCount + LINES_PER_PAGE - 1) / LINES_PER_PAGE);
    lastPageAt = now;
    needsRedraw = true;
  }
  if (needsRedraw) drawScreen();
  delay(2); // Yield to the network; no constant screen refresh or blocking animation.
}
