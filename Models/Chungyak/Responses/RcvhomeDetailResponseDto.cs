namespace SeinServices.Api.Models.Chungyak.Responses
{
    /// <summary>
    /// RcvhomeDetailResponseDto 관련 기능을 제공합니다.
    /// </summary>
    public class RcvhomeDetailResponseDto
    {
        public string PblancId { get; set; } = string.Empty;

        public string NoticeName { get; set; } = string.Empty;

        public string ComplexName { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public string RawStatus { get; set; } = string.Empty;

        public DateTime? BeginDate { get; set; }

        public DateTime? EndDate { get; set; }

        public string DateRangeText { get; set; } = string.Empty;

        public string Address { get; set; } = string.Empty;

        public string BrtcName { get; set; } = string.Empty;

        public string SignguName { get; set; } = string.Empty;

        public string HouseTypeName { get; set; } = string.Empty;

        public string DdayText { get; set; } = string.Empty;

        public string Url { get; set; } = string.Empty;

        public bool IsFavorite { get; set; }

        public DateTime? AnnouncementDate { get; set; }
    }
}
