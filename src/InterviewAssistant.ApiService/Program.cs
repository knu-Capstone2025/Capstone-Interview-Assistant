using InterviewAssistant.Common.Models;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddProblemDetails();

//OpenAPI 설정
builder.Services.AddOpenApi();

// JSON 직렬화 설정
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    options.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
});

var app = builder.Build();

// 개발 환경에서만 Swagger UI 활성화
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseExceptionHandler();

// 채팅 API 그룹
var chatGroup = app.MapGroup("/api/chat").WithTags("Chat");

// 채팅 메시지 전송 엔드포인트
chatGroup.MapPost("/", (ChatRequest request) => new List<ChatResponse>())
    .Accepts<ChatRequest>(contentType: "application/json")
    .Produces<IEnumerable<ChatResponse>>(statusCode: StatusCodes.Status200OK, contentType: "application/json")
    .WithName("ChatCompletion")
    .WithOpenApi();


// 기본 엔드포인트 매핑
app.MapDefaultEndpoints();

app.Run();