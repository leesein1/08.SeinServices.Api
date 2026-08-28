# SeinServices.Api

![.NET](https://img.shields.io/badge/.NET-8.0-purple)
![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-Web%20API-512BD4)
![SQL Server](https://img.shields.io/badge/SQL%20Server-Microsoft.Data.SqlClient-CC2927)
![Docker](https://img.shields.io/badge/Docker-Container-2496ED)

여러 개인 프로젝트에서 공통으로 사용하는 ASP.NET Core Web API 서버입니다.

청약 서비스 백엔드로 시작했고, 이후 FaultMon의 조회·실시간 처리까지 이관했습니다. 현재는 두 프로젝트의 DB 접근, 스케줄 작업, API, SignalR을 한 서버에서 관리합니다.

<p align="center">
  <a href="https://api.silee.net/swagger">
    <img src="https://img.shields.io/badge/Swagger-API%20문서%20보기-85EA2D?style=for-the-badge&logo=swagger&logoColor=black" alt="Swagger API 문서 보기" />
  </a>
</p>

---

## 연결 프로젝트

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
React Frontends
      │
      ├─ REST API
      └─ SignalR
      │
      ▼
SeinServices.Api
  ├─ Controllers/Chungyak
  ├─ Controllers/FaultMon
  ├─ Services/Chungyak
  ├─ Services/FaultMon
  └─ Services/Schedules
      │
      ├─ Chungyak DB
      └─ FaultMon DB
```

청약과 FaultMon은 Controller, Service, Data, Model을 도메인별로 나눴습니다.

Swagger 문서도 `청약 서비스`, `FaultMon 고장 관제`로 분리되어 있습니다.

---

## 백그라운드 작업

청약 쪽은 `BackgroundService`로 공고 동기화, 마감 처리, 구독 알림을 실행할 수 있습니다.

FaultMon은 `FaultMonConnectionTracker`에서 SignalR Connection ID를 관리합니다. 활성 연결이 없으면 반복 작업을 건너뛰고, 접속자가 있을 때 `PROC_SCH_REPEAT_INSERT` 실행 후 `Signal_FLTLIST` 이벤트를 전송합니다.

필요한 작업은 `run-once` API로도 실행할 수 있습니다.

---

## 배포

Dockerfile을 기준으로 이미지를 빌드합니다.

`main` 브랜치나 버전 태그가 push되면 GitHub Actions에서 Docker Hub로 이미지를 배포합니다. 태그는 `latest`, semver, commit SHA 기준으로 생성합니다.

환경별 DB 연결 문자열, API Key, Slack 설정은 환경변수에서 주입합니다.

---

## 운영 중 수정한 내용

### Azure App Service 휴면

초기 Azure App Service 무료 환경에서는 유휴 상태에서 앱이 멈춰 내부 스케줄러 실행을 보장하기 어려웠습니다.

GitHub Actions에서 `warmup → 대기 → run-once` 방식으로 우회했고, 이후 API를 홈서버 Docker 환경으로 옮겨 내부 `BackgroundService`를 사용할 수 있도록 했습니다.

### UTC / KST

Azure SQL과 실행 환경의 시간 기준 차이 때문에 청약 마감 처리와 로그 시간이 어긋났습니다. 날짜 비교와 로그 저장 기준을 KST로 맞췄습니다.

### FaultMon 실시간 반복 작업

관제 화면 사용자가 없어도 DB 작업이 반복되는 문제를 막기 위해 SignalR 활성 연결 수를 확인한 뒤 프로시저를 실행하도록 변경했습니다.

### 기존 FaultMon 경로 유지

FaultMon 프론트를 API 분리 구조로 옮기는 동안 기존 호출 경로도 함께 매핑해 이전 화면과 신규 API를 동시에 사용할 수 있도록 했습니다.

### 검색 조건 처리

FaultMon 누적 이력 검색에 Keyword, 접수번호, 차량번호, 고객명, 담당자, 상태, 기간 조건을 추가했습니다. `Page`, `PageSize`를 받아 저장 프로시저에 전달하며 PageSize는 1~500 범위로 제한합니다.

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
