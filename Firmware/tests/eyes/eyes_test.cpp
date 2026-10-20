#include "DeskBuddyEyes.h"

// A no-CRT DLL makes the real C++ renderer testable with the available Windows compiler.
extern "C" void *memset(void *target, int value, size_t size) {
  volatile uint8_t *destination = static_cast<volatile uint8_t *>(target);
  while (size--) *destination++ = static_cast<uint8_t>(value);
  return target;
}
static const char *moods[] = {"neutral", "happy", "sad", "angry", "surprised"};
extern "C" { int _fltused = 0; }
static U8G2 screen;
static void resetScreen() {
  screen.invalidWrites = 0; screen.color = 1;
  for (int y = 0; y < 64; ++y)
    for (int x = 0; x < 128; ++x) screen.pixels[y][x] = 0;
}

extern "C" __declspec(dllexport) int verifyEyes() {
  for (int mood = 0; mood < 5; ++mood) {
    testClock = 0; testRandom = 1;
    resetScreen();
    for (int y = 51; y < 64; ++y)
      for (int x = 0; x < 128; ++x) screen.pixels[y][x] = (x + y) % 2;
    DeskBuddyEyes face(screen, 51);
    face.begin(); face.setEmotion(moods[mood]);
    int minimumCenter = 128, maximumCenter = 0;
    bool sawBlink = false, sawOpen = false, openedDuringStartup = false;
    for (int frame = 0; frame < 1800; ++frame) {
      testClock += 50;
      face.update(testClock);
      int count = 0, weightedX = 0;
      for (int y = 0; y < 51; ++y)
        for (int x = 0; x < 128; ++x) if (screen.pixels[y][x]) { ++count; weightedX += x; }
      if (frame > 20 && count < 100) sawBlink = true;
      if (count > 200) {
        sawOpen = true;
        if (frame < 30) openedDuringStartup = true;
        const int center = weightedX / count;
        minimumCenter = min(minimumCenter, center);
        maximumCenter = max(maximumCenter, center);
      }
      if (screen.invalidWrites) return 10 + mood;
      for (int y = 51; y < 64; ++y)
        for (int x = 0; x < 128; ++x)
          if (screen.pixels[y][x] != (x + y) % 2) return 20 + mood;
    }
    if (!sawBlink || !sawOpen) return 30 + mood;
    if (!openedDuringStartup) return 50 + mood;
    if (maximumCenter - minimumCenter < 15) return 40 + mood;
  }
  return 0;
}

extern "C" __declspec(dllexport) void previewEyes(int mood, uint8_t *destination) {
  testClock = 0; testRandom = 1;
  resetScreen();
  DeskBuddyEyes face(screen, 51);
  face.begin(); face.setEmotion(moods[mood % 5]);
  for (int frame = 0; frame < 200; ++frame) {
    testClock += 50; face.update(testClock);
    int count = 0;
    for (int y = 0; y < 51; ++y)
      for (int x = 0; x < 128; ++x) count += screen.pixels[y][x] != 0;
    if (frame > 20 && count > 200) break; // Capture open eyes, rather than a random blink frame.
  }
  for (int y = 0; y < 64; ++y)
    for (int x = 0; x < 128; ++x) *destination++ = screen.pixels[y][x] ? 255 : 0;
}
