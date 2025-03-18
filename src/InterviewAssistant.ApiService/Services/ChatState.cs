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
    
    // 동시 요청 제어를 위한 세마포어
    private readonly SemaphoreSlim _throttleLock = new SemaphoreSlim(1, 1);
    
    // 속도 제한 로직을 적용하여 요청 간 최소 시간 간격을 유지
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
    
    // 대화 상태를 초기화
    public void Reset()
    {
        LastUserMessage = string.Empty;
        LastBotResponse = string.Empty;
    }
}