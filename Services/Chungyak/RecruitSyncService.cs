using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using SeinServices.Api.Data.Chungyak;
using SeinServices.Api.Models.Chungyak.External;
using SeinServices.Api.Models.Chungyak.Internal;
using SeinServices.Api.Models.Chungyak.Responses;
using static SeinServices.Api.Data.Chungyak.DBHelper;

namespace SeinServices.Api.Services.Chungyak
{
    /// <summary>
    /// RecruitSyncService 관련 기능을 제공합니다.
    /// </summary>
    public class RecruitSyncService
    {
        private static readonly SemaphoreSlim RunLock = new(1, 1);
        private const byte SyncJobCode = 1;

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly DBHelper _dbHelper;
        private readonly IRecruitSyncStore _store;
        private readonly ISlackNotifier _slackNotifier;
        private readonly ILogger<RecruitSyncService> _logger;

        public RecruitSyncService(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            DBHelper dbHelper,
            IRecruitSyncStore store,
            ISlackNotifier slackNotifier,
            ILogger<RecruitSyncService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _dbHelper = dbHelper;
            _store = store;
            _slackNotifier = slackNotifier;
            _logger = logger;
        }

        /// <summary>
        /// RunOnceAsync 작업을 수행합니다.
        /// </summary>
        public async Task<SyncRunResponseDto> RunOnceAsync(CancellationToken cancellationToken)
        {
            const string actionName = "SyncRecruitListToDbAsync";
            var startedAtUtc = DateTime.UtcNow;

            if (!await RunLock.WaitAsync(0, cancellationToken))
            {
                var skipMessage = "Sync job is already running.";
                try
                {
                    SaveScheduleLog(SyncJobCode, "SKIPPED", startedAtUtc, skipMessage);
                }
                catch (Exception logEx)
                {
                    return new SyncRunResponseDto
                    {
                        Success = false,
                        Message = $"{skipMessage} Schedule log save failed: {logEx.Message}"
                    };
                }

                return new SyncRunResponseDto
                {
                    Success = false,
                    Skipped = true,
                    Message = skipMessage
                };
            }

            try
            {
                var baseUrl = _configuration["MyHomeApi:BaseUrl"];
                var serviceKey = _configuration["MyHomeApi:ServiceKey"];
                var brtcCode = _configuration["MyHomeApi:BrtcCode"] ?? "28";
                var numOfRows = _configuration["MyHomeApi:NumOfRows"] ?? "100";

                if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(serviceKey))
                {
                    _logger.LogWarning("MyHomeApi settings are missing. Skip sync job.");
                    TrySaveAccLog(actionName, "00", "MyHomeApi settings are missing.");
                    SaveScheduleLog(SyncJobCode, "FAIL", startedAtUtc, "MyHomeApi settings are missing.");
                    return new SyncRunResponseDto
                    {
                        Success = false,
                        Message = "MyHomeApi settings are missing."
                    };
                }

                var requestUrl = $"{baseUrl}?serviceKey={serviceKey}&brtcCode={brtcCode}&numOfRows={numOfRows}&pageNo=1";
                var httpClient = _httpClientFactory.CreateClient();
                using var response = await httpClient.GetAsync(requestUrl, cancellationToken);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var dto = JsonSerializer.Deserialize<MyHomeRecruitDto>(
                    json,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        NumberHandling = JsonNumberHandling.AllowReadingFromString
                    });

                var header = dto?.response?.header;
                var items = dto?.response?.body?.item;

                if (header is null || items is null)
                {
                    _logger.LogWarning("MyHome API response is invalid.");
                    TrySaveAccLog(actionName, "00", "Invalid API response.");
                    SaveScheduleLog(SyncJobCode, "FAIL", startedAtUtc, "Invalid API response.");
                    return new SyncRunResponseDto
                    {
                        Success = false,
                        Message = "Invalid API response."
                    };
                }

                if (header.resultCode == "03")
                {
                    _logger.LogInformation("MyHome API returned no data.");
                    TrySaveAccLog(actionName, "10", "No data.");
                    SaveScheduleLog(SyncJobCode, "SUCCESS", startedAtUtc, "No data.");
                    return new SyncRunResponseDto
                    {
                        Success = true,
                        Message = "No data.",
                        TotalCount = 0
                    };
                }

                if (header.resultCode != "00")
                {
                    var apiError = $"API error: {header.resultCode}/{header.resultMsg}";
                    _logger.LogWarning(apiError);
                    TrySaveAccLog(actionName, "00", apiError);
                    SaveScheduleLog(SyncJobCode, "FAIL", startedAtUtc, TruncateNote(apiError));
                    return new SyncRunResponseDto
                    {
                        Success = false,
                        Message = apiError
                    };
                }

                var insertCount = 0;
                var updateCount = 0;
                var noneCount = 0;
                var errorCount = 0;

                foreach (var item in items)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        var entity = MapToRcvhome(item);
                        var result = _store.SaveRcvhome(entity);

                        switch (result)
                        {
                            case RcvhomeSaveResult.Inserted:
                                insertCount++;
                                _store.SaveRcvhomeHist(entity.PblancId, "I");
                                await _slackNotifier.SendAsync(BuildSlackMessage(item, true), cancellationToken);
                                break;
                            case RcvhomeSaveResult.Updated:
                                updateCount++;
                                _store.SaveRcvhomeHist(entity.PblancId, "U");
                                await _slackNotifier.SendAsync(BuildSlackMessage(item, false), cancellationToken);
                                break;
                            default:
                                noneCount++;
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        errorCount++;
                        _logger.LogError(ex, "Failed to upsert item. pblancId={PblancId}", item.pblancId);
                    }
                }

                _logger.LogInformation(
                    "Recruit sync completed. total={Total}, insert={Insert}, update={Update}, none={None}, error={Error}",
                    items.Count,
                    insertCount,
                    updateCount,
                    noneCount,
                    errorCount);

                TrySaveAccLog(actionName, "10", $"I:{insertCount},U:{updateCount},N:{noneCount},E:{errorCount}");
                SaveScheduleLog(
                    SyncJobCode,
                    "SUCCESS",
                    startedAtUtc,
                    $"신규 {insertCount}건, 변경 {updateCount}건, 유지 {noneCount}건, 오류 {errorCount}건");

                return new SyncRunResponseDto
                {
                    Success = true,
                    Message = "Sync completed.",
                    TotalCount = items.Count,
                    InsertCount = insertCount,
                    UpdateCount = updateCount,
                    NoneCount = noneCount,
                    ErrorCount = errorCount
                };
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Recruit sync cancelled.");
                SaveScheduleLog(SyncJobCode, "FAIL", startedAtUtc, "Sync cancelled.");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Recruit sync failed.");
                TrySaveAccLog(actionName, "00", ex.Message);

                try
                {
                    SaveScheduleLog(SyncJobCode, "FAIL", startedAtUtc, TruncateNote(ex.Message));
                }
                catch (Exception logEx)
                {
                    _logger.LogError(logEx, "Failed to write TB_SCH_LOG for sync fail case.");
                    return new SyncRunResponseDto
                    {
                        Success = false,
                        Message = $"{ex.Message} | Schedule log save failed: {logEx.Message}"
                    };
                }

                return new SyncRunResponseDto
                {
                    Success = false,
                    Message = ex.Message
                };
            }
            finally
            {
                RunLock.Release();
            }
        }

        private void TrySaveAccLog(string actionName, string resultCode, string? actionDesc)
        {
            try
            {
                _store.SaveAccLog(actionName, resultCode, actionDesc);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to write TB_ACC_LOG.");
            }
        }

        private void SaveScheduleLog(byte jobCode, string status, DateTime startedAtUtc, string? scheduleNote)
        {
            _dbHelper.SaveScheduleLog(
                jobCode,
                status,
                startedAtUtc,
                DateTime.UtcNow,
                TruncateNote(scheduleNote));
        }

        private static string? TruncateNote(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return text;
            }

            return text.Length <= 200 ? text : text[..200];
        }

        private static string BuildSlackMessage(MyHomeRecruitItem item, bool isInsert)
        {
            var title = isInsert ? "[신규 모집공고 등록]" : "[모집공고 변경]";
            var link = string.IsNullOrWhiteSpace(item.pcUrl) ? item.url : item.pcUrl;

            return
                $"{title}\n" +
                $"- {item.pblancNm} ({item.pblancId})\n" +
                $"- 단지명: {item.hsmpNm}\n" +
                $"- 지역: {item.brtcNm} {item.signguNm}\n" +
                $"- 상태: {item.sttusNm}\n" +
                $"- 모집공고일: {ToDateDash(item.rcritPblancDe)}\n" +
                $"- 청약기간: {ToDateDash(item.beginDe)} ~ {ToDateDash(item.endDe)}\n" +
                $"- 당첨자발표일: {ToDateDash(item.przwnerPresnatnDe)}\n" +
                $"- 링크: {link}";
        }

        private static TbRcvhomeUpsertDto MapToRcvhome(MyHomeRecruitItem src)
        {
            return new TbRcvhomeUpsertDto
            {
                PblancId = src.pblancId,
                HouseSn = src.houseSn,
                SttusNm = src.sttusNm,
                PblancNm = src.pblancNm,
                SuplyInsttNm = src.suplyInsttNm,
                HouseTyNm = src.houseTyNm,
                SuplyTyNm = src.suplyTyNm,
                BeforePblancId = src.beforePblancId,
                RcritPblancDe = src.rcritPblancDe,
                PrzwnerPresnatnDe = src.przwnerPresnatnDe,
                SuplyHoCo = src.suplyHoCo,
                TotHshldCo = ParseNullableInt(src.totHshldCo),
                SumSuplyCo = src.sumSuplyCo,
                RentGtn = src.rentGtn,
                Enty = src.enty,
                Prtpay = src.prtpay,
                Surlus = src.surlus,
                MtRntchrg = src.mtRntchrg,
                BeginDe = src.beginDe,
                EndDe = src.endDe,
                Refrnc = src.refrnc,
                Url = src.url,
                PcUrl = src.pcUrl,
                MobileUrl = src.mobileUrl,
                HsmpNm = src.hsmpNm,
                BrtcNm = src.brtcNm,
                SignguNm = src.signguNm,
                FullAdres = src.fullAdres,
                RnCodeNm = src.rnCodeNm,
                RefrnLegaldongNm = src.refrnLegaldongNm,
                Pnu = src.pnu,
                HeatMthdNm = src.heatMthdNm
            };
        }

        private static int? ParseNullableInt(JsonElement? value)
        {
            if (!value.HasValue)
            {
                return null;
            }

            var element = value.Value;
            if (element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out var number))
            {
                return number;
            }

            if (element.ValueKind == JsonValueKind.String
                && int.TryParse(element.GetString(), out var parsed))
            {
                return parsed;
            }

            return null;
        }

        private static string ToDateDash(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return "-";
            }

            var s = raw.Trim();
            if (DateTime.TryParseExact(s, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
            {
                return dt.ToString("yyyy-MM-dd");
            }

            return s;
        }
    }
}
