# DeskBuddy OLED client

ESP32에서 서버의 답변을 실시간으로 받아 128×64 OLED에 표시합니다. 마이크와 스피커 없이 Postman으로 질문할 수 있습니다.

## 설치

1. 이 폴더를 Arduino 스케치북에 복사합니다. 현재 PC에는 `C:\Users\silee\Documents\Arduino\DeskBuddy_DisplayClient`로 저장되어 있습니다.
2. `../libraries/DeskBuddyKoreanFont`를 Arduino 스케치북의 `libraries`에 복사합니다. 기존 설치가 있다면 최신 헤더로 교체합니다. 한글 11,172자 전체를 포함하며 현재 U8g2의 Unicode 검색 형식으로 보정한 버전입니다.
3. 라이브러리 관리자에서 U8g2 **2.36.19**, WebSockets (Markus Sattler/Links2004) **2.7.2**, ArduinoJson **7.4.3**를 설치합니다. 검증한 ESP32 코어는 **3.3.12**입니다.
4. `Secrets.example.h`를 `Secrets.h`로 복사하고 2.4GHz Wi-Fi 이름/암호와 서버의 `DeskBuddy__DeviceApiKey`를 입력합니다. 이 파일은 Git에서 제외됩니다. Gemini 키를 장치에 넣지 않습니다.
5. Arduino IDE를 다시 열고 **ESP32 Dev Module / Flash 4MB / QIO / 40MHz / PSRAM Disabled / Partition Scheme: Minimal SPIFFS (1.9MB APP with OTA/128KB SPIFFS)**를 선택해 업로드합니다. 폰트와 TLS를 포함한 프로그램은 약 1.56MB이므로 기본 1.2MB APP 설정에는 들어가지 않습니다. 파티션 변경은 기존 SPIFFS 데이터와 호환되지 않습니다.

OLED: VCC→3V3, GND→GND, SDA→GPIO21, SCL→GPIO22. I2C 주소 0x3C/0x3D를 자동으로 탐색합니다. 배선과 화면 초기화 방식은 기존 화면 테스트와 같습니다.

## 사용

시리얼 모니터 115200에서 Wi-Fi와 WebSocket 연결을 확인합니다. 서버는 `wss://api.silee.net/api/deskbuddy/device/ws`이며 `X-Device-Key`로 인증합니다. Wi-Fi 연결 후 NTP 시각 동기화가 완료되어야 TLS 인증서 검증이 가능합니다.

Postman에서 기존 `POST https://api.silee.net/api/deskbuddy/chat`에 동일한 `X-Device-Key` 헤더와 JSON을 보냅니다.

```json
{"message":"오늘 인천 날씨 어때~?"}
```

성공한 답변이 연결된 장치에 전달됩니다. 한 화면에 한글 3줄씩 표시하며 긴 답변은 6초마다 다음 페이지를 보여줍니다. 줄바꿈 계산은 수신 시에만, 화면 전송은 내용/페이지/접속 상태 변경 시에만 수행합니다. PSRAM은 사용하지 않습니다.

연결이 끊기면 자동 재접속하지만, 오프라인 동안의 답변은 재생하지 않습니다. 현재 서버는 단일 프로세스의 모든 연결에 답변을 전달하므로 서로 다른 사용자의 장치 분리나 다중 서버 인스턴스는 후속 구현이 필요합니다.

`ServerRootCA.h`는 공개된 Google Trust Services Root R4 인증서입니다. 서버 인증서의 루트가 바뀌면 교체해야 합니다. `Secrets.h`와 업로드 바이너리에는 접속 자격 정보가 들어 있으니 공개하지 마세요.
