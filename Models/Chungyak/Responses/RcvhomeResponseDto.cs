using System;

namespace SeinServices.Api.Models.Chungyak.Responses
{
    /// <summary>
    /// 화면 표시용 모집공고 응답 DTO입니다.
    /// </summary>
    public class RcvhomeResponseDto
    {
        public long 순서 { get; set; }
        public string 고유번호 { get; set; } = string.Empty;
        public string 공고명 { get; set; } = string.Empty;
        public string 단지명 { get; set; } = string.Empty;
        /// <summary>접수 상태 (예: 접수중, 접수마감, 접수예정)</summary>
        public string 상태 { get; set; } = string.Empty;
        public DateTime? 접수시작일 { get; set; }
        public DateTime? 접수마감일 { get; set; }
        public string 접수기간 { get; set; } = string.Empty;
        public string 주소 { get; set; } = string.Empty;
        public string 공급유형 { get; set; } = string.Empty;
        public string 남은일수 { get; set; } = string.Empty;
        public string URL { get; set; } = string.Empty;
        public bool 즐겨찾기 { get; set; }
    }
}

