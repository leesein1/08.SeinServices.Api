using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Any;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SeinServices.Api.Swagger
{
    public class SwaggerOperationDescriptionFilter : IOperationFilter
    {
        private static readonly Dictionary<string, (string Summary, string Description)> Descriptions = new()
        {
            ["GET api/deskbuddy/device/ws"] = ("ESP32 화면 WebSocket 연결", "장치에서 wss://api.silee.net/api/deskbuddy/device/ws 로 연결하고 업그레이드 요청의 X-Device-Key 헤더에 장치 키를 넣습니다. 정상 연결은 HTTP 101이며 device.ready 이벤트가 전송됩니다. 이후 POST /api/deskbuddy/chat 성공 시 연결된 모든 장치에 type=chat.answer, id, reply, emotion, searchUsed를 전달합니다. 일반 HTTP GET은 400입니다. Swagger Try it out은 WebSocket 클라이언트가 아니므로 Postman WebSocket 또는 ESP32로 시험하세요. 연결이 끊긴 동안의 답변은 보관하거나 재전송하지 않습니다."),
            ["POST api/deskbuddy/chat"] = ("책상 친구에게 질문하기", "X-Device-Key에 서버의 DeskBuddy 장치 접속 키를 입력합니다. message는 공백이 아닌 1~2000자이며, reply(최대 600자)와 emotion(neutral/happy/sad/angry/surprised)을 반환합니다. 호출마다 독립적으로 응답하며 대화 이력은 저장하지 않습니다. 서버 전체에서 분당 20회로 제한하고, Gemini 사용량 초과도 429로 반환합니다. 설정 누락은 503, 제공자 오류는 502, 시간 초과는 504입니다."),
            ["GET api/rcvhome-search/rcvhomes"] = ("청약 공고 목록 조회", "청약 공고 데이터를 검색합니다. 키워드, 접수 상태, 날짜 범위, 오늘 시작/마감 조건을 조합한 프론트의 공고 목록 화면에서 사용합니다."),
            ["GET api/rcvhome-search/deadline-soon"] = ("마감 임박 청약 공고 조회", "마감일이 가까운 청약 공고를 조회합니다. 대시보드나 알림 후보를 보여줄 때 사용하는 조회 API입니다."),
            ["GET api/rcvhome-search/rcvhomes/{pblancId}"] = ("청약 공고 상세 조회", "공고 고유번호로 단일 청약 공고의 상세 정보를 조회합니다."),
            ["GET api/rcvhome-favorites"] = ("즐겨찾기 청약 목록 조회", "사용자가 구독 또는 즐겨찾기한 청약 공고 목록을 조회합니다."),
            ["POST api/rcvhome-favorites/{pblancId}"] = ("청약 공고 즐겨찾기 추가", "공고 고유번호를 기준으로 즐겨찾기 또는 구독 항목을 등록합니다."),
            ["DELETE api/rcvhome-favorites/{pblancId}"] = ("청약 공고 즐겨찾기 제거", "공고 고유번호를 기준으로 즐겨찾기 또는 구독 항목을 제거합니다."),
            ["GET api/rcvhome-sync/run-once"] = ("청약 공고 수동 동기화", "청약홈 API에서 최신 공고 데이터를 가져와 내부 DB에 저장합니다."),
            ["GET api/rcvhome-close/run-once"] = ("청약 마감 상태 수동 갱신", "마감일이 지난 청약 공고의 상태를 갱신합니다."),
            ["GET api/schedule-log"] = ("스케줄 실행 로그 조회", "청약 동기화, 마감 처리 등 백그라운드 작업 실행 이력을 조회합니다."),
            ["GET api/schedule-log/last"] = ("최근 스케줄 실행 결과 조회", "작업 코드 기준으로 가장 최근 스케줄 실행 결과를 조회합니다."),
            ["GET api/alarm-log"] = ("구독 알림 발송 로그 조회", "청약 구독 알림 발송 이력을 조회합니다."),
            ["GET api/subscribe-alarm-dispatch/run-once"] = ("구독 알림 수동 발송", "구독 조건에 맞는 청약 알림을 즉시 발송합니다."),
            ["GET api/job-trigger/warmup"] = ("백그라운드 작업 워밍업", "스케줄러 또는 외부 트리거에서 API 컨테이너를 깨우기 위한 운영용 엔드포인트입니다."),
            ["GET api/faultmon/faults"] = ("FaultMon 최근 고장 목록 조회", "KORFaultWeb DB의 최근 고장 접수 목록을 조회합니다. Home 실시간 관제 화면에서 사용합니다."),
            ["GET Fault/GetFaultList"] = ("FaultMon 최근 고장 목록 조회 - 기존 경로", "기존 FaultMon 프론트 호환 경로입니다. 내부 동작은 api/faultmon/faults와 같습니다."),
            ["GET api/faultmon/faults/search"] = ("FaultMon 누적 고장 이력 검색", "RcvFault에 쌓인 전체 고장 이력을 검색합니다. 통합검색, 접수번호, 차량번호, 접수자, 담당자, 상태 복수선택, 발생 시간 범위를 조건으로 사용합니다. Search 화면 전용 API입니다."),
            ["GET api/faultmon/stats/today"] = ("FaultMon 금일 처리 통계 조회", "오늘 발생한 고장 건수, 진행 중 건수, 완료 건수, 완료율을 조회합니다."),
            ["GET Fault/GetStatToday"] = ("FaultMon 금일 처리 통계 조회 - 기존 경로", "기존 FaultMon 프론트 호환 경로입니다. 내부 동작은 api/faultmon/stats/today와 같습니다."),
            ["GET api/faultmon/faults/{incidentId}"] = ("FaultMon 고장 상세 조회", "IncidentID 기준으로 고장 접수 상세, 고장 코드 설명, 조치 안내 정보를 조회합니다."),
            ["GET Fault/GetFaultListDetail"] = ("FaultMon 고장 상세 조회 - 기존 경로", "기존 FaultMon 프론트 호환 경로입니다. IncidentID 쿼리 파라미터로 상세 정보를 조회합니다."),
            ["GET api/faultmon/faults/{incidentId}/popup"] = ("FaultMon 고장 상세 팝업 조회", "IncidentID 기준으로 팝업 화면에 필요한 고장 상세, 담당자 연락처, 차량번호, 담당자 당일 처리 건수를 조회합니다."),
            ["GET Fault/GetFaultListDetailPop"] = ("FaultMon 고장 상세 팝업 조회 - 기존 경로", "기존 FaultMon 프론트 호환 경로입니다. IncidentID 쿼리 파라미터로 팝업 상세 정보를 조회합니다.")
        };

        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var method = context.ApiDescription.HttpMethod?.ToUpperInvariant() ?? string.Empty;
            var path = context.ApiDescription.RelativePath?.Split('?')[0] ?? string.Empty;

            if (Descriptions.TryGetValue($"{method} {path}", out var description))
            {
                operation.Summary = description.Summary;
                operation.Description = description.Description;
                if (method == "POST" && path == "api/deskbuddy/chat")
                {
                    operation.Summary = "책상 친구에게 질문하기 (필요 시 웹 검색)";
                    operation.Description += " Gemini가 먼저 검색 필요 여부를 판단하고 필요한 경우에만 Tavily 기본 검색을 최대 1회 호출한 뒤 결과로 답합니다. 예: 오늘 인천 날씨 어때~? searchUsed는 검색 경로 사용 여부(5분 캐시 적중 포함), sources는 검색에서 찾은 출처 목록입니다. 검색이 필요 없으면 false와 빈 목록입니다. 검색 결과가 부족하면 확인하지 못했다고 답합니다. Tavily 키 누락은 503, 검색 한도 초과는 429입니다.";
                    operation.Description += " 답변 생성에 성공하면 같은 장치 키를 사용하는 연결된 OLED 장치에도 WebSocket으로 답변을 전달합니다. 장치가 오프라인이어도 HTTP 응답은 정상 반환되며, 화면 수신 확인을 기다리지는 않습니다.";
                    operation.Description += " 기본 지역은 대한민국, 시간대는 Asia/Seoul(KST, UTC+09:00)입니다. 상대 날짜는 한국 시각으로 해석하고 지정한 과거/미래 날짜는 유지합니다. Tavily에 country=south korea, language=ko와 검색 기준 한국 시각을 함께 보냅니다. 한국 자료를 우선하며 한국 사이트만으로 제한하지는 않습니다. 별도 시간/지역 요청 파라미터는 필요 없습니다.";
                }
            }

            if (path is "api/deskbuddy/chat" or "api/deskbuddy/device/ws")
            {
                var deviceKey = operation.Parameters.FirstOrDefault(parameter => parameter.Name == "X-Device-Key");
                if (deviceKey != null)
                {
                    deviceKey.Required = true;
                    deviceKey.Description = "서버의 DeskBuddy__DeviceApiKey 값입니다. Google Gemini 키와 다릅니다.";
                }
            }
            if (method == "POST" && path == "api/deskbuddy/chat")
            {
                if (operation.RequestBody?.Content.TryGetValue("application/json", out var requestMedia) == true)
                    requestMedia.Example = new OpenApiObject { ["message"] = new OpenApiString("안녕! 오늘 좀 피곤하다.") };
                if (operation.Responses.TryGetValue("200", out var response)
                    && response.Content.TryGetValue("application/json", out var responseMedia))
                    responseMedia.Example = new OpenApiObject
                    {
                        ["reply"] = new OpenApiString("오늘 많이 힘들었구나. 잠깐 쉬면서 이야기하자!"),
                        ["emotion"] = new OpenApiString("sad"),
                        ["searchUsed"] = new OpenApiBoolean(false),
                        ["sources"] = new OpenApiArray()
                    };
            }

            foreach (var parameter in operation.Parameters)
            {
                if (string.Equals(parameter.Name, "IncidentID", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(parameter.Name, "incidentId", StringComparison.OrdinalIgnoreCase))
                {
                    parameter.Description = "FaultMon 고장 접수 고유 ID입니다.";
                }
                else if (string.Equals(parameter.Name, "keyword", StringComparison.OrdinalIgnoreCase))
                {
                    parameter.Description = "차량, 고장명, 고장 내용, 접수 번호, 담당자, 위치를 함께 찾는 통합 검색어입니다.";
                }
                else if (string.Equals(parameter.Name, "statuses", StringComparison.OrdinalIgnoreCase))
                {
                    parameter.Description = "FaultMon 상태값 목록입니다. 예: 0,1,2";
                }
                else if (string.Equals(parameter.Name, "setTimeFrom", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(parameter.Name, "setTimeTo", StringComparison.OrdinalIgnoreCase))
                {
                    parameter.Description = "고장 발생 시간 검색 범위입니다.";
                }
                else if (string.Equals(parameter.Name, "page", StringComparison.OrdinalIgnoreCase))
                {
                    parameter.Description = "검색 결과 페이지 번호입니다. 기본값은 1입니다.";
                }
                else if (string.Equals(parameter.Name, "pageSize", StringComparison.OrdinalIgnoreCase))
                {
                    parameter.Description = "한 번에 조회할 검색 결과 건수입니다. 최대 500건입니다.";
                }
                else if (string.Equals(parameter.Name, "pblancId", StringComparison.OrdinalIgnoreCase))
                {
                    parameter.Description = "청약 공고 고유번호입니다.";
                }
            }
        }
    }
}
