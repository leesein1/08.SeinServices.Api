# DeskBuddy OLED client

ESP32에서 서버의 답변을 실시간으로 받아 128×64 OLED에 표시합니다. 마이크와 스피커 없이 Postman으로 질문할 수 있습니다.

## 설치

1. `DeskBuddyEyes.h`와 `src/third_party`를 포함한 이 폴더 전체를 Arduino 스케치북에 복사합니다. 현재 PC에는 `C:\Users\silee\Documents\Arduino\DeskBuddy_DisplayClient`로 저장되어 있습니다.
2. `../libraries/DeskBuddyCompactFont`를 Arduino 스케치북의 `libraries`에 복사합니다. 초성/중성/종성 부품을 조합하여 한글 11,172자 전체를 표시합니다. 이전 DeskBuddyKoreanFont는 기존 화면 테스트용으로 남겨둘 수 있지만 이 스케치에서는 사용하지 않습니다.
3. 라이브러리 관리자에서 U8g2 **2.36.19**, WebSockets (Markus Sattler/Links2004) **2.7.2**, ArduinoJson **7.4.3**를 설치합니다. 검증한 ESP32 코어는 **3.3.12**입니다.
4. `Secrets.example.h`를 `Secrets.h`로 복사하고 2.4GHz Wi-Fi 이름/암호와 서버의 `DeskBuddy__DeviceApiKey`를 입력합니다. 이 파일은 Git에서 제외됩니다. Gemini 키를 장치에 넣지 않습니다.
5. Arduino IDE를 다시 열고 **ESP32 Dev Module / Flash 4MB / QIO / 40MHz / PSRAM Disabled / Partition Scheme: Minimal SPIFFS (1.9MB APP with OTA/128KB SPIFFS)**를 선택해 업로드합니다. 조합형 폰트로 프로그램 크기를 줄였습니다. 현재 보드의 Minimal SPIFFS 설정을 유지합니다. 파티션 변경은 기존 SPIFFS 데이터와 호환되지 않습니다.

OLED: VCC→3V3, GND→GND, SDA→GPIO21, SCL→GPIO22. I2C 주소 0x3C/0x3D를 자동으로 탐색합니다. 배선과 화면 초기화 방식은 기존 화면 테스트와 같습니다.

## 사용

시리얼 모니터 115200에서 Wi-Fi와 WebSocket 연결을 확인합니다. 서버는 `wss://api.silee.net/api/deskbuddy/device/ws`이며 `X-Device-Key`로 인증합니다. Wi-Fi 연결 후 NTP 시각 동기화가 완료되어야 TLS 인증서 검증이 가능합니다.

Postman에서 기존 `POST https://api.silee.net/api/deskbuddy/chat`에 동일한 `X-Device-Key` 헤더와 JSON을 보냅니다.

```json
{"message":"오늘 인천 날씨 어때~?"}
```

성공한 답변이 연결된 장치에 전달됩니다. 위쪽 0~50행에는 움직이는 RoboEyes 눈을, 아래 51~63행에는 답변을 한 줄로 표시합니다. 처음 1.2초 정지 후 왼쪽으로 65ms당 1픽셀 이동하며 총 2회 재생하고 `대기중..`으로 복귀합니다. 짧은 답변도 두 번 스크롤하며, 줄바꿈과 탭은 공백으로 표시합니다. 새 답변은 이전 답변의 재생 횟수를 초기화하고 바로 교체합니다.

속도는 `SCROLL_STEP_MS`(크면 느림), 반복 횟수는 `REPEAT_COUNT`, 시작 정지는 `START_HOLD_MS`로 조절합니다. 응답 수신 때 글자 위치를 한 번 계산하고, 보이는 글자만 그리며, 눈과 자막을 같은 버퍼에 그린 뒤 변경된 8×8 타일만 전송합니다. 원문 String은 중복 보관하지 않고 JSON은 zero-copy로 파싱합니다. 한글 글꼴 데이터는 428,282→16,760바이트입니다. 조합형 자막만 넣은 버전은 1,143,229바이트였고, 눈 애니메이션을 추가한 버전은 1,148,053바이트(약 1.15MB), 전역 RAM은 54,880바이트입니다. PSRAM은 사용하지 않습니다.

연결이 끊기면 자동 재접속하지만, 오프라인 동안의 답변은 재생하지 않습니다. 현재 서버는 단일 프로세스의 모든 연결에 답변을 전달하므로 서로 다른 사용자의 장치 분리나 다중 서버 인스턴스는 후속 구현이 필요합니다.

`ServerRootCA.h`는 공개된 Google Trust Services Root R4 인증서입니다. 서버 인증서의 루트가 바뀌면 교체해야 합니다. `Secrets.h`와 업로드 바이너리에는 접속 자격 정보가 들어 있으니 공개하지 마세요.

## 움직이는 눈과 조절 위치

`DeskBuddyEyes.h`가 FluxGarage RoboEyes V1.1.1을 기존 U8g2 화면에 연결합니다. 원본 소스는 `src/third_party/FluxGarage_RoboEyes.h`, 라이선스와 출처는 같은 폴더에 보존합니다. 원본을 수정하지 않았으며 별도 Adafruit 디스플레이 라이브러리를 설치하지 않아도 됩니다. RoboEyes 및 통합 펌웨어의 관련 소스는 GPL-3.0-or-later 조건으로 제공합니다.

- 자동 깜빡임은 4~7초 간격이며 최대 30FPS로 눈을 빠르게 감고 뜹니다. 좌우/약간 위/대각선/약간 아래 시선을 무작위로 섞고 1.6~2.7초 둘러본 뒤 정면에서 2.2~4초 쉽니다. 대기할 때는 12~20초 간격으로 한쪽 눈이 약간 커지는 호기심 표정을 1초 보여줍니다. 답변 중에는 이 대기용 연출을 끼워 넣지 않습니다.
- `emotion`: neutral=기본, happy=웃는 눈+잠깐 웃음 움직임, sad=처진 눈꺼풀, angry=화난 눈, surprised=크고 둥근 눈. 답변이 두 번 재생된 뒤에는 neutral로 돌아갑니다.
- 대기 문구는 `대기중.`→`대기중..`→`대기중...`이며 **사용자 설정 각 1200ms, 전체 3600ms**입니다.
- 눈의 크기/간격/모서리/프레임 속도는 `DeskBuddyEyes.h`의 `EYE_WIDTH`, `EYE_HEIGHT`, `EYE_GAP`, `EYE_RADIUS`, `FRAME_RATE`에서 바꿉니다.
- `setEmotion()`은 서버 감정과 눈 모양의 연결, `update()`는 좌우 시선 간격, `BLINK_INTERVAL`/`BLINK_VARIATION`은 깜빡임 간격입니다.
- `.ino`의 `WAITING_INTERVAL_MS`는 대기 점 속도, `SCROLL_STEP_MS`는 자막 이동 속도입니다. 전역변수/화면 영역/통신 순서는 코드의 한글 주석을 참고하세요.
- `sentFrame`은 마지막 화면의 1,024바이트 사본입니다. 같은 그림을 반복 전송하지 않고 바뀐 타일만 보내기 위해 사용합니다. 눈과 글씨가 공유하는 48~55행 타일도 같은 버퍼에서 전송해 경계가 덮이지 않게 합니다.