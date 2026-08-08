using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SeinServices.Api.Swagger
{
    public class SwaggerOperationDescriptionFilter : IOperationFilter
    {
        private static readonly Dictionary<string, (string Summary, string Description)> Descriptions = new()
        {
            ["GET api/rcvhome-search/rcvhomes"] = ("청약 공고 목록 조회", "청약홈 공고 데이터를 검색합니다. 키워드, 접수 상태, 날짜 범위, 오늘 시작/마감 조건을 조합해 프론트의 공고 목록 화면에 사용할 데이터를 반환합니다."),
            ["GET api/rcvhome-search/deadline-soon"] = ("마감 임박 청약 공고 조회", "마감일이 가까운 청약 공고를 조회합니다. 대시보드나 알림 후보를 보여줄 때 사용하는 조회 API입니다."),
            ["GET api/rcvhome-search/rcvhomes/{pblancId}"] = ("청약 공고 상세 조회", "공고 고유번호로 단일 청약 공고의 상세 정보를 조회합니다. 목록에서 공고를 선택했을 때 상세 패널 또는 상세 화면에 사용합니다."),
            ["GET api/rcvhome-favorites"] = ("즐겨찾기 청약 목록 조회", "사용자가 구독 또는 즐겨찾기한 청약 공고 목록을 조회합니다. 검색 조건을 함께 받아 관심 공고만 추려서 반환합니다."),
            ["POST api/rcvhome-favorites/{pblancId}"] = ("청약 공고 즐겨찾기 추가", "공고 고유번호를 기준으로 즐겨찾기 또는 구독 항목을 등록합니다. 이미 등록된 경우에는 중복 등록되지 않도록 처리합니다."),
            ["DELETE api/rcvhome-favorites/{pblancId}"] = ("청약 공고 즐겨찾기 삭제", "공고 고유번호를 기준으로 즐겨찾기 또는 구독 항목을 삭제합니다. 관심 공고 해제 버튼에서 사용합니다."),
            ["GET api/rcvhome-sync/run-once"] = ("청약 공고 수동 동기화", "외부 청약홈 API에서 최신 공고 데이터를 가져와 내부 DB에 저장합니다. 배치 스케줄과 별개로 관리자가 즉시 동기화를 실행할 때 사용합니다."),
            ["GET api/rcvhome-close/run-once"] = ("청약 마감 상태 수동 갱신", "마감일이 지난 청약 공고의 상태를 갱신합니다. 자동 스케줄 외에 수동으로 마감 처리를 실행할 때 사용합니다."),
            ["GET api/schedule-log"] = ("스케줄 실행 로그 조회", "청약 동기화, 마감 처리 등 백그라운드 작업의 실행 이력을 조회합니다. 운영 상태 확인과 장애 추적에 사용합니다."),
            ["GET api/schedule-log/last"] = ("최근 스케줄 실행 결과 조회", "작업 코드 기준으로 가장 최근 실행된 스케줄의 결과를 조회합니다. 프론트에서 마지막 동기화 시각을 보여줄 때 사용합니다."),
            ["GET api/alarm-log"] = ("구독 알림 발송 로그 조회", "청약 구독 알림 발송 이력을 조회합니다. 성공/실패 여부와 발송 기록을 운영자가 확인할 때 사용합니다."),
            ["GET api/subscribe-alarm-dispatch/run-once"] = ("구독 알림 수동 발송", "구독 조건에 맞는 청약 알림을 즉시 발송합니다. 정기 발송 스케줄과 별개로 운영자가 수동 트리거할 때 사용합니다."),
            ["GET api/job-trigger/warmup"] = ("백그라운드 작업 워밍업", "스케줄러 또는 외부 잡 트리거에서 API 컨테이너를 깨우고 초기 지연 시간을 반영하기 위한 운영용 엔드포인트입니다."),
            ["GET api/faultmon/faults"] = ("FaultMon 최근 고장 목록 조회", "KORFaultWeb DB의 최근 고장 접수 목록을 조회합니다. 기존 FaultMon 대시보드의 좌측/중앙 목록과 지도 표시 데이터로 사용합니다."),
            ["GET Fault/GetFaultList"] = ("FaultMon 최근 고장 목록 조회 - 기존 경로", "기존 FaultMon 프론트 호환을 위한 경로입니다. 내부 동작은 api/faultmon/faults와 동일합니다."),
            ["GET api/faultmon/stats/today"] = ("FaultMon 금일 처리 통계 조회", "오늘 발생한 고장 건수, 진행 중 건수, 완료 건수, 완료율을 조회합니다. 대시보드 상단 카운터에 사용합니다."),
            ["GET Fault/GetStatToday"] = ("FaultMon 금일 처리 통계 조회 - 기존 경로", "기존 FaultMon 프론트 호환을 위한 경로입니다. 내부 동작은 api/faultmon/stats/today와 동일합니다."),
            ["GET api/faultmon/faults/{incidentId}"] = ("FaultMon 고장 상세 조회", "IncidentID 기준으로 고장 접수 상세, 고장 코드 설명, 조치 안내 정보를 조회합니다. 목록에서 항목을 선택했을 때 상세 패널에 사용합니다."),
            ["GET Fault/GetFaultListDetail"] = ("FaultMon 고장 상세 조회 - 기존 경로", "기존 FaultMon 프론트 호환을 위한 경로입니다. IncidentID 쿼리 파라미터로 상세 정보를 조회합니다."),
            ["GET api/faultmon/faults/{incidentId}/popup"] = ("FaultMon 고장 상세 팝업 조회", "IncidentID 기준으로 팝업 화면에 필요한 고장 상세, 담당자 연락처, 차량번호, 담당자 당일 처리 건수를 조회합니다."),
            ["GET Fault/GetFaultListDetailPop"] = ("FaultMon 고장 상세 팝업 조회 - 기존 경로", "기존 FaultMon 프론트 호환을 위한 경로입니다. IncidentID 쿼리 파라미터로 팝업 상세 정보를 조회합니다.")
        };

        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var method = context.ApiDescription.HttpMethod?.ToUpperInvariant() ?? string.Empty;
            var path = context.ApiDescription.RelativePath?.Split('?')[0] ?? string.Empty;

            if (Descriptions.TryGetValue($"{method} {path}", out var description))
            {
                operation.Summary = description.Summary;
                operation.Description = description.Description;
            }

            foreach (var parameter in operation.Parameters)
            {
                if (string.Equals(parameter.Name, "IncidentID", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(parameter.Name, "incidentId", StringComparison.OrdinalIgnoreCase))
                {
                    parameter.Description = "FaultMon 고장 접수 고유 ID입니다.";
                }
                else if (string.Equals(parameter.Name, "pblancId", StringComparison.OrdinalIgnoreCase))
                {
                    parameter.Description = "청약 공고 고유번호입니다.";
                }
            }
        }
    }
}
