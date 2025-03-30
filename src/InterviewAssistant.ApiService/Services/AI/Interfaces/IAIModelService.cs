using InterviewAssistant.ApiService.Services.Interfaces;

namespace InterviewAssistant.ApiService.Services;

/// <summary>
/// AI 모델 서비스를 위한 인터페이스
/// </summary>
public interface IAIModelService
{
    /// <summary>
    /// 채팅 클라이언트 생성 메서드
    /// </summary>
    /// <returns>구성된 IChatClient 인스턴스</returns>
    IChatClient CreateChatClient();
}