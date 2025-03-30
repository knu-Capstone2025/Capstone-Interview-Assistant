using InterviewAssistant.Common.Models;
using InterviewAssistant.Web.Clients;

namespace InterviewAssistant.Web.Services;

/// <summary>
/// 채팅 관련 비즈니스 로직을 처리하는 서비스 인터페이스
/// </summary>
public interface IChatService
{
    /// <summary>
    /// 사용자 메시지를 처리하고 응답을 반환합니다.
    /// </summary>
    /// <param name="message">사용자 메시지</param>
    /// <returns>처리된 챗봇 응답</returns>
    IAsyncEnumerable<ChatResponse> SendMessageAsync(string message);
}

/// <summary>
/// 채팅 관련 비즈니스 로직을 처리하는 서비스 구현
/// </summary>
public class ChatService(IChatApiClient client, ILoggerFactory loggerFactory) : IChatService
{
    private readonly IChatApiClient _client = client ?? throw new ArgumentNullException(nameof(client));
    private readonly ILogger<ChatService> _logger = (loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory)))
                                                    .CreateLogger<ChatService>();
    
    /// <inheritdoc/>
    public async IAsyncEnumerable<ChatResponse> SendMessageAsync(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            _logger.LogWarning("빈 메시지가 전송되었습니다.");
            yield break;
        }
        
        _logger.LogInformation("메시지 처리 시작: {Message}", message);

        // 현재는 메세지 트림만 처리한다
        string processedMessage = message.Trim();
                    
        // API 요청 생성 및 전송 (현재는 하드코딩된 응답을 반환하는 ChatApiClient 사용)
        var request = new ChatRequest
        { 
            Messages =
            [
                new ChatMessage { Role = MessageRoleType.User, Message = processedMessage }
            ]
        };
        var responses = _client.SendMessageAsync(request);
        
        await foreach (var response in responses)
        {
            yield return response;
        }

        _logger.LogInformation("메시지 처리 완료");
    }
}
