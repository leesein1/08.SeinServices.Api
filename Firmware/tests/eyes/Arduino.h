#pragma once
// Host test doubles only. They are never part of the ESP32 sketch.
typedef unsigned char uint8_t;
typedef signed char int8_t;
typedef unsigned short uint16_t;
typedef unsigned int uint32_t;
typedef int int32_t;
typedef unsigned char byte;
typedef unsigned __int64 size_t;
static uint32_t testClock = 0, testRandom = 1;
inline unsigned long millis() { return testClock; }
inline long random(long maximum) {
  testRandom = testRandom * 1664525U + 1013904223U;
  return maximum > 0 ? (testRandom >> 1) % maximum : 0;
}
inline long random(long minimum, long maximum) { return minimum + random(maximum - minimum); }
template<class T> inline T min(T a, T b) { return a < b ? a : b; }
template<class T> inline T max(T a, T b) { return a > b ? a : b; }
inline int strcmp(const char *a, const char *b) { while (*a && *a == *b) { ++a; ++b; } return *a - *b; }
struct TestSerial { template<class... Args> int printf(const char *, Args...) { return 0; } };
static TestSerial Serial;
