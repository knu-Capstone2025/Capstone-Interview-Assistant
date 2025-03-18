using Microsoft.AspNetCore.OpenApi;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using InterviewAssistant.ApiService.Models;
using InterviewAssistant.ApiService.Services;

var builder = WebApplication.CreateBuilder(args);

// API 탐색기 사용 (.NET 9의 기본 OpenAPI 패키지)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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

// 기타 미들웨어 설정
app.UseHttpsRedirection();

// 미니멀 API 엔드포인트 정의
var chatGroup = app.MapGroup("/api/v1/chat").WithTags("Chat");

// 채팅 메시지 전송 엔드포인트
chatGroup.MapPost("/", async (ChatRequest request, Kernel kernel, ChatState state, ILogger<Program> logger) =>
{
    try
    {
        // 빈 메시지 확인
        if (string.IsNullOrEmpty(request.Message))
        {
            return Results.BadRequest("메시지 내용이 비어있습니다.");
        }
        
        // 요청 스로틀링 적용
        await state.ApplyRateLimiting(logger);

        // 채팅 히스토리 생성
        var chatHistory = new ChatHistory();
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
            MaxTokens = 200,  // 최대 토큰 수 제한
            Temperature = 0.7, // 창의성 조절 (0: 결정적, 1: 창의적)
            TopP = 0.95 // 다양성 조절
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
            // 오류 발생 시 로깅 및 기본 응답 제공
            logger.LogError(ex, "Error calling AI service");
            responseText = "죄송합니다. 현재 시스템이 바쁩니다. 잠시 후 다시 시도해주세요.";
        }
        
        // 마지막 대화 저장
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
        // 예상치 못한 오류 처리
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