using System.Text.Json;
using InterviewAssistant.ApiService.Services;
using InterviewAssistant.ApiService.Repositories;
using InterviewAssistant.Common.Models;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Client;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;

namespace InterviewAssistant.ApiService.Tests.Services;

[TestFixture]
public class KernelServiceReportTests
{
    private Kernel _kernel;
    private IChatCompletionService _chatCompletionService;
    private IMcpClient _mcpClient;
    private IInterviewRepository _repository;
    private KernelService _kernelService;

    [SetUp]
    public void Setup()
    {
        // Create substitutes for interfaces only
        _chatCompletionService = Substitute.For<IChatCompletionService>();
        _mcpClient = Substitute.For<IMcpClient>();
        _repository = Substitute.For<IInterviewRepository>();

        // Create real kernel with mocked chat completion service
        var builder = Kernel.CreateBuilder();
        builder.Services.AddSingleton(_chatCompletionService);
        _kernel = builder.Build();

        // Create kernel service
        _kernelService = new KernelService(_kernel, _mcpClient, _repository);
    }

    [TearDown]
    public void TearDown()
    {
        // NUnit1032 경고를 해결하기 위한 TearDown
        // IMcpClient가 IDisposable을 구현하는 경우에만 Dispose 호출
        if (_mcpClient is IDisposable disposableMcpClient)
        {
            disposableMcpClient.Dispose();
        }
    }

    [Test]
    public async Task GenerateReportAsync_WithValidMessages_ShouldReturnReport()
    {
        // Arrange
        var messages = new List<ChatMessage>
        {
            new() { Role = MessageRoleType.User, Message = "안녕하세요, 면접 준비를 도와주세요" },
            new() { Role = MessageRoleType.Assistant, Message = "안녕하세요! 자기소개 부탁드립니다." },
            new() { Role = MessageRoleType.User, Message = "저는 3년차 개발자입니다" },
            new() { Role = MessageRoleType.Assistant, Message = "어떤 기술 스택을 주로 사용하시나요?" }
        };

        var expectedReport = new InterviewReportModel
        {
            OverallFeedback = "전반적으로 좋은 면접이었습니다.",
            Strengths = ["명확한 의사소통", "풍부한 경험", "기술적 이해도"],
            Weaknesses = ["구체적 예시 부족", "질문 이해도", "답변 구조화"],
            ChartData = new ChartDataModel
            {
                Labels = ["기술", "경험", "인성"],
                Values = [2, 1, 1]
            }
        };

        var expectedJsonResponse = JsonSerializer.Serialize(expectedReport);

        // Mock chat completion service to return expected JSON
        _chatCompletionService
            .GetChatMessageContentsAsync(
                Arg.Any<ChatHistory>(),
                Arg.Any<PromptExecutionSettings>(),
                Arg.Any<Kernel>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<ChatMessageContent>>(
                [new ChatMessageContent(AuthorRole.Assistant, expectedJsonResponse)]));

        // Act
        var result = await _kernelService.GenerateReportAsync(messages);

        // Assert
        result.ShouldNotBeNull();
        result.OverallFeedback.ShouldBe("전반적으로 좋은 면접이었습니다.");
        result.Strengths.Count.ShouldBe(3);
        result.Weaknesses.Count.ShouldBe(3);
        result.ChartData.Labels.Count.ShouldBe(3);
        result.ChartData.Values.Count.ShouldBe(3);
        result.ChartData.Values.Sum().ShouldBe(4);
    }

    [Test]
    public async Task GenerateReportAsync_WithEmptyMessages_ShouldStillGenerateReport()
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

        var expectedJsonResponse = JsonSerializer.Serialize(expectedReport);

        _chatCompletionService
            .GetChatMessageContentsAsync(
                Arg.Any<ChatHistory>(),
                Arg.Any<PromptExecutionSettings>(),
                Arg.Any<Kernel>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<ChatMessageContent>>(
                [new ChatMessageContent(AuthorRole.Assistant, expectedJsonResponse)]));

        // Act
        var result = await _kernelService.GenerateReportAsync(messages);

        // Assert
        result.ShouldNotBeNull();
        result.OverallFeedback.ShouldBe("면접 기록이 부족합니다.");
    }

    [Test]
    public async Task GenerateReportAsync_WithInvalidJsonResponse_ShouldReturnErrorReport()
    {
        // Arrange
        var messages = new List<ChatMessage>
        {
            new() { Role = MessageRoleType.User, Message = "테스트 메시지" }
        };

        // Mock invalid JSON response
        _chatCompletionService
            .GetChatMessageContentsAsync(
                Arg.Any<ChatHistory>(),
                Arg.Any<PromptExecutionSettings>(),
                Arg.Any<Kernel>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<ChatMessageContent>>(
                [new ChatMessageContent(AuthorRole.Assistant, "잘못된 JSON 응답")]));

        // Act
        var result = await _kernelService.GenerateReportAsync(messages);

        // Assert
        result.ShouldNotBeNull();
        result.OverallFeedback.ShouldBe("AI가 분석 결과를 잘못된 형식으로 반환했습니다. 잠시 후 다시 시도해 주세요.");
    }

    [Test]
    public async Task GenerateReportAsync_WithCodeBlockWrappedJson_ShouldParseCorrectly()
    {
        // Arrange
        var messages = new List<ChatMessage>
        {
            new() { Role = MessageRoleType.User, Message = "테스트 메시지" }
        };

        var reportModel = new InterviewReportModel
        {
            OverallFeedback = "코드 블록으로 감싸진 응답 테스트",
            Strengths = ["강점1"],
            Weaknesses = ["약점1"],
            ChartData = new ChartDataModel
            {
                Labels = ["기술", "경험", "인성"],
                Values = [1, 0, 0]
            }
        };

        var jsonResponse = JsonSerializer.Serialize(reportModel);
        var wrappedResponse = $"```json\n{jsonResponse}\n```";

        _chatCompletionService
            .GetChatMessageContentsAsync(
                Arg.Any<ChatHistory>(),
                Arg.Any<PromptExecutionSettings>(),
                Arg.Any<Kernel>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<ChatMessageContent>>(
                [new ChatMessageContent(AuthorRole.Assistant, wrappedResponse)]));

        // Act
        var result = await _kernelService.GenerateReportAsync(messages);

        // Assert
        result.ShouldNotBeNull();
        result.OverallFeedback.ShouldBe("코드 블록으로 감싸진 응답 테스트");
        result.Strengths.Count.ShouldBe(1);
        result.Weaknesses.Count.ShouldBe(1);
    }

    [Test]
    public async Task GenerateReportAsync_WithNullOrEmptyResponse_ShouldReturnErrorReport()
    {
        // Arrange
        var messages = new List<ChatMessage>
        {
            new() { Role = MessageRoleType.User, Message = "테스트 메시지" }
        };

        _chatCompletionService
            .GetChatMessageContentsAsync(
                Arg.Any<ChatHistory>(),
                Arg.Any<PromptExecutionSettings>(),
                Arg.Any<Kernel>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<ChatMessageContent>>(
                [new ChatMessageContent(AuthorRole.Assistant, "")]));

        // Act
        var result = await _kernelService.GenerateReportAsync(messages);

        // Assert
        result.ShouldNotBeNull();
        result.OverallFeedback.ShouldBe("AI로부터 응답을 받지 못했습니다.");
    }

    [Test]
    public async Task GenerateReportAsync_WithKernelException_ShouldReturnErrorReport()
    {
        // Arrange
        var messages = new List<ChatMessage>
        {
            new() { Role = MessageRoleType.User, Message = "테스트 메시지" }
        };

        _chatCompletionService
            .GetChatMessageContentsAsync(
                Arg.Any<ChatHistory>(),
                Arg.Any<PromptExecutionSettings>(),
                Arg.Any<Kernel>(),
                Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("커널 서비스 오류"));

        // Act
        var result = await _kernelService.GenerateReportAsync(messages);

        // Assert
        result.ShouldNotBeNull();
        result.OverallFeedback.ShouldBe("리포트 생성 중 예상치 못한 오류가 발생했습니다.");
    }

    [Test]
    public async Task GenerateReportAsync_WithMultipleRoles_ShouldHandleAllRoles()
    {
        // Arrange
        var messages = new List<ChatMessage>
        {
            new() { Role = MessageRoleType.System, Message = "시스템 메시지" },
            new() { Role = MessageRoleType.User, Message = "사용자 메시지" },
            new() { Role = MessageRoleType.Assistant, Message = "어시스턴트 메시지" },
            new() { Role = MessageRoleType.Tool, Message = "도구 메시지" }
        };

        var expectedReport = new InterviewReportModel
        {
            OverallFeedback = "다양한 역할 테스트",
            Strengths = [],
            Weaknesses = [],
            ChartData = new ChartDataModel { Labels = [], Values = [] }
        };

        var expectedJsonResponse = JsonSerializer.Serialize(expectedReport);

        _chatCompletionService
            .GetChatMessageContentsAsync(
                Arg.Any<ChatHistory>(),
                Arg.Any<PromptExecutionSettings>(),
                Arg.Any<Kernel>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<ChatMessageContent>>(
                [new ChatMessageContent(AuthorRole.Assistant, expectedJsonResponse)]));

        // Act
        var result = await _kernelService.GenerateReportAsync(messages);

        // Assert
        result.ShouldNotBeNull();
        result.OverallFeedback.ShouldBe("다양한 역할 테스트");
    }
}
