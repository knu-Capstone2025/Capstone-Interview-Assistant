using Microsoft.Extensions.Logging;

namespace InterviewAssistant.ApiService.Services;

// 채팅 상태 관리를 위한 싱글톤 서비스
public class ChatState
{
    // 마지막으로 사용자가 보낸 메시지
    public string LastUserMessage { get; set; } = string.Empty;
    
    // 마지막으로 AI 챗봇이 응답한 메시지
    public string LastBotResponse { get; set; } = string.Empty;
    
    // 가장 최근 요청이 처리된 시간
    public DateTime LastRequestTime { get; private set; } = DateTime.MinValue;
    
    // 대화 상태를 초기화
    public void Reset()
    {
        LastUserMessage = string.Empty;
        LastBotResponse = string.Empty;
    }
}