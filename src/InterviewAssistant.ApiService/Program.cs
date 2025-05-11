using System.Text.Json;
using System.Text.Json.Serialization;

using InterviewAssistant.ApiService.Endpoints;
using InterviewAssistant.ApiService.Services;
using InterviewAssistant.ApiService.Data;
using InterviewAssistant.ApiService.Repositories;

using Microsoft.SemanticKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;

using OpenAI;

var builder = WebApplication.CreateBuilder(args);

// .NET Aspire 기본 설정
builder.AddServiceDefaults();
builder.Services.AddProblemDetails();
// SQLite In-Memory 연결 생성 및 열기
var sqliteConnection = new SqliteConnection("DataSource=:memory:");
sqliteConnection.Open(); // 중요: 연결이 열려 있어야 메모리 DB 유지됨

builder.Services.AddDbContext<InterviewDbContext>(options =>
    options.UseSqlite(sqliteConnection)); // SQLite로 설정

builder.Services.AddSingleton(sqliteConnection); // 연결이 앱 생명주기와 함께 유지되도록

builder.Services.AddScoped<IKernelService, KernelService>();
builder.Services.AddScoped<IInterviewRepository, InterviewRepository>();

//OpenAPI 설정
builder.Services.AddOpenApi();

builder.AddAzureOpenAIClient("openai");

builder.Services.AddSingleton<Kernel>(sp =>
{
    var config = builder.Configuration;

    var openAIClient = sp.GetRequiredService<OpenAIClient>();
    var kernel = Kernel.CreateBuilder()
                       .AddOpenAIChatCompletion(
                           modelId: config["GitHub:Models:ModelId"]!,
                           openAIClient: openAIClient,
                           serviceId: "github")
                       .Build();

    return kernel;
});

builder.Services.AddHttpClient<IUrlContentDownloader, UrlContentDownloader>();

// JSON 직렬화 설정
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.WriteIndented = true;
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
});

var app = builder.Build();

// 개발 환경에서만 Swagger UI 활성화
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// 데이터베이스 스키마 초기화
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<InterviewDbContext>();

    dbContext.Database.EnsureCreated();
}

app.UseHttpsRedirection();
app.UseExceptionHandler();

// Chat Completion 엔드포인트 매핑
app.MapChatCompletionEndpoint();

// .NET Aspire 헬스체크 및 모니터링 엔드포인트 매핑
app.MapDefaultEndpoints();

app.Run();
