using System.Net;

namespace InterviewAssistant.ApiService.Tests.Common;

/// <summary>
/// 테스트용 HttpMessageHandler 모의 구현
/// </summary>
public class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly Dictionary<string, Func<HttpRequestMessage, HttpResponseMessage>> _responses = new();

    /// <summary>
    /// 특정 URL에 대한 응답 설정
    /// </summary>
    public MockHttpMessageHandlerResponse When(string url)
    {
        return new MockHttpMessageHandlerResponse(this, url);
    }

    /// <summary>
    /// 요청 URL에 대한 응답 함수 등록
    /// </summary>
    public void RegisterResponse(string url, Func<HttpRequestMessage, HttpResponseMessage> responseFunc)
    {
        _responses[url] = responseFunc;
    }

    /// <summary>
    /// 요청 처리 오버라이드
    /// </summary>
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var url = request.RequestUri?.ToString();

        if (url != null && _responses.ContainsKey(url))
        {
            return Task.FromResult(_responses[url](request));
        }

        // 기본 응답 - Not Found
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent("Not found")
        });
    }
}
