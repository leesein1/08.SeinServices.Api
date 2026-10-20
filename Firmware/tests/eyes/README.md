# Eyes renderer checks

실제 `DeskBuddyEyes.h`와 원본 RoboEyes를 MSVC로 컴파일합니다. 테스트 전용 Arduino 시계/난수와 화면 버퍼만 바꿔 넣습니다. 표정 5개마다 가상 시간 90초를 진행하며 다음을 검사합니다.

- 위쪽에 눈이 열리고 닫히는지, 좌우 시선이 충분히 움직이는지.
- 화면 밖에 픽셀을 쓰지 않는지.
- 아래 51~63행의 기존 자막 픽셀이 한 번도 바뀌지 않는지(공유 타일의 경계 포함).

`python verify_eyes.py --compiler <cl.exe 절대경로> --output <빌드 폴더> --preview`

MSVC 빌드 도구와 Python이 필요하며 미리보기 옵션만 Pillow를 사용합니다. 빌드 결과는 지정한 폴더에만 생깁니다. CRT 진입점 없는 테스트 DLL이므로 테스트에서 전역 화면을 명시적으로 초기화하며, 정적 초기화 관련 LNK4210 경고가 표시될 수 있습니다. 이 테스트용 헤더와 DLL은 Arduino 스케치/운영 Docker 이미지에 포함하지 않습니다.
