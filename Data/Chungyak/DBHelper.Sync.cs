using System.Data;
using Microsoft.Data.SqlClient;
using SeinServices.Api.Models.Chungyak.Internal;

namespace SeinServices.Api.Data.Chungyak
{
    /// <summary>
    /// DBHelper 관련 기능을 제공합니다.
    /// </summary>
    public partial class DBHelper
    {
        /// <summary>
        /// RcvhomeSaveResult 상태 값을 정의합니다.
        /// </summary>
        public enum RcvhomeSaveResult
        {
            None,
            Inserted,
            Updated
        }

        /// <summary>
        /// SaveRcvhome 작업을 수행합니다.
        /// </summary>
        public RcvhomeSaveResult SaveRcvhome(TbRcvhomeUpsertDto e)
        {
            ArgumentNullException.ThrowIfNull(e);

            using var conn = CreateConnection();
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandType = CommandType.Text;
            cmd.CommandText = @"
                DECLARE @MergeActions TABLE (Action NVARCHAR(10));

                MERGE TB_RCVHOME AS T
                USING (
                    SELECT
                        @PBLANC_ID            AS PBLANC_ID,
                        @HOUSE_SN             AS HOUSE_SN,
                        @STTUS_NM             AS STTUS_NM,
                        @PBLANC_NM            AS PBLANC_NM,
                        @SUPLY_INSTT_NM       AS SUPLY_INSTT_NM,
                        @HOUSE_TY_NM          AS HOUSE_TY_NM,
                        @BEFORE_PBLANC_ID     AS BEFORE_PBLANC_ID,
                        @RCRIT_PBLANC_DE      AS RCRIT_PBLANC_DE,
                        @PRZWNER_PRESNATN_DE  AS PRZWNER_PRESNATN_DE,
                        @REFRNC               AS REFRNC,
                        @URL                  AS URL,
                        @PC_URL               AS PC_URL,
                        @MOBILE_URL           AS MOBILE_URL,
                        @HSMP_NM              AS HSMP_NM,
                        @BRTC_NM              AS BRTC_NM,
                        @SIGNGU_NM            AS SIGNGU_NM,
                        @FULL_ADRES           AS FULL_ADRES,
                        @RN_CODE_NM           AS RN_CODE_NM,
                        @REFRN_LEGALDONG_NM   AS REFRN_LEGALDONG_NM,
                        @PNU                  AS PNU,
                        @HEAT_MTHD_NM         AS HEAT_MTHD_NM,
                        @SUM_SUPLY_CO         AS SUM_SUPLY_CO,
                        @ENTY                 AS ENTY,
                        @PRTPAY               AS PRTPAY,
                        @SURLUS               AS SURLUS,
                        @BEGIN_DE             AS BEGIN_DE,
                        @END_DE               AS END_DE
                ) AS S
                ON (T.PBLANC_ID = S.PBLANC_ID AND T.HOUSE_SN = S.HOUSE_SN AND T.PNU = S.PNU AND T.BRTC_NM = S.BRTC_NM AND T.SIGNGU_NM = S.SIGNGU_NM)

                WHEN MATCHED AND (
                    ISNULL(T.STTUS_NM, '')                  <> ISNULL(S.STTUS_NM, '')
                    OR ISNULL(T.PBLANC_NM, '')              <> ISNULL(S.PBLANC_NM, '')
                    OR ISNULL(T.SUPLY_INSTT_NM, '')         <> ISNULL(S.SUPLY_INSTT_NM, '')
                    OR ISNULL(T.HOUSE_TY_NM, '')            <> ISNULL(S.HOUSE_TY_NM, '')
                    OR ISNULL(T.BEFORE_PBLANC_ID, '')       <> ISNULL(S.BEFORE_PBLANC_ID, '')
                    OR ISNULL(T.RCRIT_PBLANC_DE, '1900-01-01') <> ISNULL(S.RCRIT_PBLANC_DE, '1900-01-01')
                    OR ISNULL(T.PRZWNER_PRESNATN_DE, '1900-01-01') <> ISNULL(S.PRZWNER_PRESNATN_DE, '1900-01-01')
                    OR ISNULL(T.REFRNC, '')                 <> ISNULL(S.REFRNC, '')
                    OR ISNULL(T.URL, '')                    <> ISNULL(S.URL, '')
                    OR ISNULL(T.PC_URL, '')                 <> ISNULL(S.PC_URL, '')
                    OR ISNULL(T.MOBILE_URL, '')             <> ISNULL(S.MOBILE_URL, '')
                    OR ISNULL(T.HSMP_NM, '')                <> ISNULL(S.HSMP_NM, '')
                    OR ISNULL(T.BRTC_NM, '')                <> ISNULL(S.BRTC_NM, '')
                    OR ISNULL(T.SIGNGU_NM, '')              <> ISNULL(S.SIGNGU_NM, '')
                    OR ISNULL(T.FULL_ADRES, '')             <> ISNULL(S.FULL_ADRES, '')
                    OR ISNULL(T.RN_CODE_NM, '')             <> ISNULL(S.RN_CODE_NM, '')
                    OR ISNULL(T.REFRN_LEGALDONG_NM, '')     <> ISNULL(S.REFRN_LEGALDONG_NM, '')
                    OR ISNULL(T.PNU, '')                    <> ISNULL(S.PNU, '')
                    OR ISNULL(T.HEAT_MTHD_NM, '')           <> ISNULL(S.HEAT_MTHD_NM, '')
                    OR ISNULL(T.SUM_SUPLY_CO, 0)            <> ISNULL(S.SUM_SUPLY_CO, 0)
                    OR ISNULL(T.ENTY, 0)                    <> ISNULL(S.ENTY, 0)
                    OR ISNULL(T.PRTPAY, 0)                  <> ISNULL(S.PRTPAY, 0)
                    OR ISNULL(T.SURLUS, 0)                  <> ISNULL(S.SURLUS, 0)
                    OR ISNULL(T.BEGIN_DE, '1900-01-01')     <> ISNULL(S.BEGIN_DE, '1900-01-01')
                    OR ISNULL(T.END_DE, '1900-01-01')       <> ISNULL(S.END_DE, '1900-01-01')
                ) THEN
                    UPDATE SET
                        T.STTUS_NM             = S.STTUS_NM,
                        T.PBLANC_NM            = S.PBLANC_NM,
                        T.SUPLY_INSTT_NM       = S.SUPLY_INSTT_NM,
                        T.HOUSE_TY_NM          = S.HOUSE_TY_NM,
                        T.BEFORE_PBLANC_ID     = S.BEFORE_PBLANC_ID,
                        T.RCRIT_PBLANC_DE      = S.RCRIT_PBLANC_DE,
                        T.PRZWNER_PRESNATN_DE  = S.PRZWNER_PRESNATN_DE,
                        T.REFRNC               = S.REFRNC,
                        T.URL                  = S.URL,
                        T.PC_URL               = S.PC_URL,
                        T.MOBILE_URL           = S.MOBILE_URL,
                        T.HSMP_NM              = S.HSMP_NM,
                        T.BRTC_NM              = S.BRTC_NM,
                        T.SIGNGU_NM            = S.SIGNGU_NM,
                        T.FULL_ADRES           = S.FULL_ADRES,
                        T.RN_CODE_NM           = S.RN_CODE_NM,
                        T.REFRN_LEGALDONG_NM   = S.REFRN_LEGALDONG_NM,
                        T.PNU                  = S.PNU,
                        T.HEAT_MTHD_NM         = S.HEAT_MTHD_NM,
                        T.SUM_SUPLY_CO         = S.SUM_SUPLY_CO,
                        T.ENTY                 = S.ENTY,
                        T.PRTPAY               = S.PRTPAY,
                        T.SURLUS               = S.SURLUS,
                        T.BEGIN_DE             = S.BEGIN_DE,
                        T.END_DE               = S.END_DE,
                        T.UPDATED_AT           = GETDATE()

                WHEN NOT MATCHED THEN
                    INSERT (
                        PBLANC_ID, HOUSE_SN, STTUS_NM, PBLANC_NM, SUPLY_INSTT_NM, HOUSE_TY_NM, BEFORE_PBLANC_ID,
                        RCRIT_PBLANC_DE, PRZWNER_PRESNATN_DE, REFRNC, URL, PC_URL, MOBILE_URL, HSMP_NM, BRTC_NM, SIGNGU_NM,
                        FULL_ADRES, RN_CODE_NM, REFRN_LEGALDONG_NM, PNU, HEAT_MTHD_NM, SUM_SUPLY_CO, ENTY, PRTPAY, SURLUS,
                        BEGIN_DE, END_DE, CREATED_AT, UPDATED_AT, CLS_YN
                    )
                    VALUES (
                        S.PBLANC_ID, S.HOUSE_SN, S.STTUS_NM, S.PBLANC_NM, S.SUPLY_INSTT_NM, S.HOUSE_TY_NM, S.BEFORE_PBLANC_ID,
                        S.RCRIT_PBLANC_DE, S.PRZWNER_PRESNATN_DE, S.REFRNC, S.URL, S.PC_URL, S.MOBILE_URL, S.HSMP_NM, S.BRTC_NM, S.SIGNGU_NM,
                        S.FULL_ADRES, S.RN_CODE_NM, S.REFRN_LEGALDONG_NM, S.PNU, S.HEAT_MTHD_NM, S.SUM_SUPLY_CO, S.ENTY, S.PRTPAY, S.SURLUS,
                        S.BEGIN_DE, S.END_DE, GETDATE(), GETDATE(), 'N'
                    )
                OUTPUT $action INTO @MergeActions(Action);

                SELECT TOP 1 Action FROM @MergeActions;";

            cmd.Parameters.AddWithValue("@PBLANC_ID", e.PblancId);
            cmd.Parameters.AddWithValue("@HOUSE_SN", e.HouseSn);
            cmd.Parameters.AddWithValue("@STTUS_NM", (object?)e.SttusNm ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@PBLANC_NM", (object?)e.PblancNm ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@SUPLY_INSTT_NM", (object?)e.SuplyInsttNm ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@HOUSE_TY_NM", (object?)e.HouseTyNm ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@BEFORE_PBLANC_ID", (object?)e.BeforePblancId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@RCRIT_PBLANC_DE", (object?)e.RcritPblancDe ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@PRZWNER_PRESNATN_DE", (object?)e.PrzwnerPresnatnDe ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@REFRNC", (object?)e.Refrnc ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@URL", (object?)e.Url ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@PC_URL", (object?)e.PcUrl ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@MOBILE_URL", (object?)e.MobileUrl ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@HSMP_NM", (object?)e.HsmpNm ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@BRTC_NM", (object?)e.BrtcNm ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@SIGNGU_NM", (object?)e.SignguNm ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@FULL_ADRES", (object?)e.FullAdres ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@RN_CODE_NM", (object?)e.RnCodeNm ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@REFRN_LEGALDONG_NM", (object?)e.RefrnLegaldongNm ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@PNU", (object?)e.Pnu ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@HEAT_MTHD_NM", (object?)e.HeatMthdNm ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@SUM_SUPLY_CO", e.SumSuplyCo);
            cmd.Parameters.AddWithValue("@ENTY", e.Enty);
            cmd.Parameters.AddWithValue("@PRTPAY", e.Prtpay);
            cmd.Parameters.AddWithValue("@SURLUS", e.Surlus);
            cmd.Parameters.AddWithValue("@BEGIN_DE", (object?)e.BeginDe ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@END_DE", (object?)e.EndDe ?? DBNull.Value);

            var action = cmd.ExecuteScalar() as string;
            if (string.Equals(action, "INSERT", StringComparison.OrdinalIgnoreCase))
            {
                return RcvhomeSaveResult.Inserted;
            }

            if (string.Equals(action, "UPDATE", StringComparison.OrdinalIgnoreCase))
            {
                return RcvhomeSaveResult.Updated;
            }

            return RcvhomeSaveResult.None;
        }

        /// <summary>
        /// SaveRcvhomeHist 작업을 수행합니다.
        /// </summary>
        public bool SaveRcvhomeHist(string pblancId, string changeType)
        {
            if (string.IsNullOrWhiteSpace(pblancId))
            {
                throw new ArgumentException("PBLANC_ID is required.", nameof(pblancId));
            }

            if (string.IsNullOrWhiteSpace(changeType))
            {
                throw new ArgumentException("changeType is required.", nameof(changeType));
            }

            using var conn = CreateConnection();
            using var cmd = conn.CreateCommand();

            cmd.CommandText = @"
                INSERT INTO dbo.TB_RCVHOME_HIST
                (
                    PBLANC_ID,
                    HIST_ID,
                    CHANGE_TYPE,
                    CREATED_AT
                )
                SELECT
                    @PBLANC_ID,
                    ISNULL(MAX(HIST_ID), 0) + 1,
                    @CHANGE_TYPE,
                    SYSDATETIME()
                FROM dbo.TB_RCVHOME_HIST
                WHERE PBLANC_ID = @PBLANC_ID;";

            cmd.Parameters.Add("@PBLANC_ID", SqlDbType.NVarChar, 20).Value = pblancId.Trim();
            cmd.Parameters.Add("@CHANGE_TYPE", SqlDbType.NVarChar, 50).Value = changeType.Trim().ToUpperInvariant();

            conn.Open();
            return cmd.ExecuteNonQuery() == 1;
        }

        /// <summary>
        /// SaveAccLog 작업을 수행합니다.
        /// </summary>
        public void SaveAccLog(string actionName, string resultCode, string? actionDesc = null)
        {
            if (string.IsNullOrWhiteSpace(actionName))
            {
                throw new ArgumentNullException(nameof(actionName));
            }

            if (string.IsNullOrWhiteSpace(resultCode))
            {
                throw new ArgumentNullException(nameof(resultCode));
            }

            using var conn = CreateConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO TB_ACC_LOG (
                    ACTION_NM,
                    ACTION_DESC,
                    RESULT_CD,
                    CREATED_AT
                )
                VALUES (
                    @ACTION_NM,
                    @ACTION_DESC,
                    @RESULT_CD,
                    GETDATE()
                );";

            cmd.Parameters.AddWithValue("@ACTION_NM", actionName);
            cmd.Parameters.AddWithValue("@ACTION_DESC", (object?)actionDesc ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@RESULT_CD", resultCode);

            conn.Open();
            cmd.ExecuteNonQuery();
        }
    }
}

