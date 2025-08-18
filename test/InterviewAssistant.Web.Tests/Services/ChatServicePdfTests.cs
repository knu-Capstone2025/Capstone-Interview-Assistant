using InterviewAssistant.Common.Models;
using InterviewAssistant.Web.Clients;
using InterviewAssistant.Web.Services;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;

namespace InterviewAssistant.Web.Tests.Services;

[TestFixture]
public class ChatServicePdfTests
{
    private IChatApiClient _apiClient;
    private IChatService _chatService;
    private ILoggerFactory _loggerFactory;

    [SetUp]
    public void Setup()
    {
        _apiClient = Substitute.For<IChatApiClient>();
        _loggerFactory = Substitute.For<ILoggerFactory>();
        _loggerFactory.CreateLogger<ChatService>().Returns(Substitute.For<ILogger<ChatService>>());
        
        _chatService = new ChatService(_apiClient, _loggerFactory);
    }

    [TearDown]
    public void TearDown()
    {
        _loggerFactory?.Dispose();
    }

    [Test]
    public async Task DownloadReportPdfAsync_WithValidData_ShouldReturnPdfBytes()
    {
        // Arrange
        var report = new InterviewReportModel
        {
            OverallFeedback = "테스트 피드백",
            Strengths = ["강점1"],
            Weaknesses = ["개선점1"]
        };

        var chatHistory = new List<ChatMessage>
        {
            new() { Role = MessageRoleType.User, Message = "테스트 메시지" }
        };

        var expectedPdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 }; // %PDF
        _apiClient.DownloadReportPdfAsync(report, chatHistory).Returns(expectedPdfBytes);

        // Act
        var result = await _chatService.DownloadReportPdfAsync(report, chatHistory);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBe(expectedPdfBytes);
        await _apiClient.Received(1).DownloadReportPdfAsync(report, chatHistory);
    }

    [Test]
    public async Task DownloadReportPdfAsync_WhenApiClientReturnsNull_ShouldReturnNull()
    {
        // Arrange
        var report = new InterviewReportModel();
        var chatHistory = new List<ChatMessage>();

        _apiClient.DownloadReportPdfAsync(report, chatHistory).Returns(Task.FromResult<byte[]?>(null));

        // Act
        var result = await _chatService.DownloadReportPdfAsync(report, chatHistory);

        // Assert
        result.ShouldBeNull();
    }

    [Test]
    public async Task DownloadReportPdfAsync_ShouldCallApiClientOnce()
    {
        // Arrange
        var report = new InterviewReportModel { OverallFeedback = "테스트" };
        var chatHistory = new List<ChatMessage>
        {
            new() { Role = MessageRoleType.Assistant, Message = "안녕하세요" }
        };

        var pdfBytes = new byte[] { 1, 2, 3, 4, 5 };
        _apiClient.DownloadReportPdfAsync(report, chatHistory).Returns(pdfBytes);

        // Act
        await _chatService.DownloadReportPdfAsync(report, chatHistory);

        // Assert
        await _apiClient.Received(1).DownloadReportPdfAsync(report, chatHistory);
    }
}
