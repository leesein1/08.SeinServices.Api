var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddHttpClient();

builder.Services.AddScoped<SeinServices.Api.Data.Chungyak.DBHelper>();
builder.Services.AddScoped<SeinServices.Api.Services.Chungyak.ChungyakSearchService>();
builder.Services.AddScoped<SeinServices.Api.Services.Chungyak.ChungyakFavoriteService>();
builder.Services.AddScoped<SeinServices.Api.Services.Chungyak.ScheduleLogService>();
builder.Services.AddScoped<SeinServices.Api.Services.Chungyak.AlarmLogService>();
builder.Services.AddScoped<SeinServices.Api.Services.Chungyak.IRecruitSyncStore, SeinServices.Api.Services.Chungyak.RecruitSyncStore>();
builder.Services.AddScoped<SeinServices.Api.Services.Chungyak.ISlackNotifier, SeinServices.Api.Services.Chungyak.SlackNotifier>();
builder.Services.AddScoped<SeinServices.Api.Services.Chungyak.RecruitSyncService>();
builder.Services.AddScoped<SeinServices.Api.Services.Chungyak.RcvhomeCloseService>();

var enableInProcessSchedulers = builder.Configuration.GetValue<bool>("Schedulers:EnableInProcess");
if (enableInProcessSchedulers)
{
    builder.Services.AddHostedService<SeinServices.Api.Services.Schedules.RecruitSyncBackgroundService>();
    builder.Services.AddHostedService<SeinServices.Api.Services.Schedules.RcvhomeCloseBackgroundService>();
}

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

