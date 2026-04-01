using System.ComponentModel.DataAnnotations;

namespace SeinServices.Api.Models.Chungyak.Requests
{
    /// <summary>
    /// 모집공고 목록 조회 요청 정보를 담는 DTO입니다.
    /// </summary>
    public class RcvhomesRequestDto
    {
        /// <summary>검색 키워드</summary>
        public string? Keyword { get; set; }

        /// <summary>모집 상태 조건</summary>
        public string? Status { get; set; }

        /// <summary>조회 시작일</summary>
        [Required]
        public DateTime BeginFrom { get; set; }

        /// <summary>조회 종료일</summary>
        [Required]
        public DateTime BeginTo { get; set; }

        /// <summary>오늘 시작하는 공고만 조회할지 여부</summary>
        public bool TodayStart { get; set; }

        /// <summary>오늘 마감되는 공고만 조회할지 여부</summary>
        public bool TodayEnd { get; set; }
    }
}

