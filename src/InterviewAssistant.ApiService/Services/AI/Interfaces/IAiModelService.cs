using OpenAI.Chat;

namespace InterviewAssistant.ApiService.Services;

/// <summary>
/// AI 모델 서비스를 위한 인터페이스
/// </summary>
public interface IAiModelService
{
    /// <summary>
    /// 채팅 클라이언트 생성 메서드
    /// </summary>
    /// <returns>구성된 ChatClient 인스턴스</returns>
    ChatClient CreateChatClient();
    
    /// <summary>
    /// AI 서비스 공급자
    /// </summary>
    string Provider { get; }
    
    /// <summary>
    /// 사용할 AI 모델
    /// </summary>
    string Model { get; }
    
    /// <summary>
    /// AI 서비스 엔드포인트
    /// </summary>
    string Endpoint { get; }
}