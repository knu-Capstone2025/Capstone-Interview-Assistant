using InterviewAssistant.Common.Models;
using InterviewAssistant.Web.Services;
using InterviewAssistant.Web.Clients;
using Microsoft.Extensions.Logging;
using NSubstitute.ExceptionExtensions;

namespace InterviewAssistant.Web.Tests.Services;

[TestFixture]
public class ChatServiceTests
{
    private IChatApiClient _apiClient;
    private ILoggerFactory _loggerFactory;
    private IChatService _chatService;

    [SetUp]
    public void Setup()
    {
        // API 클라이언트와 로거를 대체 객체로 생성
        _apiClient = Substitute.For<IChatApiClient>();
        _loggerFactory = Substitute.For<ILoggerFactory>();
        
        // 테스트할 ChatService 인스턴스 생성
        _chatService = new ChatService(_apiClient, _loggerFactory);
    }

    // TearDown 메서드 추가 - ILoggerFactory 정리
    [TearDown]
    public void Cleanup()
    {
        _loggerFactory?.Dispose();
    }

    // 테스트 1: 메시지 전송이 성공할 때 응답을 올바르게 반환하는지 검증
    [Test]
    public async Task SendMessageAsync_WhenSuccessful_ReturnsResponse()
    {
        // Arrange - 대체 API 클라이언트가 성공적인 응답을 반환하도록 설정
        var expectedResponse = new ChatResponse { Message = "테스트 응답입니다" };
        _apiClient.SendMessageAsync(Arg.Any<ChatRequest>()).Returns(new List<ChatResponse>() { expectedResponse }.ToAsyncEnumerable());
        
        // Act - ChatService의 메시지 전송 메서드 호출
        var result = await _chatService.SendMessageAsync("안녕하세요").ToListAsync();
        
        // Assert - 결과 검증
        result.ShouldNotBeNull();
        result[0].Message.ShouldBe("테스트 응답입니다");
    }

    // 테스트 2: 메시지 전송이 실패할 때 예외 처리하는지 검증
    [Test]
    public void SendMessageAsync_WhenApiFails_ThrowsException()
    {
        // Arrange - 대체 API 클라이언트가 예외를 발생하도록 설정
        _apiClient.SendMessageAsync(Arg.Any<ChatRequest>()).Throws(new Exception("API 호출 실패"));

        // Act & Assert - 예외가 발생하는지 검증
        var ex = Assert.ThrowsAsync<Exception>(async () =>
            await _chatService.SendMessageAsync("안녕하세요").ToListAsync());

        Assert.That(ex.Message, Is.EqualTo("API 호출 실패"));
    }
}
