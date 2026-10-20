"""Build the fixed-cell 12px display font. Requires Pillow and fonttools.

Usage: python build_compact_font.py NanumGothicCoding.ttf KSFont.h output_directory
Source: https://github.com/naver/nanumfont/releases/tag/VER2.5 (SIL OFL 1.1).
All 11,172 Hangul syllables are required; no frequently-used-character subset.
"""
import sys
import re
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
from fontTools.ttLib import TTFont

source, parts_source, output = map(Path, sys.argv[1:4])
output.mkdir(parents=True, exist_ok=True)
font = ImageFont.truetype(str(source), 12)
cmap = TTFont(source).getBestCmap()
hangul = list(range(0xAC00, 0xD7A4))
ascii_codes = list(range(32, 127))
assert all(code in cmap for code in hangul + ascii_codes)
extra_candidates = (list(range(0xA0, 0x100)) + list(range(0x2000, 0x2070))
                    + list(range(0x3000, 0x3040)) + list(range(0x3131, 0x318F))
                    + list(range(0x2190, 0x219A)) + [0x20A9, 0x2103, 0x2109])
extras = sorted({code for code in extra_candidates if code in cmap})

def bitmap(code, width):
    bounds = font.getbbox(chr(code), anchor='ls')
    if code in range(0xAC00, 0xD7A4) or code in ascii_codes:
        assert bounds[0] >= 0 and bounds[2] <= width, (hex(code), bounds)
        assert bounds[1] >= -10 and bounds[3] <= 3, (hex(code), bounds)
    canvas = Image.new('1', (width, 13))
    ImageDraw.Draw(canvas).text((0, 10), chr(code), font=font, fill=1, anchor='ls')
    bits = [canvas.getpixel((x, y)) for y in range(13) for x in range(width)]
    packed = bytearray((len(bits) + 7) // 8)
    for index, value in enumerate(bits):
        if value:
            packed[index // 8] |= 0x80 >> (index % 8)
    if code in range(0xAC00, 0xD7A4) or code in ascii_codes:
        assert any(packed) or chr(code).isspace(), hex(code)
    return bytes(packed)

def array(name, data):
    lines = [f'static const uint8_t {name}[] PROGMEM = {{']
    for start in range(0, len(data), 24):
        lines.append('  ' + ','.join(f'0x{value:02x}' for value in data[start:start + 24]) + ',')
    return '\n'.join(lines) + '\n};\n'

parts_data = bytes(int(value, 16) for value in re.findall(r'0x([0-9a-fA-F]{2})', parts_source.read_text(errors='replace')))
assert len(parts_data) == 360 * 32
ascii_data = b''.join(bitmap(code, 6) for code in ascii_codes)
extra_data = b''.join(bitmap(code, 12) for code in extras)
header = '''#pragma once
#include <Arduino.h>
// DeskBuddy Compact 12. Hangul parts: KSFont.h from the article linked below.
// ASCII/extras: NAVER NanumGothicCoding, copyright (c) 2009 NAVER, SIL OFL 1.1.
// See README.md and LICENSE.md for provenance. Generated, do not edit.
// Hangul parts: KSFont.h from https://m.blog.naver.com/dreve/221818135061 (see README).
// Fixed-cell packed bits: Hangul/extra 12x13 (20 bytes), ASCII 6x13 (10 bytes).
'''
header += array('deskBuddyParts16', parts_data)
header += array('deskBuddyAscii12', ascii_data)
header += 'static const uint16_t deskBuddyExtraCodes[] PROGMEM = {' + ','.join(hex(code) for code in extras) + '};\n'
header += array('deskBuddyExtra12', extra_data)
header += r"""
inline int deskBuddyExtraIndex(uint32_t code) {
  size_t low = 0, high = sizeof(deskBuddyExtraCodes) / sizeof(deskBuddyExtraCodes[0]);
  while (low < high) {
    size_t middle = low + (high - low) / 2;
    if (pgm_read_word(deskBuddyExtraCodes + middle) < code) low = middle + 1;
    else high = middle;
  }
  if (low < sizeof(deskBuddyExtraCodes) / sizeof(deskBuddyExtraCodes[0]) && pgm_read_word(deskBuddyExtraCodes + low) == code)
    return static_cast<int>(low);
  return -1;
}
inline uint8_t deskBuddyGlyphWidth(uint32_t code) {
  if (code >= 32 && code <= 126) return 6;
  if ((code >= 0xAC00 && code <= 0xD7A3) || deskBuddyExtraIndex(code) >= 0) return 12;
  return 6;
}
// Compose the 16x16 parts, then OR-area shrink to 12x13 without dropping thin strokes.
inline uint8_t deskBuddyGlyph(uint32_t code, uint8_t output[20]) {
  memset(output, 0, 20);
  if (code >= 0xAC00 && code <= 0xD7A3) {
    static const uint8_t leadingOpen[21] = {0,0,0,0,0,0,0,0,1,3,3,3,1,2,4,4,4,2,1,3,0};
    static const uint8_t leadingClosed[21] = {5,5,5,5,5,5,5,5,6,7,7,7,6,6,7,7,7,6,6,7,5};
    static const uint8_t trailingType[21] = {0,2,0,2,1,2,1,2,3,0,2,1,3,3,1,2,1,3,3,1,1};
    const uint16_t syllable = code - 0xAC00;
    const uint8_t initial = syllable / 588;
    const uint8_t medial = (syllable / 28) % 21;
    const uint8_t final = syllable % 28;
    const uint8_t initialStyle = final ? leadingClosed[medial] : leadingOpen[medial];
    // The narrow-stem variants are for ㄱ and ㅋ (Unicode initial indexes 0 and 15).
    const uint8_t medialStyle = (final ? 2 : 0) + ((initial == 0 || initial == 15) ? 0 : 1);
    const uint16_t first = (initialStyle * 20 + initial + 1) * 32;
    const uint16_t middle = (160 + medialStyle * 22 + medial + 1) * 32;
    const uint16_t last = (248 + trailingType[medial] * 28 + final) * 32;
    for (uint8_t sy = 0; sy < 16; ++sy) {
      for (uint8_t half = 0; half < 2; ++half) {
        const uint8_t byte = sy * 2 + half;
        uint8_t pixels = pgm_read_byte(deskBuddyParts16 + first + byte) | pgm_read_byte(deskBuddyParts16 + middle + byte);
        if (final) pixels |= pgm_read_byte(deskBuddyParts16 + last + byte);
        for (uint8_t bit = 0; bit < 8; ++bit) {
          if (!(pixels & (0x80 >> bit))) continue;
          const uint16_t target = (sy * 13 / 16) * 12 + ((half * 8 + bit) * 12 / 16);
          output[target / 8] |= 0x80 >> (target % 8);
        }
      }
    }
    return 12;
  }
  const int extra = deskBuddyExtraIndex(code);
  if (extra >= 0) {
    memcpy_P(output, deskBuddyExtra12 + extra * 20, 20);
    return 12;
  }
  if (code < 32 || code > 126) code = '?';
  memcpy_P(output, deskBuddyAscii12 + (code - 32) * 10, 10);
  return 6;
}
"""
def composed(code):
    initial, medial, final = (code - 0xAC00) // 588, ((code - 0xAC00) // 28) % 21, (code - 0xAC00) % 28
    leading = [5,5,5,5,5,5,5,5,6,7,7,7,6,6,7,7,7,6,6,7,5] if final else [0,0,0,0,0,0,0,0,1,3,3,3,1,2,4,4,4,2,1,3,0]
    trailing = [0,2,0,2,1,2,1,2,3,0,2,1,3,3,1,2,1,3,3,1,1]
    middle_type = (2 if final else 0) + (0 if initial in (0, 15) else 1)
    offsets = [(leading[medial] * 20 + initial + 1) * 32, (160 + middle_type * 22 + medial + 1) * 32]
    if final: offsets.append((248 + trailing[medial] * 28 + final) * 32)
    assert all(0 <= offset and offset + 32 <= len(parts_data) for offset in offsets)
    result = bytearray(20)
    for index in range(32):
        pixels = 0
        for offset in offsets: pixels |= parts_data[offset + index]
        for bit in range(8):
            if pixels & (0x80 >> bit):
                target = ((index // 2) * 13 // 16) * 12 + (((index % 2) * 8 + bit) * 12 // 16)
                result[target // 8] |= 0x80 >> (target % 8)
    assert any(result), hex(code)
    return result

for code in hangul: composed(code)
(output / 'DeskBuddyCompactFont.h').write_text(header, encoding='utf-8')
total = len(parts_data) + len(ascii_data) + len(extra_data) + len(extras) * 2
print(f'Font: {total:,} bytes; {len(hangul)} Hangul, {len(ascii_codes)} ASCII, {len(extras)} extras.')
print('Checked 360 component slots and all 11,172 Unicode Hangul mappings; ASCII fits 13px.')

# Pixel preview of exactly the same packed glyphs used by the ESP32 renderer.
preview = Image.new('1', (128, 64))
x = 0
for character in '대기중.. 인천 맑음':
    width = 6 if ord(character) in ascii_codes else 12
    packed = composed(ord(character)) if 0xAC00 <= ord(character) <= 0xD7A3 else bitmap(ord(character), width)
    for y in range(13):
        for column in range(width):
            bit = y * width + column
            if x + column < 128 and packed[bit // 8] & (0x80 >> (bit % 8)):
                preview.putpixel((x + column, 51 + y), 1)
    x += width
preview.resize((768, 384), Image.Resampling.NEAREST).save(output / 'preview.png')
