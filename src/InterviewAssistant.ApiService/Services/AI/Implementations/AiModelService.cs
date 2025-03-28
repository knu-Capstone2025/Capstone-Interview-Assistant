using OpenAI;
using OpenAI.Chat;

namespace InterviewAssistant.ApiService.Services;

/// <summary>
/// AI 모델 서비스 구현 클래스
/// </summary>
public class AIModelService : IAIModelService
{
    private readonly IConfiguration _configuration;
    
    /// <inheritdoc/>
    public string Provider { get; private set; } = "GitHub"; // 기본값 GitHub
    
    /// <inheritdoc/>
    public string Model { get; private set; } = "gpt-4o";
    
    /// <inheritdoc/>
    public string Endpoint { get; private set; } = "https://models.inference.ai.azure.com";
    
    /// <summary>
    /// AiModelService 생성자
    /// </summary>
    /// <param name="configuration">애플리케이션 구성</param>
    public AIModelService(IConfiguration configuration)
    {
        _configuration = configuration;
        
        // 설정에서 값 로드
        Provider = configuration["AiService:Provider"] ?? Provider;
        Model = configuration["AiService:Model"] ?? Model;
        Endpoint = configuration["AiService:Endpoint"] ?? Endpoint;
    }
    
    /// <inheritdoc/>
    public ChatClient CreateChatClient()
    {
        switch (Provider.ToLower())
        {
            case "azureopenai":
                return CreateAzureOpenAIChatClient();
                
            case "github":
            default:
                return CreateGitHubChatClient();
        }
    }
    
    /// <summary>
    /// Azure OpenAI 채팅 클라이언트 생성
    /// </summary>
    private ChatClient CreateAzureOpenAIChatClient()
    {
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
    }
    
    /// <summary>
    /// GitHub Models 채팅 클라이언트 생성
    /// </summary>
    private ChatClient CreateGitHubChatClient()
    {
        var githubToken = _configuration["GitHub:Token"] 
            ?? throw new InvalidOperationException("GitHub 토큰이 구성되지 않았습니다.");
        
        var endpoint = new Uri(Endpoint);
        var credential = new System.ClientModel.ApiKeyCredential(githubToken);
        
        return new ChatClient(Model, credential, new OpenAIClientOptions { Endpoint = endpoint });
    }
}