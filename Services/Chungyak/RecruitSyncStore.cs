using SeinServices.Api.Data.Chungyak;
using SeinServices.Api.Models.Chungyak.Internal;

namespace SeinServices.Api.Services.Chungyak
{
    /// <summary>
    /// RecruitSyncStore 관련 기능을 제공합니다.
    /// </summary>
    public class RecruitSyncStore : IRecruitSyncStore
    {
        private readonly DBHelper _dbHelper;

        public RecruitSyncStore(DBHelper dbHelper)
        {
            _dbHelper = dbHelper;
        }

        public DBHelper.RcvhomeSaveResult SaveRcvhome(TbRcvhomeUpsertDto entity)
        {
            return _dbHelper.SaveRcvhome(entity);
        }

        /// <summary>
        /// SaveRcvhomeHist 작업을 수행합니다.
        /// </summary>
        public bool SaveRcvhomeHist(string pblancId, string changeType)
        {
            return _dbHelper.SaveRcvhomeHist(pblancId, changeType);
        }

        /// <summary>
        /// SaveAccLog 작업을 수행합니다.
        /// </summary>
        public void SaveAccLog(string actionName, string resultCode, string? actionDesc = null)
        {
            _dbHelper.SaveAccLog(actionName, resultCode, actionDesc);
        }

        /// <summary>
        /// SaveAlarmLog 작업을 수행합니다.
        /// </summary>
        public void SaveAlarmLog(
            string alarmSource,
            int? subscribeIdx,
            string pblancId,
            string alarmType,
            DateTime? targetDate,
            string sendStatus,
            string? alarmTitle,
            string? alarmMessage,
            string? errorMessage = null)
        {
            _dbHelper.SaveAlarmLog(
                alarmSource,
                subscribeIdx,
                pblancId,
                alarmType,
                targetDate,
                sendStatus,
                alarmTitle,
                alarmMessage,
                errorMessage);
        }
    }
}

