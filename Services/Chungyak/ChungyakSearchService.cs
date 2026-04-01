using System.Data;
using SeinServices.Api.Data.Chungyak;
using SeinServices.Api.Models.Chungyak.Requests;
using SeinServices.Api.Models.Chungyak.Responses;

namespace SeinServices.Api.Services.Chungyak
{
    /// <summary>
    /// 청약 모집공고 조회와 관련된 비즈니스 로직을 처리하는 서비스입니다.
    /// </summary>
    public class ChungyakSearchService
    {
        private readonly DBHelper _dbHelper;

        public ChungyakSearchService(DBHelper dbHelper)
        {
            _dbHelper = dbHelper;
        }

        public bool IsValidStatus(string? status)
        {
            if (string.IsNullOrWhiteSpace(status))
            {
                return true;
            }

            return status is "전체" or "접수예정" or "접수중" or "접수마감";
        }

        public bool IsValidDateRange(DateTime beginFrom, DateTime beginTo)
        {
            return beginFrom <= beginTo;
        }

        public List<RcvhomeResponseDto> GetRcvhomes(RcvhomesRequestDto request)
        {
            var dataTable = _dbHelper.GetRcvHome(
                request.Keyword ?? string.Empty,
                request.Status,
                request.BeginFrom,
                request.BeginTo,
                request.TodayStart,
                request.TodayEnd);

            return MapToResponseList(dataTable);
        }

        public List<RcvhomeResponseDto> GetFavoriteRcvhomes(RcvhomesRequestDto request)
        {
            var dataTable = _dbHelper.GetRcvHomeFav(
                request.Keyword ?? string.Empty,
                request.Status ?? string.Empty,
                request.BeginFrom,
                request.BeginTo);

            return MapToResponseList(dataTable);
        }

        public List<RcvhomeResponseDto> GetDeadlineSoonRcvhomes(RcvhomesRequestDto request)
        {
            return _dbHelper.GetRcvHomeD7(
                request.Keyword,
                request.Status,
                request.BeginFrom,
                request.BeginTo);
        }

        /// <summary>
        /// 모집공고 고유번호를 기준으로 상세 정보를 조회합니다.
        /// </summary>
        /// <param name="pblancId">모집공고 고유번호</param>
        /// <returns>상세 정보가 있으면 DTO를 반환하고, 없으면 null을 반환합니다.</returns>
        public RcvhomeDetailResponseDto? GetRcvhomeDetail(string pblancId)
        {
            if (string.IsNullOrWhiteSpace(pblancId))
            {
                return null;
            }

            return _dbHelper.GetRcvHomeDetail(pblancId.Trim());
        }

        private List<RcvhomeResponseDto> MapToResponseList(DataTable dataTable)
        {
            var response = new List<RcvhomeResponseDto>();

            foreach (DataRow row in dataTable.Rows)
            {
                response.Add(new RcvhomeResponseDto
                {
                    순서 = row.Field<long?>("순서") ?? 0,
                    고유번호 = row.Field<string>("고유번호") ?? string.Empty,
                    공고명 = row.Field<string>("공고명") ?? string.Empty,
                    단지명 = row.Field<string>("단지명") ?? string.Empty,
                    상태 = row.Field<string>("상태") ?? string.Empty,
                    접수시작일 = row.Field<DateTime?>("접수시작일"),
                    접수마감일 = row.Field<DateTime?>("접수마감일"),
                    접수기간 = row.Field<string>("접수기간") ?? string.Empty,
                    주소 = row.Field<string>("주소") ?? string.Empty,
                    공급유형 = row.Field<string>("공급유형") ?? string.Empty,
                    남은일수 = row.Field<string>("남은일수") ?? string.Empty,
                    URL = row.Field<string>("URL") ?? string.Empty,
                    즐겨찾기 = row.Field<bool?>("즐겨찾기") ?? false
                });
            }

            return response;
        }
    }
}

