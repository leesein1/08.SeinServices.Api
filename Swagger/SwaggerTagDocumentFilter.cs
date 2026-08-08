using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SeinServices.Api.Swagger
{
    public class SwaggerTagDocumentFilter : IDocumentFilter
    {
        public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
        {
            swaggerDoc.Tags = swaggerDoc.Info.Title.Contains("FaultMon", StringComparison.Ordinal)
                ? new List<OpenApiTag>
                {
                    new()
                    {
                        Name = "FaultMon 고장 관제",
                        Description = "FaultMon 대시보드에서 사용하는 고장 목록, 금일 처리 통계, 상세 정보, 팝업 상세 조회 API입니다."
                    }
                }
                : new List<OpenApiTag>
                {
                    new()
                    {
                        Name = "청약 서비스",
                        Description = "청약홈 공고 조회, 즐겨찾기, 동기화, 마감 처리, 구독 알림, 운영 로그 조회 API입니다."
                    }
                };
        }
    }
}
