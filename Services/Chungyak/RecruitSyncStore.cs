using SeinServices.Api.Data.Chungyak;
using SeinServices.Api.Models.Chungyak.Internal;

namespace SeinServices.Api.Services.Chungyak
{
    /// <summary>
    /// ?ôÍ∏∞???Ä?•ÏÜå ?∏ÌÑ∞?òÏù¥?§Ïùò DBHelper Í∏∞Î∞ò Íµ¨ÌòÑÏ≤¥ÏûÖ?àÎã§.
    /// </summary>
    public class RecruitSyncStore : IRecruitSyncStore
    {
        private readonly DBHelper _dbHelper;

        public RecruitSyncStore(DBHelper dbHelper)
        {
            _dbHelper = dbHelper;
        }

        /// <inheritdoc />
        public DBHelper.RcvhomeSaveResult SaveRcvhome(TbRcvhomeUpsertDto entity)
        {
            return _dbHelper.SaveRcvhome(entity);
        }

        /// <inheritdoc />
        public bool SaveRcvhomeHist(string pblancId, string changeType)
        {
            return _dbHelper.SaveRcvhomeHist(pblancId, changeType);
        }

        /// <inheritdoc />
        public void SaveAccLog(string actionName, string resultCode, string? actionDesc = null)
        {
            _dbHelper.SaveAccLog(actionName, resultCode, actionDesc);
        }
    }
}

