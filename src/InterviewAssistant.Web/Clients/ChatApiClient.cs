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

    /// <summary>
    /// 이력서 및 채용공고 데이터를 백엔드 API로 전송합니다.
    /// </summary>
    /// <param name="request">이력서 및 채용공고 URL이 포함된 요청 객체</param>
    /// <returns>API 응답</returns>
    IAsyncEnumerable<ChatResponse> SendInterviewDataAsync(InterviewDataRequest request);

    Task<InterviewReportModel?> GenerateReportAsync(List<ChatMessage> messages);
}

/// <summary>
/// 백엔드 채팅 API와의 통신을 담당하는 클라이언트 구현
/// </summary>
public class ChatApiClient(HttpClient http, ILoggerFactory loggerFactory) : IChatApiClient
{
    private readonly HttpClient _http = http ?? throw new ArgumentNullException(nameof(http));
    private readonly ILogger<ChatApiClient> _logger = (loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory)))
                                                      .CreateLogger<ChatApiClient>();

    /// <inheritdoc/>
    public async IAsyncEnumerable<ChatResponse> SendMessageAsync(ChatRequest request)
    {
        _logger.LogInformation("API 요청 시뮬레이션: {Message}", request.Messages.LastOrDefault()?.Message ?? "빈 메시지");

        var httpResponse = await _http.PostAsJsonAsync("/api/chat/complete", request);
        httpResponse.EnsureSuccessStatusCode();

        var responses = httpResponse.Content.ReadFromJsonAsAsyncEnumerable<ChatResponse>();
        await foreach (var response in responses)
        {
            if (response is not null)
            {
                yield return response;
            }
        }

        _logger.LogInformation("API 응답 시뮬레이션 완료");
    }

    public async IAsyncEnumerable<ChatResponse> SendInterviewDataAsync(InterviewDataRequest request)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));  // 명시적 예외 처리

        _logger.LogInformation("ChatApiClient.cs: 인터뷰 데이터 전송 시작");

        var httpResponse = await _http.PostAsJsonAsync("/api/chat/interview-data", request);
        httpResponse.EnsureSuccessStatusCode();

        var responses = httpResponse.Content.ReadFromJsonAsAsyncEnumerable<ChatResponse>();
        await foreach (var response in responses)
        {
            if (response is not null)
            {
                yield return response;
            }
        }

        _logger.LogInformation("ChatApiClient.cs: 인터뷰 데이터 전송 완료");
    }

    public async Task<InterviewReportModel?> GenerateReportAsync(List<ChatMessage> messages)
    {
        _logger.LogInformation("API에 리포트 생성 요청");
        var httpResponse = await _http.PostAsJsonAsync("/api/chat/report", messages);

        if (httpResponse.IsSuccessStatusCode)
        {
            return await httpResponse.Content.ReadFromJsonAsync<InterviewReportModel>();
        }

        _logger.LogError("리포트 생성 실패: {StatusCode}", httpResponse.StatusCode);
        return null;
    }
}
