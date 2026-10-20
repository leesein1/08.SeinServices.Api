# DeskBuddy Korean font

Shared Arduino sketchbook library wrapping the existing converted 16px font.
Original file and copyright notice are preserved.
The legacy upstream header used the pre-index U8g2 Unicode layout. It has been
updated with the Unicode lookup index required by installed U8g2 2.36.19.
All 11,172 modern Hangul syllables and 12,313 Unicode glyph bitmaps were verified
against the original. The original header remains as a .h.original backup.
Source: https://github.com/xyz37/U8G2_Korean_Font
Original font: Naver NanumGothicCoding, https://github.com/naver/nanumfont

Restart Arduino IDE after adding the library. In any ESP32 sketch:

```cpp
#include <DeskBuddyKoreanFont.h>
// After configuring a U8g2 display:
// oled.setFont(u8g2_font_unifont_t_korean_NanumGothicCoding_16);
// oled.drawUTF8(0, 16, "인천 맑음");
```

The font remains in Documents/Arduino/libraries and does not need to be copied
when creating another sketch. A different computer needs this library installed too.
