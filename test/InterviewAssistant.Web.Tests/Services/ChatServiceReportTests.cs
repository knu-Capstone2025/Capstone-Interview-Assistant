using InterviewAssistant.Common.Models;
using InterviewAssistant.Web.Services;
using InterviewAssistant.Web.Clients;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;

namespace InterviewAssistant.Web.Tests.Services;

[TestFixture]
public class ChatServiceReportTests
{
    private IChatApiClient _apiClient;
    private ILoggerFactory _loggerFactory;
    private IChatService _chatService;

    [SetUp]
    public void Setup()
    {
        _apiClient = Substitute.For<IChatApiClient>();
        _loggerFactory = Substitute.For<ILoggerFactory>();
        _chatService = new ChatService(_apiClient, _loggerFactory);
    }

    [TearDown]
    public void Cleanup()
    {
        _loggerFactory?.Dispose();
    }

    [Test]
    public async Task GenerateReportAsync_WithValidMessages_ShouldReturnReport()
    {
        // Arrange
        var messages = new List<ChatMessage>
        {
            new() { Role = MessageRoleType.User, Message = "안녕하세요" },
            new() { Role = MessageRoleType.Assistant, Message = "안녕하세요! 자기소개 부탁드립니다." },
            new() { Role = MessageRoleType.User, Message = "저는 개발자입니다" }
        };

        var expectedReport = new InterviewReportModel
        {
            OverallFeedback = "전반적으로 좋은 면접이었습니다.",
            Strengths = ["명확한 의사소통", "적극적인 태도"],
            Weaknesses = ["구체적 예시 부족"],
            ChartData = new ChartDataModel
            {
                Labels = ["기술", "경험", "인성"],
                Values = [1, 1, 1]
            }
        };

        _apiClient.GenerateReportAsync(messages).Returns(expectedReport);

        // Act
        var result = await _chatService.GenerateReportAsync(messages);

        // Assert
        result.ShouldNotBeNull();
        result.OverallFeedback.ShouldBe("전반적으로 좋은 면접이었습니다.");
        result.Strengths.Count.ShouldBe(2);
        result.Weaknesses.Count.ShouldBe(1);
        result.ChartData.Values.Sum().ShouldBe(3);
    }

    [Test]
    public async Task GenerateReportAsync_WithApiFailure_ShouldReturnNull()
    {
        // Arrange
        var messages = new List<ChatMessage>
        {
            new() { Role = MessageRoleType.User, Message = "테스트 메시지" }
        };

        _apiClient.GenerateReportAsync(messages).Returns((InterviewReportModel?)null);

        // Act
        var result = await _chatService.GenerateReportAsync(messages);

        // Assert
        result.ShouldBeNull();
    }

    [Test]
    public async Task GenerateReportAsync_ShouldStoreDataInService()
    {
        // Arrange
        var messages = new List<ChatMessage>
        {
            new() { Role = MessageRoleType.User, Message = "테스트 메시지" }
        };

        var expectedReport = new InterviewReportModel
        {
            OverallFeedback = "테스트 리포트"
        };

        _apiClient.GenerateReportAsync(messages).Returns(expectedReport);

        // Act
        await _chatService.GenerateReportAsync(messages);

        // Assert
        var storedHistory = _chatService.GetLastChatHistory();
        var storedReport = _chatService.GetLastReportSummary();

        storedHistory.Count.ShouldBe(1);
        storedHistory[0].Message.ShouldBe("테스트 메시지");
        storedReport.ShouldNotBeNull();
        storedReport.OverallFeedback.ShouldBe("테스트 리포트");
    }

    [Test]
    public async Task GenerateReportAsync_WithNullReport_ShouldNotStoreData()
    {
        // Arrange
        var messages = new List<ChatMessage>
        {
            new() { Role = MessageRoleType.User, Message = "테스트 메시지" }
        };

        _apiClient.GenerateReportAsync(messages).Returns((InterviewReportModel?)null);

        // Act
        await _chatService.GenerateReportAsync(messages);

        // Assert
        var storedHistory = _chatService.GetLastChatHistory();
        var storedReport = _chatService.GetLastReportSummary();

        storedHistory.Count.ShouldBe(0);
        storedReport.ShouldBeNull();
    }

    [Test]
    public void GetLastChatHistory_InitialState_ShouldReturnEmptyList()
    {
        // Act
        var result = _chatService.GetLastChatHistory();

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(0);
    }

    [Test]
    public void GetLastReportSummary_InitialState_ShouldReturnNull()
    {
        // Act
        var result = _chatService.GetLastReportSummary();

        // Assert
        result.ShouldBeNull();
    }

    [Test]
    public async Task GenerateReportAsync_ShouldCallApiClientOnce()
    {
        // Arrange
        var messages = new List<ChatMessage>
        {
            new() { Role = MessageRoleType.User, Message = "테스트 메시지" }
        };

        var expectedReport = new InterviewReportModel
        {
            OverallFeedback = "테스트 리포트"
        };

        _apiClient.GenerateReportAsync(messages).Returns(expectedReport);

        // Act
        await _chatService.GenerateReportAsync(messages);

        // Assert
        await _apiClient.Received(1).GenerateReportAsync(messages);
    }

    [Test]
    public async Task GenerateReportAsync_WithEmptyMessages_ShouldStillCallApi()
    {
        // Arrange
        var messages = new List<ChatMessage>();

        var expectedReport = new InterviewReportModel
        {
            OverallFeedback = "면접 기록이 부족합니다."
        };

        _apiClient.GenerateReportAsync(messages).Returns(expectedReport);

        // Act
        var result = await _chatService.GenerateReportAsync(messages);

        // Assert
        result.ShouldNotBeNull();
        result.OverallFeedback.ShouldBe("면접 기록이 부족합니다.");
        await _apiClient.Received(1).GenerateReportAsync(messages);
    }

    [Test]
    public async Task GenerateReportAsync_MultipleCallsOverwriteStoredData()
    {
        // Arrange
        var firstMessages = new List<ChatMessage>
        {
            new() { Role = MessageRoleType.User, Message = "첫 번째 메시지" }
        };
        
        var secondMessages = new List<ChatMessage>
        {
            new() { Role = MessageRoleType.User, Message = "두 번째 메시지" }
        };

        var firstReport = new InterviewReportModel { OverallFeedback = "첫 번째 리포트" };
        var secondReport = new InterviewReportModel { OverallFeedback = "두 번째 리포트" };

        _apiClient.GenerateReportAsync(firstMessages).Returns(firstReport);
        _apiClient.GenerateReportAsync(secondMessages).Returns(secondReport);

        // Act
        await _chatService.GenerateReportAsync(firstMessages);
        await _chatService.GenerateReportAsync(secondMessages);

        // Assert
        var storedHistory = _chatService.GetLastChatHistory();
        var storedReport = _chatService.GetLastReportSummary();

        storedHistory.Count.ShouldBe(1);
        storedHistory[0].Message.ShouldBe("두 번째 메시지");
        storedReport.ShouldNotBeNull();
        storedReport.OverallFeedback.ShouldBe("두 번째 리포트");
    }
}
