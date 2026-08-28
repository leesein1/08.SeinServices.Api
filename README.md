# 🔌 SeinServices.Api

![.NET](https://img.shields.io/badge/.NET-8.0-purple)
![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-Web%20API-512BD4)
![SQL Server](https://img.shields.io/badge/SQL%20Server-Microsoft.Data.SqlClient-CC2927)
![Docker](https://img.shields.io/badge/Docker-Container-2496ED)

> 여러 개인 프로젝트에서 필요한 서버 기능을 한곳에서 관리하기 위해 만든 ASP.NET Core 기반 공용 백엔드 API입니다.

`SeinServices.Api`는 하나의 화면이나 하나의 서비스만을 위한 API로 시작하지 않았습니다.  
청약 관리 서비스의 데이터 수집·조회·스케줄링을 담당하는 서버로 시작했고, 이후 FaultMon의 백엔드 기능까지 이관하면서 **여러 프로젝트의 API, DB 접근, 백그라운드 작업, 실시간 통신을 담당하는 공용 서버**로 확장했습니다.

<p align="center">
  <a href="https://api.silee.net/swagger">
    <img src="https://img.shields.io/badge/Swagger-API%20문서%20보기-85EA2D?style=for-the-badge&logo=swagger&logoColor=black" alt="Swagger API 문서 보기" />
  </a>
</p>

---

## 💛 왜 따로 만들었을까?

프론트 프로젝트마다 외부 API 호출, DB 접근, 스케줄 작업 같은 서버 기능을 직접 포함시키기 시작하면 기능이 늘어날수록 역할이 섞이고 같은 운영 문제를 프로젝트마다 다시 처리해야 합니다.

그래서 청약 서비스를 만들면서 먼저 **프론트와 데이터 처리 영역을 분리한 별도 API 서버**를 구성했습니다.

이후 기존 MVC 구조로 구현했던 FaultMon을 React 기반 프론트로 개편하면서 FaultMon의 조회·실시간 처리 역시 이 서버로 옮겼습니다.

현재는 프로젝트별 프론트는 화면과 사용자 인터랙션에 집중하고, 이 서버가 다음 영역을 담당합니다.

- 외부 공공 API 데이터 수집 및 내부 DB 동기화
- 청약 공고 조회·즐겨찾기·마감 상태·알림 처리
- 백그라운드 스케줄 실행 및 실행 로그 관리
- FaultMon 고장 관제 데이터 조회·검색
- SignalR 기반 FaultMon 실시간 갱신
- 프로젝트별 DB 연결과 저장 프로시저 호출

---

## 🧩 연결 프로젝트

### 🏡 Chungyak Manager

React 기반 청약 관리 프론트에서 사용하는 백엔드입니다.

- 청약 공고 검색 / 상세 조회
- 외부 공공 API → 내부 DB 동기화
- 즐겨찾기 및 알림 예정 데이터 관리
- 마감 상태 자동 갱신
- Slack 알림 발송 및 발송 로그
- 스케줄 실행 로그 / 최근 동기화 상태 제공

**Frontend**  
[leesein1/chungyak_manage_web](https://github.com/leesein1/chungyak_manage_web)

### 🚨 FaultMon

기존 MVC 통합 구조에서 React + API 구조로 개편하면서 백엔드 기능을 이관했습니다.

- 최근 고장 목록 / 금일 통계 / 상세 조회
- 누적 고장 이력 조건 검색 및 페이징
- 기존 FaultMon 경로 호환
- SignalR Hub를 통한 실시간 연결
- 접속자가 있을 때 반복 프로시저 실행 후 변경 이벤트 전파

**Frontend**  
[leesein1/FaultMon-Front](https://github.com/leesein1/FaultMon-Front)

---

## 🎯 이 API가 담당하는 역할

### Chungyak

- 청약 공고 목록 / 상세 / 마감 임박 조회
- 즐겨찾기 등록·해제 및 목록 조회
- 공공 API 데이터 동기화
- 마감 공고 상태 갱신
- 구독 알림 발송 및 Slack 연동
- 스케줄 / 알림 실행 로그 조회
- 수동 실행용 Job Trigger 제공

### FaultMon

- 최근 고장 접수 목록 조회
- 금일 처리 현황 통계
- 고장 상세 / 팝업 상세 조회
- 누적 고장 이력 다중 조건 검색
- SignalR 연결 및 접속자 수 관리
- 관제 화면 접속 상태에 따른 반복 DB 작업 및 실시간 이벤트 전송

### Common / Operation

- 공통 오류 응답 처리
- 도메인별 Swagger 문서 분리
- 환경변수 기반 DB / API Key / Slack 설정
- Docker 이미지 빌드 및 Docker Hub 배포
- CORS 설정

---

## 🛠️ 기술 요약

### Backend
`C#` `.NET 8` `ASP.NET Core Web API`

### Data
`Microsoft.Data.SqlClient` `SQL Server` `Azure SQL` `Stored Procedure`

### Realtime / Background
`SignalR` `BackgroundService` `PeriodicTimer`

### External Integration
`공공데이터 API` `Slack Webhook`

### Documentation / Operation
`Swagger` `Swashbuckle` `Docker` `Docker Hub` `GitHub Actions`

---

## 🧩 설계 방향

### 1️⃣ 프론트 기능과 서버 역할 분리

React 프론트가 외부 API와 DB 처리까지 직접 담당하지 않도록 데이터 수집, 저장, 조회, 스케줄링을 API 서버로 분리했습니다.

프론트는 HTTP API와 SignalR을 통해 필요한 데이터를 전달받도록 구성했습니다.

### 2️⃣ 하나의 서버 안에서도 도메인 분리

공용 API라고 해서 모든 기능을 한 구조에 섞지 않고 `Chungyak`, `FaultMon` 단위로 Controller / Service / Data / Model을 구분했습니다.

```text
Controllers/
├─ Chungyak/
└─ FaultMon/

Services/
├─ Chungyak/
├─ FaultMon/
└─ Schedules/

Data/
├─ Chungyak/
└─ FaultMon/
```

### 3️⃣ HTTP 조회 + 백그라운드 작업을 같은 서버에서 관리

청약 데이터 동기화, 마감 처리, 구독 알림처럼 사용자의 요청과 관계없이 실행되어야 하는 작업은 `BackgroundService`로 분리했습니다.

필요한 경우 동일 로직을 `run-once` API로도 실행할 수 있어 자동 스케줄과 수동 실행을 함께 지원합니다.

### 4️⃣ 필요한 곳에만 실시간 통신 적용

FaultMon은 관제 화면의 변경을 즉시 전달해야 하므로 일반 REST 조회와 별도로 SignalR Hub를 구성했습니다.

접속 Connection을 추적하고, 실제 관제 화면 접속자가 있을 때만 일정 주기로 저장 프로시저를 실행한 뒤 `Signal_FLTLIST` 이벤트를 전달하도록 구성했습니다.

### 5️⃣ API 문서를 도메인별로 분리

청약과 FaultMon API가 한 Swagger 문서에 섞이지 않도록 API Explorer Group을 사용해 문서를 분리했습니다.

Swagger UI에서 **청약 서비스 / FaultMon 고장 관제** 문서를 선택해 실제 운영 API 계약을 확인할 수 있습니다.

👉 [https://api.silee.net/swagger](https://api.silee.net/swagger)

### 6️⃣ 개발 환경 의존 → 컨테이너 기반 운영

초기에는 Azure App Service 환경에서 운영했지만, 이후 Docker 이미지를 기준으로 실행할 수 있도록 변경했습니다.

`main` 브랜치와 버전 태그가 push되면 GitHub Actions가 이미지를 빌드하고 Docker Hub에 `latest`, 버전, commit SHA 기준 태그로 배포합니다.

---

## 🔄 동작 구조

```text
┌─────────────────────┐       ┌─────────────────────┐
│ Chungyak React      │       │ FaultMon React      │
│ Frontend            │       │ Frontend            │
└──────────┬──────────┘       └──────────┬──────────┘
           │ REST API                    │ REST / SignalR
           └──────────────┬──────────────┘
                          ▼
                ┌───────────────────┐
                │ SeinServices.Api  │
                │ ASP.NET Core 8    │
                └─────────┬─────────┘
                          │
          ┌───────────────┼────────────────┐
          ▼               ▼                ▼
   Chungyak Service   FaultMon Service   BackgroundService
          │               │                │
          ▼               ▼                ├─ 공고 동기화
    Chungyak DB        FaultMon DB         ├─ 마감 처리
          │               │                ├─ Slack 알림
          │               │                └─ FaultMon 반복 작업
          │               │
          ▼               ▼
   Public Data API   Stored Procedures

                          │
                          ▼
                    SignalR Hub
                          │
                          ▼
                   FaultMon Clients
```

---

## 🔄 개발과 운영 구조 변화

이 API는 처음부터 현재 구조로 만들어진 것이 아니라 실제 운영 과정에서 필요한 기능을 추가하면서 확장했습니다.

- **2026-04-01** — 청약 공고 조회·동기화·마감 처리를 위한 API 서버 시작
- **2026-04-02** — UTC/KST 차이로 발생한 마감 누락과 로그 시간 문제 수정
- **2026-04-03 ~ 04-16** — 스케줄 실행 로그, 즐겨찾기 기반 알림, Slack 발송 로그와 자동 발송 추가
- **2026-07-04 ~ 07-05** — Docker 실행 환경과 Docker Hub 자동 이미지 배포 구성
- **2026-07-05** — 청약 알림 스케줄을 서버 내부에서도 실행할 수 있도록 `BackgroundService` 추가
- **2026-08-08** — FaultMon 백엔드를 공용 API로 이관하고 청약 / FaultMon Swagger 문서 분리
- **2026-08-08** — FaultMon SignalR Hub, 접속자 추적, 반복 스케줄러 추가
- **2026-08-28** — FaultMon 누적 고장 이력 다중 조건 검색 / 페이징 API 추가

---

## 🔍 트러블슈팅

### Azure Free 환경의 Sleep과 스케줄 실행

초기에는 Azure App Service Free 환경에서 API를 운영했습니다.

유휴 상태에서 애플리케이션이 Sleep 상태로 전환될 수 있어 `BackgroundService`만으로 정기 작업 실행을 보장하기 어려웠고, 이를 우회하기 위해 GitHub Actions에서 먼저 warm-up 요청을 보낸 뒤 실제 작업 API를 호출하도록 구성했습니다.

이후 홈서버에서 Docker 컨테이너로 API를 상시 실행할 수 있게 되면서 서버 내부 스케줄링을 사용할 수 있는 구조로 확장했습니다. 외부 트리거 방식도 `run-once` API와 Workflow 형태로 남겨 필요할 때 사용할 수 있도록 했습니다.

### UTC / KST 차이로 인한 마감 처리 누락

Azure SQL과 실행 환경의 시간 기준이 UTC인 상태에서 날짜를 단순 비교하면서 한국시간 자정 이후에도 전날로 판단되는 구간이 생겼습니다.

마감 처리와 공고 상태 판단에 `DATEADD(hour, 9, GETUTCDATE())` 기준을 적용하고, 로그와 이력 저장 시간 역시 KST 기준으로 맞춰 날짜 경계에서도 동일하게 처리되도록 수정했습니다.

### 외부 공공 API Query Parameter 처리

공공 API 호출 시 검색 조건에 포함되는 문자열이 URL에 그대로 들어가면서 특수문자와 공백이 있는 경우 요청이 정상적으로 전달되지 않을 수 있었습니다.

Query Parameter를 URL Encoding한 뒤 요청하도록 수정해 입력값에 따른 호출 오류를 줄였습니다.

### FaultMon 실시간 작업의 불필요한 반복 실행

FaultMon의 반복 DB 작업은 관제 화면이 열려 있을 때 의미가 있지만 사용자가 없는 상황에서도 계속 실행하면 불필요한 DB 작업이 됩니다.

SignalR Connection ID를 `ConcurrentDictionary`로 관리하고 활성 연결 수가 0이면 반복 프로시저 실행을 건너뛰도록 구성했습니다. 접속자가 있을 때만 작업 후 SignalR 이벤트를 전송합니다.

### 기존 FaultMon 경로와 신규 API 구조 병행

FaultMon 프론트를 점진적으로 분리하는 과정에서 기존 MVC 호출 경로를 한 번에 제거하면 기존 화면과의 호환 문제가 발생할 수 있었습니다.

신규 `/api/faultmon/...` 경로와 기존 `/Fault/...` 경로를 함께 매핑해 백엔드를 먼저 이관하면서도 기존 호출을 유지할 수 있도록 구성했습니다.

---

## 👤 현재 운영 범위

현재 `SeinServices.Api`는 **개인 프로젝트에서 실제 사용하는 공용 백엔드 서버**입니다.

청약 서비스와 FaultMon은 서로 다른 목적과 DB를 사용하지만, 개인 프로젝트마다 별도 API 서버를 반복해서 운영하기보다 공통 운영 환경을 공유하고 코드 내부에서는 도메인을 분리하는 방식을 사용하고 있습니다.

인증·회원 시스템을 제공하는 범용 SaaS 백엔드를 목표로 한 프로젝트는 아니며, 현재 연결된 프로젝트에서 필요한 서버 기능을 안정적으로 제공하는 범위에 집중하고 있습니다.

---

## 🌱 현재 상태

- [x] 청약 공고 조회 / 상세 / 마감 임박 검색
- [x] 공공 API 데이터 동기화
- [x] 즐겨찾기 / 구독 알림 / Slack 연동
- [x] 스케줄 및 알림 로그 관리
- [x] BackgroundService 기반 정기 작업
- [x] Docker / Docker Hub 배포
- [x] FaultMon 백엔드 API 이관
- [x] FaultMon 누적 이력 검색 / 페이징
- [x] SignalR 기반 FaultMon 실시간 갱신
- [x] 청약 / FaultMon Swagger 문서 분리

---

## ✨ 마무리

처음에는 청약 서비스에서 필요한 서버 기능을 분리하기 위해 시작했습니다.

이후 실제 운영 과정에서 스케줄링과 시간대 문제를 수정하고 Docker 배포 구조를 추가했으며, FaultMon 개편 과정에서는 기존 백엔드까지 이관하면서 **여러 개인 프로젝트의 서버 기능을 담당하는 공용 ASP.NET Core API**로 확장했습니다.

현재는 기능을 한곳에 단순히 모으는 것보다, **공통 운영 환경 안에서 프로젝트별 역할을 구분하고 필요한 API·배치·실시간 기능을 각각 관리하는 구조**로 유지하고 있습니다.

---

<details>
<summary><b>API / 개발 / 배포 참고</b></summary>
<br/>

### Swagger

- 운영 문서: https://api.silee.net/swagger
- 청약 서비스 / FaultMon 고장 관제 문서 분리

### Local Run

```bash
dotnet restore
dotnet build
dotnet run
```

### Docker

```bash
docker build -t seinservices-api .
docker run -p 8080:8080 seinservices-api
```

### 주요 환경 설정

```text
ConnectionStrings__ChungyakDb
ConnectionStrings__FaultMonDb
MyHomeApi__ServiceKey
SlackApi__BaseUrl
JobTrigger__ApiKey
Schedulers__EnableInProcess
FaultMon__EnableSignalRScheduler
FaultMon__RepeatInsertIntervalSeconds
Cors__AllowedOrigins__0
```

DB Connection String, API Key, Slack 주소 등의 비밀값은 저장소에 직접 저장하지 않고 실행 환경의 환경변수로 주입합니다.

### 주요 Endpoint

정확한 Request / Response와 현재 Endpoint는 운영 Swagger에서 확인할 수 있습니다.

- `/api/rcvhome-search/*`
- `/api/rcvhome-favorites/*`
- `/api/rcvhome-sync/*`
- `/api/rcvhome-close/*`
- `/api/schedule-log/*`
- `/api/alarm-log/*`
- `/api/faultmon/*`
- `/hubs/faultmon`

</details>
