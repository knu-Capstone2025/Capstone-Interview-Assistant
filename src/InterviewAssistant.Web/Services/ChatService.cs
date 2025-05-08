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
    /// <param name="messages">사용자 메시지</param>
    /// <returns>처리된 챗봇 응답</returns>
    IAsyncEnumerable<ChatResponse> SendMessageAsync(IEnumerable<ChatMessage> messages);

    /// <summary>
    /// 이력서 및 채용공고 데이터를 백엔드 API로 전송합니다.
    /// </summary>
    /// <param name="request">이력서 및 채용공고 URL이 포함된 요청 객체</param>
    /// <returns>API 응답</returns>
    IAsyncEnumerable<ChatResponse> SendInterviewDataAsync(InterviewDataRequest request);
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
    public async IAsyncEnumerable<ChatResponse> SendMessageAsync(IEnumerable<ChatMessage> messages)
    {
        if (messages == null || !messages.Any())
        {
            _logger.LogWarning("빈 메시지 목록이 전송되었습니다.");
            yield break;
        }

        _logger.LogInformation("메시지 목록 처리 시작: {Count}개", messages.Count());

        // 메시지 목록 정리 (필요 시 트림 등 추가 가능)
        var processedMessages = messages
            .Where(m => string.IsNullOrWhiteSpace(m.Message.Trim()) == false)
            .Select(m => new ChatMessage
            {
                Role = m.Role,
                Message = m.Message.Trim()
            })
            .ToList();

        if (processedMessages.Count == 0)
        {
            _logger.LogWarning("모든 메시지가 비어있습니다.");
            yield break;
        }

        // API 요청 생성 및 전송 (현재는 하드코딩된 응답을 반환하는 ChatApiClient 사용)
        var request = new ChatRequest
        {
            Messages = processedMessages
        };
        var responses = _client.SendMessageAsync(request);

        await foreach (var response in responses)
        {
            yield return response;
        }

        _logger.LogInformation("메시지 목록 처리 완료");
    }

    public async IAsyncEnumerable<ChatResponse> SendInterviewDataAsync(InterviewDataRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.ResumeUrl) || string.IsNullOrWhiteSpace(request.JobDescriptionUrl))
        {
            _logger.LogWarning("잘못된 인터뷰 데이터 요청이 수신되었습니다.");
            yield break;
        }

        _logger.LogInformation("인터뷰 데이터 전송 시작: ResumeUrl={ResumeUrl}, JobDescriptionUrl={JobDescriptionUrl}",
            request.ResumeUrl, request.JobDescriptionUrl);

        var responses = _client.SendInterviewDataAsync(request);
        await foreach (var response in responses)
        {
            yield return response;
        }

        _logger.LogInformation("인터뷰 데이터 전송 완료");
    }
}