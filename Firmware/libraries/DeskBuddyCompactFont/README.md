# DeskBuddy Compact Font

한글 완성 글자를 개별 저장하지 않고 초성/중성/종성의 360개 부품을 조합합니다. 현대 한글 11,172자 전체를 지원하며, 16×16 조합 결과의 획을 합쳐 12×13 칸에 표시합니다. 영문은 6×13, 호환 자모와 일부 기호는 12×13입니다. 지원하지 않는 문자(이모지/한자 등)는 `?`로 표시합니다.

전체 글꼴 데이터는 16,760바이트입니다. 한글 부품 11,520바이트, ASCII 950바이트, 자모/기호 3,900바이트, 기호 색인 390바이트입니다. 변환용 32바이트 버퍼 대신 각 부품을 읽으며 작은 출력 버퍼에 직접 합칩니다.

- 한글 부품 데이터: [dreve님의 글](https://m.blog.naver.com/dreve/221818135061)의 공개 첨부 `KSFont.h`. 원글은 bigwavek/​orasman의 한글 출력 예제를 출처로 밝힙니다. 이 데이터의 출처를 보존하며 아래 OFL 표기는 영문/기호 부분에 해당합니다.
- 영문/기호: [NAVER NanumGothicCoding 2.5](https://github.com/naver/nanumfont/releases/tag/VER2.5), SIL Open Font License 1.1. 저작권/라이선스는 `LICENSE.md`에 포함합니다.
- 글의 TFT 드라이버 및 문자열 파서는 사용하지 않습니다. OLED용 렌더링과 UTF-8 검증을 별도로 구현했습니다. ㄱ/ㅋ의 중성 형태 분기는 Unicode 초성 순서 0/15를 사용합니다.

`Firmware/tools/build_compact_font.py`로 재생성할 수 있습니다. Pillow와 fonttools가 필요합니다. 스크립트는 모든 한글 조합에 대해 부품 범위와 비어 있지 않은 출력을 검사합니다. Arduino에서는 이 폴더를 스케치북 `libraries` 아래 설치합니다.
