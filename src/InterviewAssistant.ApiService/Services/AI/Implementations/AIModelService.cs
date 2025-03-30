using OpenAI;
using OpenAI.Chat;
using InterviewAssistant.ApiService.Services.Interfaces;
using InterviewAssistant.ApiService.Services.Implementations;

namespace InterviewAssistant.ApiService.Services;

/// <summary>
/// AI 모델 서비스 구현 클래스 (기본 생성자 사용)
/// </summary>
public class AIModelService(IConfiguration configuration) : IAIModelService
{
    private readonly string _provider = configuration["AiService:Provider"] ?? "GitHub";
    private readonly string _model = configuration["AiService:Model"] ?? "gpt-4o";
    private readonly string _endpoint = configuration["AiService:Endpoint"] ?? "https://models.inference.ai.azure.com";
    
    /// <inheritdoc/>
    public IChatClient CreateChatClient()
    {
        switch (_provider.ToLower())
        {
            case "azureopenai":
                return new OpenAIChatClient(CreateAzureOpenAIChatClient());
                
            case "github":
            default:
                return new OpenAIChatClient(CreateGitHubChatClient());
        }
    }
    
    /// <summary>
    /// Azure OpenAI 채팅 클라이언트 생성
    /// </summary>
    private ChatClient CreateAzureOpenAIChatClient()
    {
        var azureOpenAIKey = configuration["AzureOpenAI:ApiKey"] 
            ?? throw new InvalidOperationException("Azure OpenAI API 키가 구성되지 않았습니다.");
        var azureOpenAIEndpoint = configuration["AzureOpenAI:Endpoint"] 
            ?? throw new InvalidOperationException("Azure OpenAI 엔드포인트가 구성되지 않았습니다.");
        var deploymentName = configuration["AzureOpenAI:DeploymentName"] ?? "gpt-4o";
        
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
        var githubToken = configuration["GitHub:Token"] 
            ?? throw new InvalidOperationException("GitHub 토큰이 구성되지 않았습니다.");
        
        var endpoint = new Uri(_endpoint);
        var credential = new System.ClientModel.ApiKeyCredential(githubToken);
        
        return new ChatClient(_model, credential, new OpenAIClientOptions { Endpoint = endpoint });
    }
}