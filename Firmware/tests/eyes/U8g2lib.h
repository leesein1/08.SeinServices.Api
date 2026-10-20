#pragma once
#include "Arduino.h"
class U8G2 {
 public:
  uint8_t pixels[64][128] = {};
  int invalidWrites = 0;
  uint8_t color = 1;
  void setDrawColor(uint8_t next) { color = next; }
  void drawHLine(int x, int y, int width) {
    for (int column = x; column < x + width; ++column) {
      if (column < 0 || column >= 128 || y < 0 || y >= 64) { ++invalidWrites; continue; }
      pixels[y][column] = color;
    }
  }
  void drawBox(int x, int y, int width, int height) {
    for (int row = y; row < y + height; ++row) drawHLine(x, row, width);
  }
};
