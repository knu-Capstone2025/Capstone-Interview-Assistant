namespace InterviewAssistant.ApiService.Services;

/// <summary>
/// 서비스 등록을 위한 확장 메서드
/// </summary>
public static class ServiceExtensions
{
    /// <summary>
    /// AI 모델 서비스를 DI 컨테이너에 등록
    /// </summary>
    /// <param name="services">서비스 컬렉션</param>
    /// <returns>변경된 서비스 컬렉션</returns>
    public static IServiceCollection AddAIModelService(this IServiceCollection services)
    {
        // IAiModelService와 구현체 AiModelService를 싱글톤으로 등록
        services.AddSingleton<IAiModelService, AiModelService>();
        
        return services;
    }
}