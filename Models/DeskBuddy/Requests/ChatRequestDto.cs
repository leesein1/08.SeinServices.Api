using System.ComponentModel.DataAnnotations;

namespace SeinServices.Api.Models.DeskBuddy.Requests;

public sealed class ChatRequestDto
{
    [Required]
    [StringLength(2000, MinimumLength = 1)]
    public string Message { get; set; } = string.Empty;
}
