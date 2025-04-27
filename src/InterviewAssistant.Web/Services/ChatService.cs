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
    private readonly ChatHistory _chatHistory = new(); // 대화 히스토리 객체
    
    /// <inheritdoc/>
    public async IAsyncEnumerable<ChatResponse> SendMessageAsync(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            _logger.LogWarning("빈 메시지가 전송되었습니다.");
            yield break;
        }
        
        _logger.LogInformation("메시지 처리 시작: {Message}", message);

        // 사용자 메시지를 대화 히스토리에 추가
        _chatHistory.Messages.Add(new ChatMessage 
        {
            Role = MessageRoleType.User, 
            Message = message.Trim()
        });

        // responses를 받을 때 임시로 메세지를 누적할 변수 생성
        string accumulatedBotMessage = string.Empty;
        // API 요청 생성 및 전송 (현재는 하드코딩된 응답을 반환하는 ChatApiClient 사용)
        var request = new ChatRequest
        {
            Messages = _chatHistory.Messages // 전체 대화 히스토리를 포함해서 요청
        };
        var responses = _client.SendMessageAsync(request);
        
        await foreach (var response in responses)
        {
            accumulatedBotMessage += response.Message; // 스트림으로 들어오는 대화를 누적 
            yield return response;
        }

        // 스트리밍이 끝난 뒤, 하나의 메시지로 Assistant 히스토리에 추가
        if (!string.IsNullOrWhiteSpace(accumulatedBotMessage))
        {
            var botMessage = new ChatMessage { Role = MessageRoleType.Assistant, Message = accumulatedBotMessage.Trim() };
            _chatHistory.Messages.Add(botMessage);

            LogChatHistory(); // 히스토리 출력
        }
        _logger.LogInformation("메시지 처리 완료");
    }

    /// <summary>
    /// 대화 히스토리를 로그로 출력합니다.
    /// </summary>
    private void LogChatHistory()
    {
        _logger.LogInformation("현재 대화 히스토리:");
        foreach (var message in _chatHistory.Messages)
        {
            _logger.LogInformation("[{Role}] {Message}", message.Role, message.Message);
        }
    }
}