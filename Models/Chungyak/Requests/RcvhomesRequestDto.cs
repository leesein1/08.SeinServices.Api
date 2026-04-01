using System.ComponentModel.DataAnnotations;

namespace SeinServices.Api.Models.Chungyak.Requests
{
    /// <summary>
    /// RcvhomesRequestDto 관련 기능을 제공합니다.
    /// </summary>
    public class RcvhomesRequestDto
    {
        public string? Keyword { get; set; }

        public string? Status { get; set; }

        [Required]
        public DateTime BeginFrom { get; set; }

        [Required]
        public DateTime BeginTo { get; set; }

        public bool TodayStart { get; set; }

        public bool TodayEnd { get; set; }
    }
}

