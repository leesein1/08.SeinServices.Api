#pragma once
#include <Arduino.h>
#include <U8g2lib.h>
#include "src/third_party/FluxGarage_RoboEyes.h"

/*
 * RoboEyes는 원래 Adafruit 방식의 도형 함수를 호출한다.
 * 이 연결 클래스가 같은 요청을 기존 U8g2 버퍼에 그리므로 드라이버/화면 버퍼를
 * 추가로 설치하지 않아도 된다. 모든 좌표는 눈 영역 안으로 제한한다.
 * 특히 웃는 눈의 검은 덮개가 아래 한글 자막까지 지우지 않도록 경계를 지킨다.
 */
class RoboEyesCanvas {
 public:
  // 화면은 외부 객체를 참조한다. 이 클래스가 별도의 1KB 화면을 만들지는 않는다.
  RoboEyesCanvas(U8G2 &screen, int faceHeight) : screen_(screen), height_(faceHeight) {}

  // RoboEyes의 '전체 지우기'를 눈 영역만 지우는 동작으로 바꾼다.
  void clearDisplay() {
    screen_.setDrawColor(0);
    screen_.drawBox(0, 0, 128, height_);
    screen_.setDrawColor(1);
  }

  // 즉시 I2C 전송하지 않는다. 눈/글씨를 모두 그린 뒤 변경된 타일만 한 번 보낸다.
  void display() { dirty_ = true; }
  bool takeDirty() { const bool result = dirty_; dirty_ = false; return result; }

  // 음수 좌표도 안전하게 잘라낸다. 웃음/시선 이동 때 화면 밖 도형이 생길 수 있다.
  void fillRoundRect(int x, int y, int width, int height, int radius, uint8_t color) {
    if (width <= 0 || height <= 0) return;
    radius = max(0, min(radius, min(width, height) / 2));
    screen_.setDrawColor(color ? 1 : 0);
    for (int row = max(0, y); row < min(height_, y + height); ++row) {
      const int local = row - y;
      int inset = 0;
      if (radius && (local < radius || local >= height - radius)) {
        // 픽셀의 중심을 기준으로 원의 경계를 계산한다. 실수 연산은 쓰지 않는다.
        const int edgeRow = min(local, height - 1 - local);
        const int dy = 2 * radius - 1 - 2 * edgeRow;
        while (inset < radius) {
          const int dx = 2 * radius - 1 - 2 * inset;
          if (dx * dx + dy * dy <= 4 * radius * radius) break;
          ++inset;
        }
      }
      horizontal(x + inset, x + width - inset - 1, row);
    }
    screen_.setDrawColor(1);
  }

  // 화남/졸림의 눈꺼풀에 쓰는 삼각형. 정수 외적으로 내부를 판정한다.
  void fillTriangle(int x0, int y0, int x1, int y1, int x2, int y2, uint8_t color) {
    const int left = max(0, min(x0, min(x1, x2)));
    const int right = min(127, max(x0, max(x1, x2)));
    const int top = max(0, min(y0, min(y1, y2)));
    const int bottom = min(height_ - 1, max(y0, max(y1, y2)));
    screen_.setDrawColor(color ? 1 : 0);
    for (int row = top; row <= bottom; ++row) {
      int first = right + 1, last = left - 1;
      for (int column = left; column <= right; ++column) {
        const int32_t a = cross(x0, y0, x1, y1, column, row);
        const int32_t b = cross(x1, y1, x2, y2, column, row);
        const int32_t c = cross(x2, y2, x0, y0, column, row);
        if ((a >= 0 && b >= 0 && c >= 0) || (a <= 0 && b <= 0 && c <= 0)) {
          first = min(first, column);
          last = column;
        }
      }
      horizontal(first, last, row);
    }
    screen_.setDrawColor(1);
  }

 private:
  U8G2 &screen_;       // 기존 OLED의 화면 버퍼 참조.
  const int height_;   // 이 높이 이상에는 눈/눈꺼풀을 그리지 않는다(현재 51픽셀).
  bool dirty_ = false; // RoboEyes가 새 프레임을 그렸는지 알리는 표시.

  void horizontal(int left, int right, int y) {
    left = max(0, left);
    right = min(127, right);
    if (left <= right) screen_.drawHLine(left, y, right - left + 1);
  }
  static int32_t cross(int ax, int ay, int bx, int by, int px, int py) {
    return static_cast<int32_t>(bx - ax) * (py - ay) - static_cast<int32_t>(by - ay) * (px - ax);
  }
};

/* 표정과 시선의 제어를 한곳에 모은다. delay 없이 loop에서 update()를 호출한다. */
class DeskBuddyEyes {
 public:
  // 변경하기 쉬운 기본 모양. 픽셀 단위이며 아래 자막 영역과 별개다.
  static constexpr uint8_t FRAME_RATE = 20;  // 최대 초당 20프레임. 같은 그림은 전송하지 않는다.
  static constexpr uint8_t EYE_WIDTH = 30;   // 기본 눈 한 개의 너비.
  static constexpr uint8_t EYE_HEIGHT = 26;  // 기본 눈 한 개의 높이.
  static constexpr uint8_t EYE_GAP = 16;     // 두 눈 사이 간격.
  static constexpr uint8_t EYE_RADIUS = 8;   // 눈 모서리의 둥근 정도.

  DeskBuddyEyes(U8G2 &screen, uint8_t height) : canvas_(screen, height), eyes_(canvas_), height_(height) {}

  void begin() {
    eyes_.begin(128, height_, FRAME_RATE);
    eyes_.setDisplayColors(0, 1);
    eyes_.setSpacebetween(EYE_GAP);
    eyes_.setAutoblinker(true, 2, 3); // 보통 2~4초 사이에 한 번씩 자동 깜빡임.
    eyes_.setIdleMode(false);        // 시선은 아래 좌/중앙/우 순서로 직접 제어한다.
    eyes_.setCuriosity(true);        // 옆을 볼 때 바깥쪽 눈을 약간 키워 호기심을 표현한다.
    setEmotion("neutral");
    // 원본은 화면 크기/눈 간격을 바꾸기 전 기본 좌표를 생성자에서 계산한다.
    // 현재 설정으로 시작 좌표도 명시해 첫 프레임부터 화면 중앙에서 뜨게 한다.
    eyes_.eyeLwidthCurrent = eyes_.eyeRwidthCurrent = EYE_WIDTH;
    eyes_.spaceBetweenCurrent = EYE_GAP;
    eyes_.setPosition(DEFAULT);
    eyes_.eyeLx = eyes_.eyeLxNext;
    eyes_.eyeLy = eyes_.eyeLyNext;
    eyes_.eyeRx = eyes_.eyeRxNext = eyes_.eyeLx + EYE_WIDTH + EYE_GAP;
    eyes_.eyeRy = eyes_.eyeRyNext = eyes_.eyeLy;
    eyes_.open();
    lastGazeAt_ = millis();
    gazeInterval_ = 2200;
  }

  // 서버에서 받은 감정 이름을 RoboEyes의 실제 눈 모양으로 연결한다.
  // 알려지지 않은 값은 기본 표정으로 처리해 잘못된 데이터에도 계속 동작한다.
  void setEmotion(const char *name) {
    eyes_.setWidth(EYE_WIDTH, EYE_WIDTH);
    eyes_.setHeight(EYE_HEIGHT, EYE_HEIGHT);
    eyes_.setBorderradius(EYE_RADIUS, EYE_RADIUS);
    eyes_.setMood(DEFAULT);
    if (strcmp(name, "happy") == 0) {
      eyes_.setMood(HAPPY);
      eyes_.anim_laugh(); // 기쁜 답변이 도착하면 잠깐 위아래로 웃는다.
    } else if (strcmp(name, "sad") == 0) {
      eyes_.setMood(TIRED); // 내려온 눈꺼풀로 시무룩한 느낌을 표현한다.
    } else if (strcmp(name, "angry") == 0) {
      eyes_.setMood(ANGRY);
    } else if (strcmp(name, "surprised") == 0) {
      eyes_.setWidth(36, 36);
      eyes_.setHeight(34, 34);
      eyes_.setBorderradius(15, 15); // 놀랐을 때 크고 둥근 눈.
    } else name = "neutral";
    eyes_.setPosition(DEFAULT);     // 새 답변을 받을 때 우선 정면을 본다.
    lastGazeAt_ = millis();
    gazeInterval_ = 2200;
    Serial.printf("Emotion: %s\n", name);
  }

  // 반환값은 새 프레임 유무. 눈과 글씨는 같은 메인 루프에서 동시에 진행한다.
  bool update(uint32_t now) {
    if (now - lastGazeAt_ >= gazeInterval_) {
      static const uint8_t directions[] = {W, DEFAULT, E, DEFAULT};
      eyes_.setPosition(directions[gazeStep_]);
      gazeStep_ = (gazeStep_ + 1) % 4;
      lastGazeAt_ = now;
      gazeInterval_ = random(1600, 3301); // 시선 순서는 지키되 머무는 시간은 자연스럽게 달리한다.
    }
    eyes_.update();
    return canvas_.takeDirty();
  }

 private:
  RoboEyesCanvas canvas_;           // U8g2와 RoboEyes 사이의 연결 객체.
  RoboEyes<RoboEyesCanvas> eyes_;    // 원본 RoboEyes의 깜빡임/눈꺼풀/보간 애니메이션 상태.
  const uint8_t height_;            // 위쪽 표정 영역의 높이.
  uint8_t gazeStep_ = 0;            // 다음 시선: 왼쪽→중앙→오른쪽→중앙.
  uint32_t lastGazeAt_ = 0;         // 마지막 시선 변경 시각(ms).
  uint32_t gazeInterval_ = 2200;    // 다음 시선 변경까지 기다리는 시간(ms).
};
