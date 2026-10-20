using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.RateLimiting;
using SeinServices.Api.Hubs.FaultMon;
using SeinServices.Api.Swagger;
using SeinServices.Api.Services.DeskBuddy;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers(options =>
{
    options.Conventions.Add(new ApiExplorerGroupConvention());
});
builder.Services.AddHttpClient();
builder.Services.Configure<GeminiOptions>(builder.Configuration.GetSection("Gemini"));
builder.Services.Configure<TavilyOptions>(builder.Configuration.GetSection("Tavily"));
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient<TavilyClient>(client => client.Timeout = Timeout.InfiniteTimeSpan);
builder.Services.Configure<DeskBuddyOptions>(builder.Configuration.GetSection("DeskBuddy"));
builder.Services.AddHttpClient<GeminiClient>(client => client.Timeout = Timeout.InfiniteTimeSpan);
builder.Services.AddScoped<DeskBuddyService>();
builder.Services.AddSingleton<DeskBuddyDeviceConnections>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("deskbuddy", limiter =>
    {
        limiter.PermitLimit = 20;
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.QueueLimit = 0;
        limiter.AutoReplenishment = true;
    });
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
            context.HttpContext.Response.Headers.RetryAfter = Math.Ceiling(retryAfter.TotalSeconds).ToString(System.Globalization.CultureInfo.InvariantCulture);
        await context.HttpContext.Response.WriteAsJsonAsync(new SeinServices.Api.Models.Common.ErrorResponseDto
        {
            Code = "DESKBUDDY_RATE_LIMITED",
            Message = "DeskBuddy 요청이 많습니다. 잠시 후 다시 시도해주세요.",
            TraceId = context.HttpContext.TraceIdentifier
        }, cancellationToken);
    };
});
builder.Services.AddSignalR();
builder.Services.AddCors(options =>
{
    options.AddPolicy("FaultMonCors", policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();

        if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins);
        }
        else
        {
            policy.SetIsOriginAllowed(_ => true);
        }

        policy.AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddScoped<SeinServices.Api.Data.Chungyak.DBHelper>();
builder.Services.AddScoped<SeinServices.Api.Data.FaultMon.FaultMonDbHelper>();
builder.Services.AddScoped<SeinServices.Api.Data.FaultMon.FaultMonRepository>();
builder.Services.AddSingleton<SeinServices.Api.Services.FaultMon.FaultMonConnectionTracker>();
builder.Services.AddScoped<SeinServices.Api.Services.Chungyak.ChungyakSearchService>();
builder.Services.AddScoped<SeinServices.Api.Services.Chungyak.ChungyakFavoriteService>();
builder.Services.AddScoped<SeinServices.Api.Services.Chungyak.ScheduleLogService>();
builder.Services.AddScoped<SeinServices.Api.Services.Chungyak.AlarmLogService>();
builder.Services.AddScoped<SeinServices.Api.Services.FaultMon.FaultMonService>();
builder.Services.AddScoped<SeinServices.Api.Services.Chungyak.IRecruitSyncStore, SeinServices.Api.Services.Chungyak.RecruitSyncStore>();
builder.Services.AddScoped<SeinServices.Api.Services.Chungyak.ISlackNotifier, SeinServices.Api.Services.Chungyak.SlackNotifier>();
builder.Services.AddScoped<SeinServices.Api.Services.Chungyak.RecruitSyncService>();
builder.Services.AddScoped<SeinServices.Api.Services.Chungyak.RcvhomeCloseService>();
builder.Services.AddScoped<SeinServices.Api.Services.Chungyak.SubscribeAlarmDispatchService>();

var enableInProcessSchedulers = builder.Configuration.GetValue<bool>("Schedulers:EnableInProcess");
if (enableInProcessSchedulers)
{
    builder.Services.AddHostedService<SeinServices.Api.Services.Schedules.RecruitSyncBackgroundService>();
    builder.Services.AddHostedService<SeinServices.Api.Services.Schedules.RcvhomeCloseBackgroundService>();
    builder.Services.AddHostedService<SeinServices.Api.Services.Schedules.SubscribeAlarmDispatchBackgroundService>();
}

var enableFaultMonSignalRScheduler = builder.Configuration.GetValue("FaultMon:EnableSignalRScheduler", true);
if (enableFaultMonSignalRScheduler)
{
    builder.Services.AddHostedService<SeinServices.Api.Services.Schedules.FaultMonRepeatInsertBackgroundService>();
}

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("chungyak", new OpenApiInfo
    {
        Title = "SeinServices API - 청약 서비스",
        Version = "v1",
        Description = "청약홈 공고 조회, 즐겨찾기, 데이터 동기화, 마감 처리, 구독 알림과 운영 로그를 제공하는 API 문서입니다."
    });

    options.SwaggerDoc("faultmon", new OpenApiInfo
    {
        Title = "SeinServices API - FaultMon 고장 관제",
        Version = "v1",
        Description = "FaultMon 프론트 화면에서 사용하는 고장 목록, 금일 통계, 상세 정보, 팝업 상세 데이터를 제공하는 API 문서입니다."
    });

    options.DocInclusionPredicate((documentName, apiDescription) =>
        string.Equals(apiDescription.GroupName, documentName, StringComparison.Ordinal));

    options.SwaggerDoc("deskbuddy", new OpenApiInfo
    {
        Title = "SeinServices API - DeskBuddy",
        Version = "v1",
        Description = "ESP32 책상 친구의 텍스트 대화 API입니다. Gemini가 검색 필요 여부를 판단하고 필요한 경우 Tavily 검색 후 대사·표정·출처를 반환합니다. X-Device-Key 헤더가 필요하며, 현재는 대화 기억·음성·실제 기기 제어를 포함하지 않습니다."
    });
    options.TagActionsBy(apiDescription => new[] { apiDescription.GroupName switch
    {
        "faultmon" => "FaultMon 고장 관제",
        "deskbuddy" => "DeskBuddy",
        _ => "청약 서비스"
    } });

    options.OperationFilter<SwaggerOperationDescriptionFilter>();
    options.DocumentFilter<SwaggerTagDocumentFilter>();
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/chungyak/swagger.json", "청약 서비스");
    options.SwaggerEndpoint("/swagger/faultmon/swagger.json", "FaultMon 고장 관제");
    options.SwaggerEndpoint("/swagger/deskbuddy/swagger.json", "DeskBuddy");
});

if (!app.Environment.IsEnvironment("Docker"))
{
    app.UseHttpsRedirection();
}

app.UseCors("FaultMonCors");
app.UseWebSockets(new WebSocketOptions { KeepAliveInterval = TimeSpan.FromSeconds(20) });
app.UseRateLimiter();
app.UseAuthorization();

app.MapControllers();
app.MapHub<FaultMonHub>("/hubs/faultmon");

app.Run();

public partial class Program { }
