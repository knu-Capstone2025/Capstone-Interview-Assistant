using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

using InterviewAssistant.Common.Models;
using InterviewAssistant.Web.Clients;

using Microsoft.Extensions.Logging;

namespace InterviewAssistant.Web.Tests.Clients;

[TestFixture]
public class ChatApiClientTests
{
    private IChatApiClient _chatApiClient;
    private HttpClient _httpClient;
    private ILoggerFactory _loggerFactory;


    [SetUp]
    public void Setup()
    {
        // 로거 팩토리 대체 객체 생성
        _httpClient = Substitute.For<HttpClient>();
        _loggerFactory = Substitute.For<ILoggerFactory>();

        // 테스트할 ChatApiClient 인스턴스 생성
        _chatApiClient = new ChatApiClient(_httpClient, _loggerFactory);
    }

    [TearDown]
    public void Cleanup()
    {
        // 리소스 정리
        _httpClient.Dispose();
        _loggerFactory.Dispose();
    }

    // 테스트 1:  가짜 응답 데이터 (백엔드에서 GPT 응답처럼 준다고 가정)
    [Test]
    public async Task SendMessageAsync_ReturnsGptLikeResponses_WithRole()
    {
        var mockResponseJson = JsonSerializer.Serialize(new List<ChatResponse>
            {
                new ChatResponse { Message = "안녕하세요!" },
                new ChatResponse { Message = "자기소개 부탁드립니다." }
            });

        var fakeHttpContent = new StringContent(mockResponseJson, Encoding.UTF8, "application/json");
        var fakeResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = fakeHttpContent
        };

        // 2. Fake 핸들러 주입
        var handler = new FakeHttpMessageHandler(fakeResponse);
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://fake-api.test")
        };

        // 3. ChatApiClient 생성
        var loggerFactory = Substitute.For<ILoggerFactory>();
        var chatApiClient = new ChatApiClient(httpClient, loggerFactory);

        // 4. 실제 흐름에 맞는 요청 구성 (Role 포함)
        var request = new ChatRequest
        {
            Messages = new List<ChatMessage>
                {
                    new ChatMessage
                    {
                        Role = MessageRoleType.User,
                        Message = "안녕하세요, 면접 준비를 도와주세요."
                    }
                }
        };

        // 5. 응답 받기
        var responses = await chatApiClient.SendMessageAsync(request).ToListAsync();

        // 6. 검증
        responses.Count.ShouldBe(2);
        responses[0].Message.ShouldBe("안녕하세요!");
        responses[1].Message.ShouldBe("자기소개 부탁드립니다.");
    }

    // 유효한 요청이 전송되었을 때 올바른 응답을 반환하는지 검증.
    [Test]
    public async Task SendInterviewDataAsync_ReturnsResponses_WithValidRequest()
    {
        // Arrange
        var mockResponseJson = JsonSerializer.Serialize(new List<ChatResponse>
        {
            new ChatResponse { Message = "이력서 검토 완료" },
            new ChatResponse { Message = "채용공고 분석 완료" }
        });

        var fakeHttpContent = new StringContent(mockResponseJson, Encoding.UTF8, "application/json");
        var fakeResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = fakeHttpContent
        };

        var handler = new FakeHttpMessageHandler(fakeResponse);
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://fake-api.test")
        };

        var loggerFactory = Substitute.For<ILoggerFactory>();
        var chatApiClient = new ChatApiClient(httpClient, loggerFactory);

        var request = new InterviewDataRequest
        {
            ResumeUrl = "https://www.example.com/resume",
            JobDescriptionUrl = "https://www.example.com/job"
        };

        // Act
        var responses = await chatApiClient.SendInterviewDataAsync(request).ToListAsync();

        // Assert
        responses.Count.ShouldBe(2);
        responses[0].Message.ShouldBe("이력서 검토 완료");
        responses[1].Message.ShouldBe("채용공고 분석 완료");
    }

    // null 요청이 전달될 때 ArgumentNullException이 발생하는지 검증.
    [Test]
    public void SendInterviewDataAsync_WithNullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        var chatApiClient = new ChatApiClient(new HttpClient(), Substitute.For<ILoggerFactory>());

        // Act & Assert
        Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await chatApiClient.SendInterviewDataAsync(null!).ToListAsync());
    }
}
