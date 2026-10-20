#include <WiFi.h>
#include <Wire.h>
#include <U8g2lib.h>
#include <DeskBuddyCompactFont.h>
#include <WebSocketsClient.h>
#include <ArduinoJson.h>
#include <time.h>
#include "Secrets.h"
#include "ServerRootCA.h"

U8G2_SSD1306_128X64_NONAME_F_HW_I2C oled(U8G2_R0, U8X8_PIN_NONE, 22, 21);
WebSocketsClient socketClient;

// Rows 0..50 are reserved for expressions. Text occupies only rows 51..63.
constexpr uint8_t TEXT_TOP = 51;
constexpr uint8_t TEXT_HEIGHT = 13;
constexpr uint16_t MAX_GLYPHS = 600;
constexpr size_t MAX_REPLY_BYTES = 2400;
constexpr uint32_t SCROLL_STEP_MS = 65; // About 15 pixels/second; larger means slower.
constexpr uint32_t START_HOLD_MS = 1200;
constexpr uint8_t REPEAT_COUNT = 2;

struct TickerGlyph { uint16_t code; uint16_t position; };
TickerGlyph glyphs[MAX_GLYPHS];
uint16_t glyphCount = 0;
uint16_t firstVisible = 0;
uint16_t textWidth = 0;
int16_t scrollX = 0;
uint8_t completedPasses = 0;
bool playing = false;
bool holding = false;
bool displayReady = false;
bool socketStarted = false;
bool lastWifiConnected = false;
bool needsRedraw = true;
uint32_t lastScrollAt = 0;
uint32_t holdStartedAt = 0;
uint32_t lastWifiAttempt = 0;
uint32_t lastClockNotice = 0;
char authHeader[160];

// Validate UTF-8 before indexing the component tables. Unsupported code points use '?'.
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

void receiveEvent(WStype_t type, uint8_t *payload, size_t length) {
  if (type == WStype_CONNECTED) Serial.println("WebSocket connected.");
  else if (type == WStype_DISCONNECTED) Serial.println("WebSocket disconnected; reconnecting automatically.");
  else if (type == WStype_TEXT && length <= 8192) {
    // Mutable input enables zero-copy JSON parsing; no duplicated reply String is retained.
    JsonDocument event;
    if (deserializeJson(event, payload, length)) return;
    if (strcmp(event["type"] | "", "chat.answer") != 0) return;
    const char *text = event["reply"] | "";
    const size_t bytes = strlen(text);
    if (bytes && bytes <= MAX_REPLY_BYTES) startReply(text);
  }
}

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
    const char *waiting = "대기중..";
    int16_t x = 0;
    while (*waiting) {
      const uint32_t code = nextCodePoint(waiting);
      drawGlyph(x, code);
      x += deskBuddyGlyphWidth(code);
    }
  }
  // Transfer two 8-row tiles (256 bytes). Upper buffer pixels remain intact for expressions.
  oled.updateDisplayArea(0, 6, 16, 2);
  needsRedraw = false;
}

void advanceTicker(uint32_t now) {
  if (!playing) return;
  if (holding) {
    if (now - holdStartedAt < START_HOLD_MS) return;
    holding = false;
    lastScrollAt = now;
  }
  if (now - lastScrollAt < SCROLL_STEP_MS) return;
  // One pixel per frame avoids jumping over letters after a slow network operation.
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
      Serial.println("Ticker finished; waiting.");
    } else { scrollX = 128; firstVisible = 0; }
  }
  needsRedraw = true;
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
  if (!displayReady) { Serial.println("OLED not found. Check GND/3V3/SDA21/SCL22."); return; }
  oled.setBusClock(100000);
  oled.begin();
  oled.clearBuffer();
  oled.sendBuffer();
  drawTextStrip();
  WiFi.persistent(false);
  WiFi.mode(WIFI_STA);
  WiFi.setAutoReconnect(true);
  WiFi.begin(WIFI_SSID, WIFI_PASSWORD);
  configTime(0, 0, "time.google.com", "pool.ntp.org", "time.cloudflare.com");
  // The library adds CRLF; an extra newline would corrupt the WebSocket stream.
  snprintf(authHeader, sizeof(authHeader), "X-Device-Key: %s", DEVICE_API_KEY);
  socketClient.onEvent(receiveEvent);
  socketClient.setReconnectInterval(5000);
  socketClient.enableHeartbeat(20000, 5000, 2);
  lastWifiAttempt = millis();
  Serial.println("Connecting WiFi; compact Hangul ticker ready.");
}

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
  advanceTicker(millis());
  if (needsRedraw) drawTextStrip();
  delay(2);
}
