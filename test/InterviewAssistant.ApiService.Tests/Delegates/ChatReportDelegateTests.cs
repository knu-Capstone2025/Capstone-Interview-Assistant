using InterviewAssistant.ApiService.Delegates;
using InterviewAssistant.ApiService.Services;
using InterviewAssistant.Common.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;

namespace InterviewAssistant.ApiService.Tests.Delegates;

[TestFixture]
public class ChatReportDelegateTests
{
    private IKernelService _kernelService;
    private IServiceProvider _serviceProvider;

    [SetUp]
    public void Setup()
    {
        _kernelService = Substitute.For<IKernelService>();
        
        // 완전한 서비스 프로바이더 설정
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IServiceProvider>(sp => sp);
        _serviceProvider = services.BuildServiceProvider();
    }

    [TearDown]
    public void TearDown()
    {
        if (_serviceProvider is IDisposable disposableServiceProvider)
        {
            disposableServiceProvider.Dispose();
        }
    }

    private HttpContext CreateHttpContext()
    {
        return new DefaultHttpContext
        {
            RequestServices = _serviceProvider
        };
    }

    [Test]
    public async Task PostChatReportAsync_WithValidMessages_ShouldReturnOkResult()
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

        _kernelService.GenerateReportAsync(messages).Returns(expectedReport);

        // Act
        var result = await ChatReportDelegate.PostChatReportAsync(messages, _kernelService);

        // Assert
        result.ShouldNotBeNull();
        
        // HttpContext with proper ServiceProvider setup
        var httpContext = CreateHttpContext();
        await result.ExecuteAsync(httpContext);
        
        httpContext.Response.StatusCode.ShouldBe(200);
    }

    [Test]
    public async Task PostChatReportAsync_WithEmptyMessages_ShouldStillReturnOkResult()
    {
        // Arrange
        var messages = new List<ChatMessage>();

        var expectedReport = new InterviewReportModel
        {
            OverallFeedback = "면접 기록이 부족합니다.",
            Strengths = [],
            Weaknesses = [],
            ChartData = new ChartDataModel
            {
                Labels = ["기술", "경험", "인성"],
                Values = [0, 0, 0]
            }
        };

        _kernelService.GenerateReportAsync(messages).Returns(expectedReport);

        // Act
        var result = await ChatReportDelegate.PostChatReportAsync(messages, _kernelService);

        // Assert
        result.ShouldNotBeNull();
        
        var httpContext = CreateHttpContext();
        await result.ExecuteAsync(httpContext);
        
        httpContext.Response.StatusCode.ShouldBe(200);
    }

    [Test]
    public async Task PostChatReportAsync_WithKernelServiceException_ShouldThrowException()
    {
        // Arrange
        var messages = new List<ChatMessage>
        {
            new() { Role = MessageRoleType.User, Message = "테스트 메시지" }
        };

        _kernelService.GenerateReportAsync(messages)
            .Throws(new InvalidOperationException("커널 서비스 오류"));

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(
            async () => await ChatReportDelegate.PostChatReportAsync(messages, _kernelService));
        
        exception.Message.ShouldBe("커널 서비스 오류");
    }

    [Test]
    public async Task PostChatReportAsync_WithNullReport_ShouldReturnOkWithNull()
    {
        // Arrange
        var messages = new List<ChatMessage>
        {
            new() { Role = MessageRoleType.User, Message = "테스트 메시지" }
        };

        _kernelService.GenerateReportAsync(messages).Returns(Task.FromResult<InterviewReportModel?>(null));

        // Act
        var result = await ChatReportDelegate.PostChatReportAsync(messages, _kernelService);

        // Assert
        result.ShouldNotBeNull();
        
        var httpContext = CreateHttpContext();
        await result.ExecuteAsync(httpContext);
        
        httpContext.Response.StatusCode.ShouldBe(200);
    }

    [Test]
    public async Task PostChatReportAsync_ShouldCallKernelServiceOnce()
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

        _kernelService.GenerateReportAsync(messages).Returns(expectedReport);

        // Act
        await ChatReportDelegate.PostChatReportAsync(messages, _kernelService);

        // Assert
        await _kernelService.Received(1).GenerateReportAsync(messages);
    }

    [Test]
    public async Task PostChatReportAsync_WithLargeMessageList_ShouldProcessCorrectly()
    {
        // Arrange
        var messages = new List<ChatMessage>();
        for (int i = 0; i < 100; i++)
        {
            messages.Add(new ChatMessage
            {
                Role = i % 2 == 0 ? MessageRoleType.User : MessageRoleType.Assistant,
                Message = $"메시지 {i}"
            });
        }

        var expectedReport = new InterviewReportModel
        {
            OverallFeedback = "대화가 길었습니다.",
            Strengths = ["인내심"],
            Weaknesses = ["간결함 부족"],
            ChartData = new ChartDataModel
            {
                Labels = ["기술", "경험", "인성"],
                Values = [30, 30, 40]
            }
        };

        _kernelService.GenerateReportAsync(messages).Returns(expectedReport);

        // Act
        var result = await ChatReportDelegate.PostChatReportAsync(messages, _kernelService);

        // Assert
        result.ShouldNotBeNull();
        
        var httpContext = CreateHttpContext();
        await result.ExecuteAsync(httpContext);
        
        httpContext.Response.StatusCode.ShouldBe(200);
        await _kernelService.Received(1).GenerateReportAsync(messages);
    }
}
