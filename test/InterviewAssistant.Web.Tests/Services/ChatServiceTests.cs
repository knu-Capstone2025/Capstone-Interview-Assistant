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

    // 테스트 1:  메시지 목록이 정상적으로 처리될 때 응답 반환
    [Test]
    public async Task SendMessageAsync_WhenSuccessful_ReturnsResponse()
    {
        // Arrange - 테스트 데이터 설정
        var messages = new List<ChatMessage>
        {
            new ChatMessage { Role = MessageRoleType.User, Message = "안녕하세요" }
        };
        var expectedResponse = new ChatResponse { Message = "테스트 응답입니다" };
        _apiClient.SendMessageAsync(Arg.Any<ChatRequest>())
            .Returns(new List<ChatResponse> { expectedResponse }.ToAsyncEnumerable());

        // Act - 메시지 목록을 API에 전송
        var result = await _chatService.SendMessageAsync(messages).ToListAsync();

        // Assert - 결과 검증
        result.ShouldNotBeNull();
        result.Count.ShouldBe(1);
        result[0].Message.ShouldBe("테스트 응답입니다");
    }

    // 테스트 2: 빈 메시지 목록이 전달될 때 처리 중단
    [Test]
    public async Task SendMessageAsync_WhenMessagesAreEmpty_YieldsNoResponses()
    {
        // Arrange - 메세지 목록을 빈 리스트로 설정
        var messages = new List<ChatMessage>();

        // Act - 빈 메시지 목록을 전달
        var result = await _chatService.SendMessageAsync(messages).ToListAsync();

        // Assert - 빈 리스트가 반환되는지 검증
        result.ShouldBeEmpty();
    }


    // 테스트 3: 메시지 전송이 실패할 때 예외 처리하는지 검증
    [Test]
    public void SendMessageAsync_WhenApiFails_ThrowsException()
    {
        // Arrange - 대체 API 클라이언트가 예외를 발생하도록 설정
        var messages = new List<ChatMessage>
        {
            new ChatMessage { Role = MessageRoleType.User, Message = "안녕하세요" }
        };
        _apiClient.SendMessageAsync(Arg.Any<ChatRequest>())
            .Throws(new Exception("API 호출 실패"));

        // Act & Assert - 예외가 발생하는지 검증
        var ex = Assert.ThrowsAsync<Exception>(async () =>
            await _chatService.SendMessageAsync(messages).ToListAsync());

        Assert.That(ex.Message, Is.EqualTo("API 호출 실패"));
    }

    // InterviewDataRequest가 올바른 경우 API에서 반환된 응답을 잘 처리하는지 검증.
    [Test]
    public async Task SendInterviewDataAsync_WhenRequestIsValid_ReturnsResponses()
    {
        // Arrange
        var request = new InterviewDataRequest
        {
            ResumeUrl = "https://www.example.com/resume",
            JobDescriptionUrl = "https://www.example.com/job"
        };

        var expectedResponses = new List<ChatResponse>
        {
            new ChatResponse { Message = "이력서 검토 완료" },
            new ChatResponse { Message = "채용공고 분석 완료" }
        };

        _apiClient.SendInterviewDataAsync(Arg.Any<InterviewDataRequest>())
            .Returns(expectedResponses.ToAsyncEnumerable());

        // Act
        var responses = await _chatService.SendInterviewDataAsync(request).ToListAsync();

        // Assert
        responses.Count.ShouldBe(2);
        responses[0].Message.ShouldBe("이력서 검토 완료");
        responses[1].Message.ShouldBe("채용공고 분석 완료");
    }

    // 비어있는 URL이 포함된 요청이 전달되었을 때 빈 응답 목록을 반환하는지 검증.
    [Test]
    public async Task SendInterviewDataAsync_WithInvalidRequest_YieldsNoResponses()
    {
        // Arrange
        var invalidRequest = new InterviewDataRequest
        {
            ResumeUrl = "",
            JobDescriptionUrl = ""
        };

        // Act
        var responses = await _chatService.SendInterviewDataAsync(invalidRequest).ToListAsync();

        // Assert
        responses.ShouldBeEmpty();
    }

    //null 요청이 전달될 때 예외가 발생하는지 검증.
    [Test]
    public void SendInterviewDataAsync_WithNullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        InterviewDataRequest? request = null;

        // Act & Assert
        var ex = Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await _chatService.SendInterviewDataAsync(request!).ToListAsync());

        // Assert - 예외 메시지 검증 (매개변수 이름도 검증)
        Assert.That(ex.ParamName, Is.EqualTo("request"));
    }
}
