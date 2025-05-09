using System.Net;
using System.Text;

namespace InterviewAssistant.ApiService.Tests.Common;

/// <summary>
/// MockHttpMessageHandler에 대한 유창한 API를 제공하는 응답 래퍼
/// </summary>
public class MockHttpMessageHandlerResponse
{
    private readonly MockHttpMessageHandler _handler;
    private readonly string _url;

    public MockHttpMessageHandlerResponse(MockHttpMessageHandler handler, string url)
    {
        _handler = handler;
        _url = url;
    }

    /// <summary>
    /// 문자열 콘텐츠로 응답 설정
    /// </summary>
    public void Respond(string contentType, string content)
    {
        _handler.RegisterResponse(_url, _ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(content, Encoding.UTF8, contentType)
        });
    }

    /// <summary>
    /// 바이트 배열 콘텐츠로 응답 설정
    /// </summary>
    public void Respond(string contentType, byte[] content)
    {
        _handler.RegisterResponse(_url, _ =>
        {
            var httpContent = new ByteArrayContent(content);
            httpContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = httpContent
            };
        });
    }

    /// <summary>
    /// 사용자 정의 응답 설정
    /// </summary>
    public void Respond(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
    {
        _handler.RegisterResponse(_url, responseFactory);
    }

    /// <summary>
    /// HTTP 상태 코드로 응답 설정
    /// </summary>
    public void Respond(HttpStatusCode statusCode)
    {
        _handler.RegisterResponse(_url, _ => new HttpResponseMessage(statusCode));
    }
}
