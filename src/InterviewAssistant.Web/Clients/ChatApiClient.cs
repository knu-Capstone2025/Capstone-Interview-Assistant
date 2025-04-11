using InterviewAssistant.Common.Models;

namespace InterviewAssistant.Web.Clients;

/// <summary>
/// 백엔드 채팅 API와의 통신을 담당하는 클라이언트 인터페이스
/// </summary>
public interface IChatApiClient
{
    /// <summary>
    /// 메시지를 백엔드 API로 전송합니다.
    /// </summary>
    /// <param name="request">전송할 채팅 요청</param>
    /// <returns>API 응답</returns>
    IAsyncEnumerable<ChatResponse> SendMessageAsync(ChatRequest request);
}

/// <summary>
/// 백엔드 채팅 API와의 통신을 담당하는 클라이언트 구현
/// </summary>
public class ChatApiClient(HttpClient http, ILoggerFactory loggerFactory) : IChatApiClient
{
    private readonly HttpClient _http = http ?? throw new ArgumentNullException(nameof(http));
    private readonly ILogger<ChatApiClient> _logger = (loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory)))
                                                      .CreateLogger<ChatApiClient>();

    private static readonly string loremipsum = "Lorem ipsum dolor sit amet, \nconsectetur adipiscing elit.";
    
    /// <inheritdoc/>
    public async IAsyncEnumerable<ChatResponse> SendMessageAsync(ChatRequest request)
    {
        _logger.LogInformation("API 요청 시뮬레이션: {Message}", request.Messages.LastOrDefault()?.Message ?? "빈 메시지");
        
        var httpResponse = await _http.PostAsJsonAsync("/api/chat/complete", request);
        httpResponse.EnsureSuccessStatusCode();

        var responses = await httpResponse.Content.ReadFromJsonAsync<List<ChatResponse>>();
        if (responses is null || responses.Count == 0)
        {
            yield break;
        }

        // 응답 메시지 전송
        foreach (var response in responses)
        {
            yield return response;
        }

        _logger.LogInformation("API 응답 시뮬레이션 완료");
    }
}
