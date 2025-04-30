using System.Text.Json;
using System.Text.Json.Serialization;

using InterviewAssistant.ApiService.Endpoints;
using InterviewAssistant.ApiService.Services;

using Microsoft.SemanticKernel;

using OpenAI;

var builder = WebApplication.CreateBuilder(args);

// .NET Aspire 기본 설정
builder.AddServiceDefaults();
builder.Services.AddProblemDetails();
builder.Services.AddScoped<IKernelService, KernelService>();

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

// HttpClient 및 GoogleDriveDownloader 서비스 등록
builder.Services.AddHttpClient();
builder.Services.AddScoped<UrlContentDownloader>();

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

app.UseHttpsRedirection();
app.UseExceptionHandler();

// Chat Completion 엔드포인트 매핑
app.MapChatCompletionEndpoint();

// .NET Aspire 헬스체크 및 모니터링 엔드포인트 매핑
app.MapDefaultEndpoints();

app.Run();
