#pragma warning disable CS1998

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Shouldly;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

using NSubstitute;

using NUnit.Framework;

using InterviewAssistant.ApiService.Services;

namespace InterviewAssistant.ApiService.Tests.Services;

[TestFixture]
public class KernelServiceTests
{
    private Kernel _kernel;
    private IChatCompletionService _chatCompletionService;
    private IConfiguration _configuration;
    private KernelService _kernelService;
    private const string ServiceId = "testServiceId";

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
        _kernelService = new KernelService(_kernel, _configuration);
    }

    [Test]
    public async Task CompleteChatStreamingAsync_ShouldReturnStreamingResults()
    {
        // Arrange
        var messages = new List<ChatMessageContent>
            {
                new ChatMessageContent(AuthorRole.User, "Hello")
            };

        // Create a list of expected streaming results
        var expectedResults = new List<StreamingChatMessageContent>
            {
                new StreamingChatMessageContent(AuthorRole.Assistant, "Hello"),
                new StreamingChatMessageContent(AuthorRole.Assistant, " there"),
                new StreamingChatMessageContent(AuthorRole.Assistant, "!")
            };

        // Setup substitute chat completion service to return our expected results
        _chatCompletionService
            .GetStreamingChatMessageContentsAsync(
                Arg.Any<ChatHistory>(),
                Arg.Any<PromptExecutionSettings>(),
                Arg.Any<Kernel>(),
                Arg.Any<CancellationToken>())
            .Returns(expectedResults.ToAsyncEnumerable());

        // Act
        var results = new List<string>();
        await foreach (var result in _kernelService.CompleteChatStreamingAsync(messages))
        {
            results.Add(result);
        }

        // Assert
        results.Count.ShouldBe(expectedResults.Count);
        for (int i = 0; i < expectedResults.Count; i++)
        {
            results[i].ShouldBe(expectedResults[i].ToString());
        }

    }

    [Test]
    public async Task CompleteChatStreamingAsync_ShouldHandleEmptyMessages()
    {
        // Arrange
        var messages = new List<ChatMessageContent>();
        var expectedResults = new List<StreamingChatMessageContent>();

        _chatCompletionService
            .GetStreamingChatMessageContentsAsync(
                Arg.Any<ChatHistory>(),
                Arg.Any<PromptExecutionSettings>(),
                Arg.Any<Kernel>(),
                Arg.Any<CancellationToken>())
            .Returns(expectedResults.ToAsyncEnumerable());

        // Act
        var results = new List<string>();
        await foreach (var result in _kernelService.CompleteChatStreamingAsync(messages))
        {
            results.Add(result);
        }

        // Assert
        results.ShouldBeEmpty();
    }

    [Test]
    public async Task CompleteChatStreamingAsync_ShouldHandleMultipleMessages()
    {
        // Arrange
        var messages = new List<ChatMessageContent>
            {
                new ChatMessageContent(AuthorRole.User, "Hello"),
                new ChatMessageContent(AuthorRole.Assistant, "Hi there!"),
                new ChatMessageContent(AuthorRole.User, "How are you?")
            };

        var expectedResults = new List<StreamingChatMessageContent>
            {
                new StreamingChatMessageContent(AuthorRole.Assistant, "I'm"),
                new StreamingChatMessageContent(AuthorRole.Assistant, " doing"),
                new StreamingChatMessageContent(AuthorRole.Assistant, " great,"),
                new StreamingChatMessageContent(AuthorRole.Assistant, " thanks"),
                new StreamingChatMessageContent(AuthorRole.Assistant, " for"),
                new StreamingChatMessageContent(AuthorRole.Assistant, " asking!")
            };

        _chatCompletionService
            .GetStreamingChatMessageContentsAsync(
                Arg.Any<ChatHistory>(),
                Arg.Any<PromptExecutionSettings>(),
                Arg.Any<Kernel>(),
                Arg.Any<CancellationToken>())
            .Returns(expectedResults.ToAsyncEnumerable());

        // Act
        var results = new List<string>();
        await foreach (var result in _kernelService.CompleteChatStreamingAsync(messages))
        {
            results.Add(result);
        }

        // Assert
        results.Count.ShouldBe(expectedResults.Count);
        for (int i = 0; i < expectedResults.Count; i++)
        {
            results[i].ShouldBe(expectedResults[i].ToString());
        }

    }
}
