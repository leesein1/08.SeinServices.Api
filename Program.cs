using Microsoft.OpenApi.Models;
using SeinServices.Api.Swagger;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers(options =>
{
    options.Conventions.Add(new ApiExplorerGroupConvention());
});
builder.Services.AddHttpClient();

builder.Services.AddScoped<SeinServices.Api.Data.Chungyak.DBHelper>();
builder.Services.AddScoped<SeinServices.Api.Data.FaultMon.FaultMonDbHelper>();
builder.Services.AddScoped<SeinServices.Api.Data.FaultMon.FaultMonRepository>();
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

    options.TagActionsBy(apiDescription =>
        apiDescription.GroupName == "faultmon"
            ? new[] { "FaultMon 고장 관제" }
            : new[] { "청약 서비스" });

    options.OperationFilter<SwaggerOperationDescriptionFilter>();
    options.DocumentFilter<SwaggerTagDocumentFilter>();
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/chungyak/swagger.json", "청약 서비스");
    options.SwaggerEndpoint("/swagger/faultmon/swagger.json", "FaultMon 고장 관제");
});

if (!app.Environment.IsEnvironment("Docker"))
{
    app.UseHttpsRedirection();
}

app.UseAuthorization();

app.MapControllers();

app.Run();
