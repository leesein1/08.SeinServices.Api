namespace SeinServices.Api.Models.Chungyak.Responses
{
    /// <summary>
    /// Favorite add/remove mutation response.
    /// </summary>
    public class FavoriteMutationResponseDto
    {
        public string PblancId { get; set; } = string.Empty;

        public bool IsFavorite { get; set; }

        public string Message { get; set; } = string.Empty;
    }
}
