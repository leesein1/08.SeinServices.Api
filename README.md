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

- `POST /api/deskbuddy/chat`: 텍스트 질문을 Gemini에 전달해 `reply`와 `emotion` 반환
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
  "emotion": "sad"
}
```

`emotion`은 `neutral`, `happy`, `sad`, `angry`, `surprised` 중 하나입니다. 서버가 잘못된 표정은 `neutral`로 보정하며, 비어 있거나 형식이 잘못된 답변은 오류로 처리합니다. 질문은 공백이 아닌 최대 2,000자, 답변은 최대 600자입니다.

#### 설정과 인증

| 환경변수 | 기본값 / 설명 |
|---|---|
| `Gemini__ApiKey` | 필수. Google AI Studio에서 발급한 키 |
| `Gemini__Model` | `gemini-3.5-flash-lite`. AI Studio에서 해당 프로젝트에 사용 가능한 모델로 변경 가능 |
| `Gemini__TimeoutSeconds` | 30초 (1~120초 범위) |
| `DeskBuddy__DeviceApiKey` | 필수. 직접 정한 충분히 긴 장치 접속 키. Gemini 키와 별개 |
| `DeskBuddy__CharacterName` | `데스크버디` |
| `DeskBuddy__Personality` | 호기심 많고 다정한 책상 친구, 친근한 반말 |

Google API 키는 서버에만 보관합니다. ESP32·Swagger 요청에는 **`X-Device-Key`** 헤더로 장치 접속 키만 전달합니다. Swagger의 요청 입력란에 있는 `X-Device-Key`에 입력한 후 실행하세요. 서버 전체 DeskBuddy 요청은 분당 20회로 제한합니다 (컨테이너별 메모리 기준). 제공자의 모델별 무료 티어/사용량 제한은 별도로 적용됩니다.

설정이 없어도 기존 서비스는 시작됩니다. DeskBuddy를 호출할 때만 설정 누락 오류가 발생합니다. Google API 키나 장치 접속 키를 소스, Dockerfile, Git에 저장하지 마세요.

| HTTP | 의미 |
|---|---|
| 400 | 질문 누락·공백·길이 초과 또는 잘못된 JSON |
| 401 | 장치 접속 키 누락/불일치 |
| 413 | 요청 본문 16KB 초과 |
| 422 | 제공자가 답변을 차단함 |
| 429 | 이 서버의 요청 제한 또는 Gemini 사용량 초과 (`code`로 구분) |
| 502 | Gemini 연결·인증·모델·응답 형식 오류 |
| 503 | 서버 설정 누락/잘못된 모델 설정 |
| 504 | Gemini 응답 시간 초과 |

오류는 기존 `ErrorResponseDto` (`code`, `message`, `traceId`)를 사용합니다. 자동 모델 검증 400 응답은 ASP.NET Core의 ValidationProblemDetails입니다. 사용자 질문·답변과 제공자 오류 본문은 별도 로그나 DB에 저장하지 않습니다.

#### 로컬 테스트

PowerShell 예시 (실제 키는 자신의 터미널에서 입력):

```powershell
$env:Gemini__ApiKey = 'YOUR_GEMINI_API_KEY'
$env:DeskBuddy__DeviceApiKey = 'YOUR_OWN_LONG_RANDOM_DEVICE_KEY'
dotnet run
```

HTTP 요청 예시는 `SeinServices.Api.http`에 있습니다. API 호출 코드는 추가 SDK 없이 `HttpClient`와 `System.Text.Json`을 사용합니다. Google의 `generateContent` REST API와 JSON 응답 스키마를 사용합니다.

#### Docker

독립 테스트 컨테이너 (기존 8080 서비스와 충돌하지 않음):

```powershell
Copy-Item .env.example .env
# .env의 Gemini__ApiKey와 DeskBuddy__DeviceApiKey 값을 자신의 편집기에서 설정
docker compose -f compose.deskbuddy.yml up -d --build
```

Swagger: `http://localhost:8081/swagger` → **DeskBuddy**. 이 테스트 구성은 DB 스케줄러를 끄므로 DB 없이 실행 가능합니다. 중지: `docker compose -f compose.deskbuddy.yml down`.

기존 홈서버의 `seinservices-api`에 통합할 때는 기존 DB·Slack·스케줄러 설정을 유지하고, 위 환경변수만 컨테이너에 추가한 후 새 이미지로 재생성하면 됩니다. Gemini API 키는 이미지 빌드에 필요하지 않으며 실행 시 주입합니다. 개발용/로컬 설정 파일과 `.env`는 Docker 빌드 컨텍스트에서 제외됩니다.

현재 홈서버에는 `silee98/seinservices-api:deskbuddy-20261020` 이미지를 적용했습니다. `C:\workspace_silee\home-server\deskbuddy.env`에 `Gemini__ApiKey`를 입력한 다음, 해당 폴더에서 아래 명령을 실행하세요. 장치 접속 키는 이 파일의 `DeskBuddy__DeviceApiKey`에 생성되어 있습니다. Swagger 요청의 `X-Device-Key`에는 이 장치 접속 키를 입력합니다.

```powershell
cd C:\workspace_silee\home-server
docker compose up -d --no-deps --pull never --force-recreate seinservices-api
```

이 이미지는 로컬에서 빌드했으며 레지스트리에 푸시하지 않았습니다. `deskbuddy.env`의 비밀 값은 소스에 복사하지 마세요. 이전 API 이미지 `silee98/seinservices-api:1.0.10`도 로컬에 유지됩니다.

#### 검증

```powershell
dotnet build
dotnet test Tests/SeinServices.Api.Tests.csproj
```

테스트는 Gemini 통신을 모의 처리하며 실제 API 키나 유료 호출이 필요하지 않습니다. 실 API 호출 성공 여부는 자신의 키로 별도 확인해야 합니다.

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
