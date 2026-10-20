# SeinServices.Api

![.NET](https://img.shields.io/badge/.NET-8.0-purple)
![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-Web%20API-512BD4)
![SQL Server](https://img.shields.io/badge/SQL%20Server-Microsoft.Data.SqlClient-CC2927)
![Docker](https://img.shields.io/badge/Docker-Container-2496ED)

여러 개인 프로젝트에서 공통으로 사용하는 ASP.NET Core Web API 서버입니다. 청약·FaultMon과 함께 ESP32 책상 친구용 **DeskBuddy 텍스트 대화 API**를 제공합니다.

처음에는 청약 서비스의 조회·동기화·마감 처리를 담당하는 백엔드로 시작했습니다. 이후 FaultMon의 조회와 실시간 처리까지 이관하면서 두 프로젝트의 DB 접근, 백그라운드 작업, REST API, SignalR을 한 서버에서 관리하게 됐습니다.

<p align="center">
  <a href="https://api.silee.net/swagger">
    <img src="https://img.shields.io/badge/Swagger-API%20문서%20보기-85EA2D?style=for-the-badge&logo=swagger&logoColor=black" alt="Swagger API 문서 보기" />
  </a>
</p>

---

## 시작 배경

청약 서비스를 만들면서 외부 API 호출, DB 접근, 스케줄 작업을 프론트와 분리할 필요가 있었습니다. 그래서 조회·동기화·마감 처리를 담당하는 별도 API 서버를 먼저 구성했습니다.

이후 FaultMon을 MVC 통합 구조에서 React + API 구조로 바꾸면서 FaultMon의 조회와 실시간 처리도 이 서버로 옮겼습니다. 지금은 청약과 FaultMon이 함께 사용하는 공용 백엔드입니다.

---

## 담당 영역

### DeskBuddy (ESP32 책상 친구)

- `POST /api/deskbuddy/chat`: Gemini가 질문을 먼저 판단하고, 필요할 때만 Tavily로 웹 검색한 뒤 `reply`, `emotion`, `searchUsed`, `sources` 반환
- 마이크·스피커 없이 Swagger와 HTTP 요청으로 테스트 가능
- 캐릭터 이름·성격·말투를 서버 설정으로 관리
- 이번 단계는 단일 질문/답변입니다. 대화 기억, DB 저장, 음성 입출력, 호출어 인식은 아직 포함하지 않습니다.
- Swagger에서 **DeskBuddy** 문서를 선택하거나 `/swagger/deskbuddy/swagger.json`을 확인하세요.

```json
{ "message": "안녕! 오늘 너무 피곤하다." }
```

응답 예시 (실제 문장은 모델에 따라 달라집니다):

```json
{
  "reply": "오늘 많이 힘들었구나. 잠깐 쉬면서 나랑 이야기하자!",
  "emotion": "sad",
  "searchUsed": false,
  "sources": []
}
```

`emotion`은 `neutral`, `happy`, `sad`, `angry`, `surprised` 중 하나입니다. 서버가 잘못된 표정은 `neutral`로 보정하며, 비어 있거나 형식이 잘못된 답변은 오류로 처리합니다. 질문은 공백이 아닌 최대 2,000자, 답변은 최대 600자입니다.

#### 필요한 경우에만 웹 검색

```text
사용자 질문 → Gemini (대사·표정 또는 검색어 판단)
  ├─ 검색 불필요 → 대사·표정 반환 (Gemini 1회, Tavily 0회)
  └─ 검색 필요 → Tavily basic 검색 → Gemini가 근거를 읽고 답변 (Gemini 2회, Tavily 최대 1회)
```

한국 날짜와 시간을 함께 전달합니다. 인사·잡담·일반 지식은 검색하지 않고, 오늘 날씨·최신 뉴스·현재 가격이나 명시적 검색 요청에는 검색하도록 지시합니다. 모델의 판단이므로 불필요한 검색이 완전히 배제되지는 않습니다. 같은 검색어의 성공 결과는 컨테이너 메모리에 5분간 보관해 재사용합니다. 한 요청에서 추가 검색을 반복하지 않으며, Tavily `auto_parameters=false`, `search_depth=basic`, `max_results=3`으로 호출합니다.

`searchUsed=true`는 검색 경로를 사용했다는 뜻이며 캐시 적중도 포함합니다. `sources`는 모델이 만든 URL이 아니라 실제 검색 결과의 제목·URL 목록입니다. 답변 문장별 인용 매핑은 제공하지 않습니다. 검색 결과가 없으면 확인하지 못했다고 답하고, 제공자 오류나 한도 초과는 오류 응답을 반환합니다. 날짜·장소가 다른 자료로 현재 수치를 추측하지 않도록 지시하지만, 일반 웹 검색은 기상 관측 전용 API처럼 실시간 날씨의 정확성을 보장하지 않습니다.

#### Postman에서 날씨 질문하기

- Method: `POST`
- URL: `https://api.silee.net/api/deskbuddy/chat`
- Headers: `Content-Type: application/json`, `X-Device-Key: <장치 접속 키>`
- Body → raw → JSON:

```json
{ "message": "오늘 인천 날씨 어때~?" }
```

질문만 보내면 서버가 검색을 판단합니다. 검색용 파라미터나 Tavily 키를 Postman에 보낼 필요가 없습니다. 응답의 `searchUsed`와 `sources`로 검색 경로와 출처를 확인할 수 있습니다. 인사 테스트는 `{ "message": "안녕!" }`을 보냅니다. 전체 서버 처리 제한은 75초이므로 클라이언트 대기 시간은 85초 이상을 권장합니다.

#### ESP32 OLED에 답변 실시간 표시

ESP32는 `wss://api.silee.net/api/deskbuddy/device/ws`에 연결을 유지합니다. WebSocket 업그레이드 요청의 `X-Device-Key` 헤더로 `/chat`과 동일한 장치 키를 보냅니다. URL에 키를 넣지 않습니다. 정상 연결 후 다음 이벤트가 옵니다.

```json
{ "type": "device.ready" }
```

이 상태에서 Postman으로 기존 `POST /api/deskbuddy/chat`에 질문하면 HTTP 응답과 함께 연결된 화면에 아래 이벤트를 전달합니다. ESP32 메모리를 절약하도록 출처 URL 목록은 장치 이벤트에서 제외합니다.

```json
{
  "type": "chat.answer",
  "id": "이벤트마다 다른 식별자",
  "reply": "안녕! 오늘은 무슨 이야기 하고 싶어?",
  "emotion": "happy",
  "searchUsed": false
}
```

- 현재는 하나의 장치 키를 공유하는 연결된 모든 화면으로 전송합니다. 장치별 선택 전송이나 사용자별 대화방은 없습니다.
- 장치가 없어도 `/chat`은 정상 동작합니다. HTTP 200은 화면 표시 완료 확인을 뜻하지 않습니다.
- 소켓별 전송 대기열은 4개이며 느린 장치는 오래된 대기 이벤트부터 버립니다. 전송 제한은 10초입니다. 오프라인 기록/재접속 시 이전 답변 재생은 제공하지 않습니다.
- 일반 GET으로 소켓 URL을 열면 400입니다. Postman의 **WebSocket 요청**에서 URL과 장치 키 헤더를 설정하면 `device.ready`와 실제 이벤트를 확인할 수 있습니다. Swagger는 연결 규격을 문서화하며 직접 소켓을 열지는 않습니다.
- 장치 코드는 `Firmware/DeskBuddy_DisplayClient` 참고. 로컬 Arduino 스케치도 `Documents/Arduino/DeskBuddy_DisplayClient`에 저장합니다. Wi-Fi/장치 키는 로컬 `Secrets.h`로 분리하며 Git과 Docker 이미지에 포함하지 않습니다.
- 화면 위 51픽셀에는 좌우를 보고 깜빡이는 RoboEyes 눈을 그리고, 아래 13픽셀(약 20%)에 한 줄 답변을 오른쪽에서 왼쪽으로 흘려보냅니다. 2회 재생하면 `대기중..`으로 돌아갑니다. 새 답변은 기존 재생을 즉시 교체합니다. 한글 전체를 조합형으로 표시해 글꼴 데이터를 약 16.8KB로 줄였고, 눈과 글씨를 함께 그린 뒤 변경된 타일만 전송합니다. 서버의 emotion으로 표정이 바뀌고 답변 재생 후 기본 표정으로 복귀합니다. 대기 점은 각 0.2초, 한 바퀴 0.6초로 표시합니다. Wi-Fi/소켓 자동 재연결과 heartbeat를 유지합니다.
- TLS 인증서 검증을 사용하며 연결 전에 NTP 시각을 맞춥니다. 현재 Google Trust Services Root R4를 사용합니다. 서버 인증서의 루트가 바뀌면 `ServerRootCA.h`를 갱신해야 합니다. 인증서 검증을 끄지 않습니다.

#### 서버 환경변수

| 환경변수 | 기본값 / 설명 |
|---|---|
| `Gemini__ApiKey` | 필수. Google AI Studio에서 발급한 키 |
| `Gemini__Model` | `gemini-3.5-flash-lite`. AI Studio에서 해당 프로젝트에 사용 가능한 모델로 변경 가능 |
| `Gemini__TimeoutSeconds` | 30초 (1~120초 범위) |
| `Tavily__ApiKey` | 웹 검색 시 필수. Tavily에서 발급한 키. 없어도 검색이 필요 없는 대화는 가능 |
| `Tavily__TimeoutSeconds` | 검색 대기 15초 (1~30초 범위) |
| `DeskBuddy__DeviceApiKey` | 필수. 직접 정한 충분히 긴 장치 접속 키. Gemini 키와 별개 |
| `DeskBuddy__CharacterName` | `데스크버디` |
| `DeskBuddy__Personality` | 호기심 많고 다정한 책상 친구, 친근한 반말 |

Google API 키는 서버에만 보관합니다. ESP32·Swagger 요청에는 **`X-Device-Key`** 헤더로 장치 접속 키만 전달합니다. Swagger의 요청 입력란에 있는 `X-Device-Key`에 입력한 후 실행하세요. 서버 전체 DeskBuddy 요청은 분당 20회로 제한합니다 (컨테이너별 메모리 기준). 제공자의 모델별 무료 티어/사용량 제한은 별도로 적용됩니다.

설정이 없어도 기존 서비스는 시작됩니다. DeskBuddy를 호출할 때만 설정 누락 오류가 발생합니다. Google API 키·Tavily 키·장치 접속 키를 소스, Dockerfile, Git에 저장하지 마세요. Gemini 프로젝트는 기존 무료 티어를 사용하며, 이 구현은 Google 자체 검색 도구나 유료 티어 전환을 사용하지 않습니다. Tavily 과금 여부는 Tavily 계정의 플랜과 종량제 설정에 따르므로 무료 사용은 Researcher 플랜에서 종량제를 꺼두세요.

| HTTP | 의미 |
|---|---|
| 400 | 질문 누락·공백·길이 초과 또는 잘못된 JSON |
| 401 | 장치 접속 키 누락/불일치 |
| 413 | 요청 본문 16KB 초과 |
| 422 | 제공자가 답변을 차단함 |
| 429 | 이 서버의 요청 제한 또는 Gemini/Tavily 사용량 초과 (`code`로 구분) |
| 502 | Gemini/Tavily 연결·인증·모델·응답 형식 오류 |
| 503 | 서버 설정 누락/잘못된 모델 설정 또는 Gemini 결제 상태 문제 |
| 504 | Gemini/Tavily 응답 시간 초과 또는 전체 처리 75초 초과 |

오류는 기존 `ErrorResponseDto` (`code`, `message`, `traceId`)를 사용합니다. 자동 모델 검증 400 응답은 ASP.NET Core의 ValidationProblemDetails입니다. 사용자 질문·답변과 제공자 오류 본문은 별도 로그나 DB에 저장하지 않습니다. 검색 시 Gemini가 만든 검색어는 Tavily로 전송하고, 검색 결과의 발췌는 Gemini로 전달합니다. 검색 캐시는 재시작 시 사라지며 대화 기억과는 별개입니다.

대표 오류 코드: `TAVILY_NOT_CONFIGURED`(503), `TAVILY_RATE_LIMITED`(429), `TAVILY_AUTH_FAILED`(502), `TAVILY_TIMEOUT`(504), `GEMINI_BILLING_REQUIRED`(503). 마지막 코드는 Google의 선불 잔액 소진/결제 필요 응답을 나타냅니다. 키 교체 또는 무료 티어 복귀 후 컨테이너를 재생성해야 합니다.

#### 로컬 테스트

PowerShell 예시 (실제 키는 자신의 터미널에서 입력):

```powershell
$env:Gemini__ApiKey = 'YOUR_GEMINI_API_KEY'
$env:Tavily__ApiKey = 'YOUR_TAVILY_API_KEY'
$env:DeskBuddy__DeviceApiKey = 'YOUR_OWN_LONG_RANDOM_DEVICE_KEY'
dotnet run
```

HTTP 요청 예시는 `SeinServices.Api.http`에 있습니다. API 호출 코드는 추가 SDK 없이 `HttpClient`와 `System.Text.Json`을 사용합니다. Google의 `generateContent` REST API와 JSON 응답 스키마를 사용합니다.

#### Docker

독립 테스트 컨테이너 (기존 8080 서비스와 충돌하지 않음):

```powershell
Copy-Item .env.example .env
# .env의 Gemini__ApiKey, Tavily__ApiKey, DeskBuddy__DeviceApiKey 값을 자신의 편집기에서 설정
docker compose -f compose.deskbuddy.yml up -d --build
```

Swagger: `http://localhost:8081/swagger` → **DeskBuddy**. 이 테스트 구성은 DB 스케줄러를 끄므로 DB 없이 실행 가능합니다. 중지: `docker compose -f compose.deskbuddy.yml down`.

기존 홈서버의 `seinservices-api`에 통합할 때는 기존 DB·Slack·스케줄러 설정을 유지하고, 위 환경변수만 컨테이너에 추가한 후 새 이미지로 재생성하면 됩니다. Gemini API 키는 이미지 빌드에 필요하지 않으며 실행 시 주입합니다. 개발용/로컬 설정 파일과 `.env`는 Docker 빌드 컨텍스트에서 제외됩니다.

홈서버는 `C:\workspace_silee\home-server\deskbuddy.env`에서 `Gemini__ApiKey`, `Tavily__ApiKey`를 읽습니다. 값을 변경한 다음 해당 폴더에서 아래 명령을 실행하세요. Swagger 요청의 `X-Device-Key`에는 이 파일의 `DeskBuddy__DeviceApiKey` 값을 입력합니다.

```powershell
cd C:\workspace_silee\home-server
docker compose up -d --no-deps --pull never --force-recreate seinservices-api
```

화면 연동 운영 이미지는 `silee98/seinservices-api:deskbuddy-display-20261020`이며 Docker Hub에서 내려받을 수 있습니다. `main` 브랜치의 GitHub Actions는 별도로 `latest`와 commit SHA 태그를 게시합니다. `deskbuddy.env`의 비밀 값은 소스에 복사하지 마세요. 검색 연동 이전 이미지 `silee98/seinservices-api:deskbuddy-20261020`도 로컬에 유지됩니다.

#### 검증

```powershell
dotnet build
dotnet test Tests/SeinServices.Api.Tests.csproj
```

테스트는 Gemini·Tavily 통신을 모의 처리하며 실제 API 키나 유료 호출이 필요하지 않습니다. 인사의 검색 생략, Gemini→Tavily→Gemini 순서, 캐시 재사용, 검색 실패/빈 결과/잘못된 출처 처리와 기존 인증·제한을 검증합니다. 실 API 호출 성공 여부는 자신의 키로 별도 확인해야 합니다.

공식 참고: [Gemini REST API](https://ai.google.dev/api/generate-content), [모델 목록](https://ai.google.dev/gemini-api/docs/models), [사용량 제한](https://ai.google.dev/gemini-api/docs/rate-limits).

### Chungyak Manager

- 청약 공고 검색 / 상세 조회
- 공공 API 데이터 동기화
- 즐겨찾기 등록·해제
- 마감 상태 갱신
- Slack 알림 및 로그 저장
- 스케줄 실행 로그 조회

Frontend: [chungyak_manage_web](https://github.com/leesein1/chungyak_manage_web)

### FaultMon

- 최근 고장 목록 / 금일 통계 / 상세 조회
- 누적 고장 이력 조건 검색 및 페이징
- 기존 FaultMon 호출 경로 호환
- SignalR Hub 연결
- 접속자가 있을 때 반복 프로시저 실행 및 이벤트 전송

Frontend: [FaultMon-Front](https://github.com/leesein1/FaultMon-Front)

---

## 기술

**Backend**  
`C#` `.NET 8` `ASP.NET Core Web API`

**Data**  
`Microsoft.Data.SqlClient` `SQL Server` `Azure SQL` `Stored Procedure`

**Realtime / Background**  
`SignalR` `BackgroundService` `PeriodicTimer`

**Operation**  
`Swagger` `Docker` `Docker Hub` `GitHub Actions`

---

## 구조

```text
Chungyak React ── REST ──┐
                         │
FaultMon React ─ REST ───┼──> SeinServices.Api
               SignalR ─┘        ├─ Controllers/Chungyak
                                  ├─ Controllers/FaultMon
                                  ├─ Services/Chungyak
                                  ├─ Services/FaultMon
                                  └─ Services/Schedules
                                           │
                                  ┌────────┴────────┐
                                  ▼                 ▼
                             Chungyak DB       FaultMon DB
```

청약과 FaultMon은 Controller, Service, Data, Model을 도메인별로 나눴습니다. Swagger도 `청약 서비스`, `FaultMon 고장 관제` 문서로 분리합니다.

---

## 서버에서 처리하는 작업

청약 쪽은 `BackgroundService`로 공고 동기화, 마감 처리, 구독 알림을 실행할 수 있습니다. 같은 작업을 수동으로 실행하는 `run-once` API도 남겨두었습니다.

FaultMon은 `FaultMonConnectionTracker`에서 SignalR Connection ID를 관리합니다. 활성 연결이 없으면 반복 작업을 건너뜁니다. 접속자가 있을 때 `PROC_SCH_REPEAT_INSERT`를 실행하고 `Signal_FLTLIST` 이벤트를 전송합니다.

누적 고장 검색은 Keyword, 접수번호, 차량번호, 고객명, 담당자, 상태, 기간을 조건으로 받습니다. `Page`, `PageSize`를 저장 프로시저에 전달하며 PageSize는 1~500 범위로 제한합니다.

---

## 운영 구조 변화

초기에는 Azure App Service 무료 환경에 배포했습니다. 유휴 상태에서 앱이 멈춰 내부 스케줄 실행을 보장하기 어려워 GitHub Actions에서 `warmup → 대기 → run-once` 순서로 작업을 호출했습니다.

이후 Docker 이미지를 만들고 홈서버에서 API를 상시 실행하도록 이전했습니다. 현재 구조에서는 `BackgroundService`를 사용할 수 있고, 기존 외부 트리거 방식도 필요할 때 사용할 수 있도록 남아 있습니다.

`main` 브랜치나 버전 태그가 push되면 GitHub Actions가 Docker Hub에 이미지를 배포합니다. `latest`, semver, commit SHA 태그를 생성합니다.

환경별 DB 연결 문자열, API Key, Slack 설정은 환경변수에서 주입합니다.

---

## 수정한 문제

### UTC / KST

Azure SQL과 실행 환경의 시간 기준 차이 때문에 청약 마감 처리와 로그 시간이 어긋났습니다. 날짜 비교와 로그 저장 기준을 KST로 맞췄습니다.

### FaultMon 반복 DB 작업

관제 화면 사용자가 없어도 DB 작업이 반복되는 것을 막기 위해 SignalR 활성 연결 수를 확인한 뒤 프로시저를 실행하도록 변경했습니다.

### 기존 FaultMon 경로 유지

기존 MVC 화면과 신규 React 화면을 병행할 수 있도록 `/Fault/...` 경로와 신규 `/api/faultmon/...` 경로를 함께 매핑했습니다.

### 공공 API Query Parameter

외부 API 요청에서 문자열 조건을 URL Encoding 후 전달하도록 수정했습니다.

---

<details>
<summary><b>로컬 실행</b></summary>
<br/>

```bash
dotnet restore
dotnet build
dotnet run
```

Swagger는 실행 후 `/swagger`에서 확인할 수 있습니다.

</details>
