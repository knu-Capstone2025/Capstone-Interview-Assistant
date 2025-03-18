using Microsoft.AspNetCore.OpenApi; // 추가된 네임스페이스
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System.Text.Json.Serialization;
using System.Threading;

var builder = WebApplication.CreateBuilder(args);

// API 탐색기 사용 (.NET 9의 기본 OpenAPI 패키지)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(); // .NET 9에서도 Swagger 패키지는 여전히 필요합니다

// Semantic Kernel 서비스 등록
builder.Services.AddSingleton(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var azureOpenAIKey = configuration["AzureOpenAI:ApiKey"] 
        ?? throw new InvalidOperationException("Azure OpenAI API 키가 구성되지 않았습니다.");
    var azureOpenAIEndpoint = configuration["AzureOpenAI:Endpoint"] 
        ?? throw new InvalidOperationException("Azure OpenAI 엔드포인트가 구성되지 않았습니다.");
    var deploymentName = configuration["AzureOpenAI:DeploymentName"] ?? "gpt-4";

    // 최신 API 방식으로 커널 생성
    var kernelBuilder = Kernel.CreateBuilder();
    kernelBuilder.AddAzureOpenAIChatCompletion(
        deploymentName: deploymentName,
        endpoint: azureOpenAIEndpoint,
        apiKey: azureOpenAIKey);
    
    return kernelBuilder.Build();
});

// CORS 설정 추가
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

// ServiceDefaults 추가 (.NET Aspire 프로젝트에 포함된 기본 설정)
builder.AddServiceDefaults();

// 채팅 상태 관리 서비스 등록
builder.Services.AddSingleton<ChatState>();

var app = builder.Build();

// OpenAPI UI 설정
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Interview Assistant API v1");
    });
}

// CORS 미들웨어 활성화
app.UseCors("AllowAll");

// 기타 미들웨어 설정
app.UseHttpsRedirection();

// 미니멀 API 엔드포인트 정의
var chatGroup = app.MapGroup("/api/v1/chat").WithTags("Chat");

// 채팅 메시지 전송 엔드포인트
chatGroup.MapPost("/", async (ChatRequest request, Kernel kernel, ChatState state, ILogger<Program> logger) =>
{
    try
    {
        if (string.IsNullOrEmpty(request.Message))
        {
            return Results.BadRequest("메시지 내용이 비어있습니다.");
        }
        
        // 요청 스로틀링 적용
        await state.ApplyRateLimiting(logger);

        // 채팅 히스토리 생성
        var chatHistory = new Microsoft.SemanticKernel.ChatCompletion.ChatHistory();
        chatHistory.AddSystemMessage("당신은 유용한 AI 챗봇입니다. 짧고 명확하게 답변해주세요.");
        
        // 이전 대화가 있으면 추가
        if (!string.IsNullOrEmpty(state.LastUserMessage) && !string.IsNullOrEmpty(state.LastBotResponse))
        {
            chatHistory.AddUserMessage(state.LastUserMessage);
            chatHistory.AddAssistantMessage(state.LastBotResponse);
        }
        
        // 현재 메시지 추가
        chatHistory.AddUserMessage(request.Message);
        state.LastUserMessage = request.Message;

        // 짧은 응답을 위한 설정
        var openAIPromptExecutionSettings = new OpenAIPromptExecutionSettings
        {
            MaxTokens = 200,
            Temperature = 0.7,
            TopP = 0.95
        };

        string responseText;
        try 
        {
            // AI 응답 요청
            var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();
            var result = await chatCompletionService.GetChatMessageContentAsync(
                chatHistory,
                openAIPromptExecutionSettings,
                kernel
            );
            
            responseText = result.ToString();
        }
        catch (Exception ex) 
        {
            logger.LogError(ex, "Error calling AI service");
            // 실패 시 간단한 응답 제공
            responseText = "죄송합니다. 현재 시스템이 바쁩니다. 잠시 후 다시 시도해주세요.";
        }
        
        state.LastBotResponse = responseText;

        // 응답 생성
        var response = new ChatResponse
        {
            Response = responseText,
            Timestamp = DateTime.UtcNow
        };

        logger.LogInformation("Chat response generated");
        return Results.Ok(response);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Chat request processing failed");
        return Results.Problem($"내부 서버 오류: {ex.Message}");
    }
})
.WithName("PostChatMessage")
.WithOpenApi();

// 대화 초기화 엔드포인트
chatGroup.MapPost("/reset", (ChatState state) =>
{
    state.Reset();
    return Results.Ok(new { message = "대화가 초기화되었습니다." });
})
.WithName("ResetChat")
.WithOpenApi();

// ServiceDefaults 맵핑
app.MapDefaultEndpoints();

app.Run();

// 데이터 모델 정의
public class ChatRequest
{
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}

public class ChatResponse
{
    [JsonPropertyName("response")]
    public string Response { get; set; } = string.Empty;
    
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

// 채팅 상태 관리를 위한 싱글톤 서비스
public class ChatState
{
    // 간단한 히스토리 (최근 메시지 2개만 유지)
    public string LastUserMessage { get; set; } = string.Empty;
    public string LastBotResponse { get; set; } = string.Empty;
    
    // 속도 제한을 위한 타임스탬프
    public DateTime LastRequestTime { get; private set; } = DateTime.MinValue;
    private readonly SemaphoreSlim _throttleLock = new SemaphoreSlim(1, 1);
    
    public async Task ApplyRateLimiting(ILogger logger)
    {
        await _throttleLock.WaitAsync();
        try
        {
            var timeSinceLastRequest = DateTime.UtcNow - LastRequestTime;
            if (timeSinceLastRequest.TotalSeconds < 60)
            {
                var waitTime = TimeSpan.FromSeconds(60) - timeSinceLastRequest;
                logger.LogInformation("Rate limiting: Waiting for {WaitTime}ms before next request", waitTime.TotalMilliseconds);
                await Task.Delay(waitTime);
            }
            
            LastRequestTime = DateTime.UtcNow;
        }
        finally
        {
            _throttleLock.Release();
        }
    }
    
    public void Reset()
    {
        LastUserMessage = string.Empty;
        LastBotResponse = string.Empty;
    }
}