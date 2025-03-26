using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenAI;
using OpenAI.Chat;
using System;

namespace InterviewAssistant.ApiService.Services;
// 옵션과 팩토리를 하나의 클래스로 통합
public class AiModelService
{
    private readonly IConfiguration _configuration;
    private string Provider { get; set; } = "GitHub"; // 기본값 GitHub
    private string Model { get; set; } = "gpt-4o";
    private string Endpoint { get; set; } = "https://models.inference.ai.azure.com";
    public AiModelService(IConfiguration configuration)
    {
        _configuration = configuration;
        
        // 설정에서 값 로드
        Provider = configuration["AiService:Provider"] ?? Provider;
        Model = configuration["AiService:Model"] ?? Model;
        Endpoint = configuration["AiService:Endpoint"] ?? Endpoint;
    }
    // 팩토리 메서드 직접 구현
    public ChatClient CreateChatClient()
    {
        switch (Provider.ToLower())
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
                
                var endpoint = new Uri(Endpoint);
                var credential = new System.ClientModel.ApiKeyCredential(githubToken);
                return new ChatClient(Model, credential, new OpenAIClientOptions { Endpoint = endpoint });
        }
    }
}
// 확장 메서드도 같은 파일에 포함
public static class ServiceExtensions
{
    public static IServiceCollection AddAiModelService(this IServiceCollection services)
    {
        // AiModelService를 싱글톤으로 등록
        services.AddSingleton<AiModelService>();
        
        // ChatClient를 서비스로 등록
        services.AddSingleton(sp => 
        {
            var aiModelService = sp.GetRequiredService<AiModelService>();
            return aiModelService.CreateChatClient();
        });
        
        return services;
    }
}