# SeinServices.Api

청약 모집공고 데이터를 조회/동기화/마감 처리하는 ASP.NET Core Web API입니다.

현재 운영 전략은 Azure Web App(F1)의 콜드 슬립 이슈를 고려해, 내부 스케줄러 대신 GitHub Actions Schedule이 API를 호출하는 구조를 기본으로 사용합니다.

## 1. 프로젝트 목적

- 청약 모집공고 조회 API 제공
- MyHome 외부 API 데이터를 로컬 DB에 동기화
- 마감 공고 상태 일괄 갱신
- 즐겨찾기(구독) 등록/해제 관리

## 2. 핵심 기능

### 조회

- 전체 모집공고 조회
- D-7(마감임박/접수중) 조회
- 모집공고 상세 조회
- 즐겨찾기 목록 조회

### 배치 실행

- 모집공고 동기화 1회 실행 (`/api/rcvhome-sync/run-once`)
- 마감 처리 1회 실행 (`/api/rcvhome-close/run-once`)
- 콜드슬립 완화용 웜업 (`/api/job-trigger/warmup`)

### 즐겨찾기

- 즐겨찾기 등록 (`POST /api/rcvhome-favorites/{pblancId}`)
- 즐겨찾기 해제 (`DELETE /api/rcvhome-favorites/{pblancId}`)

## 3. 아키텍처

```text
GitHub Actions Scheduler
        |
        v
Controllers (API contract + validation + error mapping)
        |
        v
Services (business rules / orchestration)
        |
        v
Data(DBHelper: SQL query, upsert, log)
        |
        v
Azure SQL (TB_RCVHOME, TB_SUBSCRIBE, TB_RCVHOME_HIST, TB_ACC_LOG)
```

## 4. 프로젝트 구조

```text
Controllers/
  BaseController.cs
  Chungyak/
    JobTriggerController.cs
    RcvhomeSearchController.cs
    RcvhomeFavoriteController.cs
    RcvhomeSyncController.cs
    RcvhomeCloseController.cs

Services/
  Chungyak/
    ChungyakSearchService.cs
    ChungyakFavoriteService.cs
    RecruitSyncService.cs
    RcvhomeCloseService.cs
    RecruitSyncStore.cs
    SlackNotifier.cs
  Schedules/
    RecruitSyncBackgroundService.cs
    RcvhomeCloseBackgroundService.cs

Data/
  Chungyak/
    DBHelper.cs
    DBHelper.*.cs

Models/
  Common/
  Chungyak/
    Requests/
    Responses/
    Internal/
    External/
    Enums/
```

## 5. 기술 스택과 선택 이유

- .NET 8 (ASP.NET Core Web API)
  - 선택 이유: LTS, 고성능, DI/HostedService/미들웨어 기반 운영 표준에 적합

- Microsoft.Data.SqlClient
  - 선택 이유: Azure SQL 연결 안정성, 파라미터 바인딩/트랜잭션 제어 등 SQL Server 친화성

- Swashbuckle (Swagger)
  - 선택 이유: API 계약 확인/테스트/협업 문서화 효율 향상

- Azure Web App + Azure SQL
  - 선택 이유: 배포 간편성, 운영 자동화, 관리형 DB 사용 가능

- GitHub Actions Schedule (외부 스케줄러)
  - 선택 이유: Web App F1 콜드 슬립 환경에서도 정시 실행 신뢰성 확보

## 6. API 엔드포인트 요약

### Search

- `GET /api/rcvhome-search/rcvhomes`
- `GET /api/rcvhome-search/deadline-soon`
- `GET /api/rcvhome-search/rcvhomes/{pblancId}`

### Favorite

- `GET /api/rcvhome-favorites`
- `POST /api/rcvhome-favorites/{pblancId}`
- `DELETE /api/rcvhome-favorites/{pblancId}`

### Job Trigger (보호 대상)

- `GET /api/job-trigger/warmup`
- `GET /api/rcvhome-sync/run-once`
- `GET /api/rcvhome-close/run-once`

위 3개 endpoint는 `X-Job-Key` 헤더가 필요합니다.

## 7. 스케줄 실행 전략 (현재 기본)

### 기본 원칙

- 인프로세스 스케줄러는 비활성화 기본값 사용
- 외부 GitHub Actions 스케줄이 API를 호출해서 배치 실행

### 호출 절차 (권장)

1. `GET /api/job-trigger/warmup`
2. 30초 대기 (`JobTrigger:WarmupDelaySeconds`)
3. `GET /api/rcvhome-sync/run-once` 또는 `GET /api/rcvhome-close/run-once`

## 8. 설정

### appsettings 핵심 키

- `ConnectionStrings:ChungyakDb`
- `MyHomeApi:*`
- `SlackApi:BaseUrl`
- `Schedulers:EnableInProcess` (기본 `false`)
- `JobTrigger:ApiKey`
- `JobTrigger:WarmupDelaySeconds` (기본 `30`)

### Azure App Service 환경변수 권장

- `JobTrigger__ApiKey`
- `JobTrigger__WarmupDelaySeconds`
- 운영 비밀값(DB/Slack/API Key 등)은 포털 환경변수에서 관리

## 9. 로컬 실행

```bash
dotnet restore
dotnet build
dotnet run
```

- 기본 실행 URL은 `Properties/launchSettings.json` 참조
- Swagger UI에서 엔드포인트 테스트 가능

## 10. 트러블슈팅 기록

### 10.1 Azure F1 콜드 슬립으로 인한 스케줄 누락 리스크

- 문제: F1 환경에서 앱 유휴 시 슬립 발생, 내부 `BackgroundService` 스케줄 누락 가능
- 원인: F1은 `Always On` 미지원으로 프로세스 상주 보장 불가
- 조치: GitHub Actions Schedule + warmup(30초) + run-once 호출 구조로 변경
- 결과: 앱 슬립 여부와 분리된 외부 스케줄 제어로 정시 실행 안정성 개선

### 10.2 run-once 엔드포인트 노출 위험

- 문제: 외부에서 run-once를 임의 호출할 수 있는 보안 리스크
- 조치: `X-Job-Key` 기반 인증 추가
- 결과: 승인된 트리거(GitHub Actions)만 배치 엔드포인트 호출 가능

## 11. 운영 체크리스트

- `Schedulers:EnableInProcess=false` 확인
- `JobTrigger__ApiKey`를 충분히 긴 랜덤 값으로 설정
- GitHub Actions에서 동일 키를 `X-Job-Key`로 전달
- 배치 호출 시 warmup -> 대기 -> run-once 순서 준수
- 장애 시 GitHub Actions/App Service/Application Insights 로그 함께 확인

## 12. GitHub Actions 스케줄 배치

이 저장소에는 무료 스케줄 실행을 위한 워크플로우가 포함되어 있습니다.

- 워크플로우 파일: `.github/workflows/scheduled-jobs.yml`
- 동작 방식:
  - `warmup` 호출
  - `30초 대기`
  - `run-once` 호출

### GitHub Secrets / Variables

- `WEBAPP_BASE_URL` (예: `https://<your-webapp>.azurewebsites.net`)
- `JOB_TRIGGER_API_KEY` (App Service의 `JobTrigger:ApiKey`와 동일값)
- `WARMUP_DELAY_SECONDS` (Repository Variable, 기본 30)

### 수동 실행

- Actions 탭에서 `Scheduled Jobs` 워크플로우를 `workflow_dispatch`로 실행 가능
- input `target` 값으로 `sync` 또는 `close` 선택
