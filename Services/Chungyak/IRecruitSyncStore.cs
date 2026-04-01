using SeinServices.Api.Models.Chungyak.Internal;

namespace SeinServices.Api.Services.Chungyak
{
    /// <summary>
    /// ëª¨ì§‘ê³µê³  ?™ê¸°?????„ìš”???€?¥ì†Œ ?‘ì—… ê³„ì•½?…ë‹ˆ??
    /// </summary>
    public interface IRecruitSyncStore
    {
        /// <summary>
        /// ëª¨ì§‘ê³µê³ ë¥?MERGE ê¸°ë°˜?¼ë¡œ ?€?¥í•©?ˆë‹¤.
        /// </summary>
        /// <param name="entity">?€???€???”í‹°??/param>
        /// <returns>?€??ê²°ê³¼(? ê·œ/?˜ì •/ë³€ê²½ì—†??</returns>
        SeinServices.Api.Data.Chungyak.DBHelper.RcvhomeSaveResult SaveRcvhome(TbRcvhomeUpsertDto entity);

        /// <summary>
        /// ëª¨ì§‘ê³µê³  ë³€ê²??´ë ¥???€?¥í•©?ˆë‹¤.
        /// </summary>
        /// <param name="pblancId">ëª¨ì§‘ê³µê³  ê³ ìœ ë²ˆí˜¸</param>
        /// <param name="changeType">?´ë ¥ ?€??I/U)</param>
        /// <returns>?€???±ê³µ ?¬ë?</returns>
        bool SaveRcvhomeHist(string pblancId, string changeType);

        /// <summary>
        /// ë°°ì¹˜ ?¤í–‰ ë¡œê·¸ë¥??€?¥í•©?ˆë‹¤.
        /// </summary>
        /// <param name="actionName">?™ìž‘ ?´ë¦„</param>
        /// <param name="resultCode">ê²°ê³¼ ì½”ë“œ</param>
        /// <param name="actionDesc">?¤ëª…</param>
        void SaveAccLog(string actionName, string resultCode, string? actionDesc = null);
    }
}

