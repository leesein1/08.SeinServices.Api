using SeinServices.Api.Models.Chungyak.Internal;

namespace SeinServices.Api.Services.Chungyak
{
    /// <summary>
    /// IRecruitSyncStore 계약을 정의합니다.
    /// </summary>
    public interface IRecruitSyncStore
    {
        SeinServices.Api.Data.Chungyak.DBHelper.RcvhomeSaveResult SaveRcvhome(TbRcvhomeUpsertDto entity);

        bool SaveRcvhomeHist(string pblancId, string changeType);

        void SaveAccLog(string actionName, string resultCode, string? actionDesc = null);
    }
}

