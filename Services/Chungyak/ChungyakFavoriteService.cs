using SeinServices.Api.Data.Chungyak;
using SeinServices.Api.Models.Chungyak.Enums;
using SeinServices.Api.Models.Chungyak.Responses;

namespace SeinServices.Api.Services.Chungyak
{
    /// <summary>
    /// 청약 모집공고 즐겨찾기 등록/해제 기능을 처리합니다.
    /// </summary>
    public class ChungyakFavoriteService
    {
        private readonly DBHelper _dbHelper;

        public ChungyakFavoriteService(DBHelper dbHelper)
        {
            _dbHelper = dbHelper;
        }

        public FavoriteMutationResponseDto? AddFavorite(string pblancId)
        {
            if (string.IsNullOrWhiteSpace(pblancId))
            {
                return null;
            }

            var normalizedPblancId = pblancId.Trim();
            var region = _dbHelper.GetFavoriteRegion(normalizedPblancId);
            if (region is null)
            {
                return null;
            }

            var result = _dbHelper.SetFavorite(normalizedPblancId, true, region.Value.BrtcNm, region.Value.SignguNm);

            return new FavoriteMutationResponseDto
            {
                PblancId = normalizedPblancId,
                IsFavorite = true,
                Message = GetAddMessage(result)
            };
        }

        public FavoriteMutationResponseDto? RemoveFavorite(string pblancId)
        {
            if (string.IsNullOrWhiteSpace(pblancId))
            {
                return null;
            }

            var normalizedPblancId = pblancId.Trim();
            var region = _dbHelper.GetFavoriteRegion(normalizedPblancId);
            if (region is null)
            {
                return null;
            }

            var result = _dbHelper.SetFavorite(normalizedPblancId, false, region.Value.BrtcNm, region.Value.SignguNm);

            return new FavoriteMutationResponseDto
            {
                PblancId = normalizedPblancId,
                IsFavorite = false,
                Message = GetRemoveMessage(result)
            };
        }

        private static string GetAddMessage(FavoriteResult result)
        {
            return result switch
            {
                FavoriteResult.Added => "즐겨찾기에 등록했습니다.",
                FavoriteResult.AlreadyAdded => "이미 즐겨찾기에 등록되어 있습니다.",
                _ => "즐겨찾기 등록 결과를 확인할 수 없습니다."
            };
        }

        private static string GetRemoveMessage(FavoriteResult result)
        {
            return result switch
            {
                FavoriteResult.Removed => "즐겨찾기에서 해제했습니다.",
                FavoriteResult.AlreadyRemoved => "이미 즐겨찾기 해제 상태입니다.",
                _ => "즐겨찾기 해제 결과를 확인할 수 없습니다."
            };
        }
    }
}
