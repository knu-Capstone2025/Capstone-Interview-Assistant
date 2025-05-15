#pragma warning disable CS1998

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using InterviewAssistant.ApiService.Services;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;

using ModelContextProtocol.Client;

using NSubstitute;
using NSubstitute.ReturnsExtensions;

using NUnit.Framework;

using Shouldly;

namespace InterviewAssistant.ApiService.Tests.Services;

[TestFixture]
public class KernelServiceTests
{
    private Kernel _kernel;
    private IChatCompletionService _chatCompletionService;
    private IConfiguration _configuration;
    private KernelService _kernelService;

    private Task<IMcpClient> _mcpClient;
    private const string ServiceId = "testServiceId";
    private const string TestResume = "테스트 이력서 내용";
    private const string TestJobDescription = "테스트 직무 설명";

    [SetUp]
    public void Setup()
    {
        // Create substitutes
        _chatCompletionService = Substitute.For<IChatCompletionService>();
        _configuration = Substitute.For<IConfiguration>();

        // Setup configuration
        _configuration["SemanticKernel:ServiceId"].Returns(ServiceId);

        // Create a kernel with our substitute service
        var builder = Kernel.CreateBuilder();
        builder.Services.AddKeyedSingleton<IChatCompletionService>(ServiceId, _chatCompletionService);
        _kernel = builder.Build();

        // Create kernel service
        _kernelService = new KernelService(_kernel, _configuration, _mcpClient);
        
        // Setup File.Exists to return true for our YAML path
        var fileSystem = Substitute.For<IFileSystem>();
        var filePath = Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!,
            "Agents/InterviewAgents/InterviewAgent.yaml");
            
    }

    [Test]
    public async Task InvokeInterviewAgentAsync_WithNoMessages_ShouldStartInterview()
    {
        // Arrange
        SetupAgentResponse(new List<AgentResponse> {
            new AgentResponse { Message = new ChatMessageContent(AuthorRole.Assistant, "안녕하세요, 면접을 시작하겠습니다.") }
        });

        // Act
        var results = new List<string>();
        await foreach (var result in _kernelService.InvokeInterviewAgentAsync(TestResume, TestJobDescription))
        {
            results.Add(result);
        }

        // Assert
        results.ShouldNotBeEmpty();
        results[0].ShouldBe("안녕하세요, 면접을 시작하겠습니다.");
    }

    [Test]
    public async Task InvokeInterviewAgentAsync_WithMessages_ShouldContinueConversation()
    {
        // Arrange
        var messages = new List<ChatMessageContent>
        {
            new ChatMessageContent(AuthorRole.User, "안녕하세요"),
            new ChatMessageContent(AuthorRole.Assistant, "안녕하세요, 어떤 질문이 있으신가요?"),
            new ChatMessageContent(AuthorRole.User, "제 경력에 대해 질문해주세요")
        };

        SetupAgentResponse(new List<AgentResponse> {
            new AgentResponse { Message = new ChatMessageContent(AuthorRole.Assistant, "귀하의 이전 직무에서의 주요 성과는 무엇인가요?") }
        });

        // Act
        var results = new List<string>();
        await foreach (var result in _kernelService.InvokeInterviewAgentAsync(TestResume, TestJobDescription, messages))
        {
            results.Add(result);
        }

        // Assert
        results.ShouldNotBeEmpty();
        results[0].ShouldBe("귀하의 이전 직무에서의 주요 성과는 무엇인가요?");
    }

    [Test]
    public async Task InvokeInterviewAgentAsync_WithEmptyMessages_ShouldHandleGracefully()
    {
        // Arrange
        var messages = new List<ChatMessageContent>();

        SetupAgentResponse(new List<AgentResponse> {
            new AgentResponse { Message = new ChatMessageContent(AuthorRole.Assistant, "면접을 시작하겠습니다. 자기소개 부탁드립니다.") }
        });

        // Act
        var results = new List<string>();
        await foreach (var result in _kernelService.InvokeInterviewAgentAsync(TestResume, TestJobDescription, messages))
        {
            results.Add(result);
        }

        // Assert
        results.ShouldNotBeEmpty();
        results[0].ShouldBe("면접을 시작하겠습니다. 자기소개 부탁드립니다.");
    }

    private void SetupAgentResponse(List<AgentResponse> responses)
    {
        _chatCompletionService
            .GetStreamingChatMessageContentsAsync(
                Arg.Any<ChatHistory>(),
                Arg.Any<PromptExecutionSettings>(),
                Arg.Any<Kernel>(),
                Arg.Any<CancellationToken>())
            .Returns(responses.Select(r => new StreamingChatMessageContent(r.Message.Role, r.Message.Content!)).ToAsyncEnumerable());
    }
}

public class AgentResponse
{
    public ChatMessageContent Message { get; set; } = new ChatMessageContent(AuthorRole.Assistant, string.Empty);
}

public interface IFileSystem
{
    bool Exists(string path);
    string ReadAllText(string path);
}
