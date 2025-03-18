using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using OpenAI;
using OpenAI.Chat;
using InterviewAssistant.ApiService.Models;
using InterviewAssistant.ApiService.Services;

var builder = WebApplication.CreateBuilder(args);

// API 탐색기 사용 (.NET 9의 기본 OpenAPI 패키지)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Interview Assistant API", Version = "v1" });
});

// 의존성 주입 설정
builder.Services.Configure<AiServiceOptions>(builder.Configuration.GetSection("AiService"));
builder.Services.AddSingleton<IAiServiceFactory, AiServiceFactory>();

// ChatClient를 명확하게 Singleton으로 등록
builder.Services.AddSingleton(sp => 
{
    var factory = sp.GetRequiredService<IAiServiceFactory>();
    return factory.CreateChatClient();
});

// ServiceDefaults 추가
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
chatGroup.MapPost("/", async (ChatRequest request, ChatClient chatClient, ChatState state, ILogger<Program> logger) =>
{
    try
    {
        // 빈 메시지 확인
        if (string.IsNullOrEmpty(request.Message))
        {
            return Results.BadRequest("메시지 내용이 비어있습니다.");
        }

        // 메시지 준비
        var messages = new List<ChatMessage>();
        
        // 시스템 메시지 추가
        messages.Add(new SystemChatMessage("당신은 유용한 AI 챗봇입니다. 짧고 명확하게 답변해주세요."));
        
        // 이전 대화가 있으면 추가
        if (!string.IsNullOrEmpty(state.LastUserMessage) && !string.IsNullOrEmpty(state.LastBotResponse))
        {
            messages.Add(new UserChatMessage(state.LastUserMessage));
            messages.Add(new AssistantChatMessage(state.LastBotResponse));
        }
        
        // 현재 메시지 추가
        messages.Add(new UserChatMessage(request.Message));
        state.LastUserMessage = request.Message;

        // AI 응답 요청
        var response = chatClient.CompleteChat(messages);
        
        string responseText = response.Value.Content[0].Text;

        // 마지막 대화 저장
        state.LastBotResponse = responseText;

        // 응답 생성
        var chatResponse = new ChatResponse
        {
            Response = responseText,
            Timestamp = DateTime.UtcNow
        };

        logger.LogInformation("Chat response generated");
        return Results.Ok(chatResponse);
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

// AI 서비스 옵션 클래스 추가
public class AiServiceOptions
{
    public string Provider { get; set; } = "GitHub"; // 기본값 GitHub
    public string Model { get; set; } = "gpt-4o";
    public string Endpoint { get; set; } = "https://models.inference.ai.azure.com";
    public string ApiKey { get; set; } = string.Empty;
}

// AI 서비스 팩토리 인터페이스
public interface IAiServiceFactory
{
    ChatClient CreateChatClient();
}

// AI 서비스 팩토리 구현
public class AiServiceFactory : IAiServiceFactory
{
    private readonly AiServiceOptions _options;
    private readonly IConfiguration _configuration;

    public AiServiceFactory(IConfiguration configuration)
    {
        _configuration = configuration;
        _options = new AiServiceOptions();
        configuration.GetSection("AiService").Bind(_options);
    }

    public ChatClient CreateChatClient()
    {
        switch (_options.Provider.ToLower())
        {
            case "azureopenai":
                // Azure OpenAI 클라이언트 생성
                var azureOpenAIKey = _configuration["AzureOpenAI:ApiKey"] 
                    ?? throw new InvalidOperationException("Azure OpenAI API 키가 구성되지 않았습니다.");
                var azureOpenAIEndpoint = _configuration["AzureOpenAI:Endpoint"] 
                    ?? throw new InvalidOperationException("Azure OpenAI 엔드포인트가 구성되지 않았습니다.");
                var deploymentName = _configuration["AzureOpenAI:DeploymentName"] ?? "gpt-4";

                var azureOpenAIOptions = new OpenAIClientOptions()
                {
                    Endpoint = new Uri(azureOpenAIEndpoint)
                };
                var azureCredential = new System.ClientModel.ApiKeyCredential(azureOpenAIKey);

                return new ChatClient(deploymentName, azureCredential, azureOpenAIOptions);

            case "github":
            default:
                // GitHub Models 클라이언트 생성
                var githubToken = _configuration["GitHub:Token"] 
                    ?? throw new InvalidOperationException("GitHub 토큰이 구성되지 않았습니다.");
                
                var endpoint = new Uri(_options.Endpoint);
                var credential = new System.ClientModel.ApiKeyCredential(githubToken);

                return new ChatClient(_options.Model, credential, new OpenAIClientOptions { Endpoint = endpoint });
        }
    }
}