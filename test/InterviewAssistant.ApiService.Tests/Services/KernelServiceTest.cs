#pragma warning disable CS1998

using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using InterviewAssistant.ApiService.Services;
using Microsoft.Extensions.DependencyInjection;

namespace InterviewAssistant.ApiService.Tests.Services
{
    [TestFixture]
    public class KernelServiceTests
    {
        private Kernel _kernel;
        private Mock<IChatCompletionService> _mockChatCompletionService;
        private Mock<IConfiguration> _mockConfiguration;
        private KernelService _kernelService;
        private const string ServiceId = "testServiceId";

        [SetUp]
        public void Setup()
        {
            // Create mock services
            _mockChatCompletionService = new Mock<IChatCompletionService>();
            _mockConfiguration = new Mock<IConfiguration>();

            // Setup configuration
            _mockConfiguration.Setup(c => c["SemanticKernel:ServiceId"]).Returns(ServiceId);
            
            // Create a kernel with our mock service
            var builder = Kernel.CreateBuilder();
            builder.Services.AddKeyedSingleton<IChatCompletionService>(ServiceId, _mockChatCompletionService.Object);
            _kernel = builder.Build();

            // Create kernel service
            _kernelService = new KernelService(_kernel, _mockConfiguration.Object);
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

            // Setup mock chat completion service to return our expected results
            _mockChatCompletionService
                .Setup(s => s.GetStreamingChatMessageContentsAsync(
                    It.IsAny<ChatHistory>(), 
                    It.IsAny<PromptExecutionSettings>(), 
                    It.IsAny<Kernel>(), 
                    It.IsAny<CancellationToken>()))
                .Returns(expectedResults.ToAsyncEnumerable());

            // Act
            var results = new List<string>();
            await foreach (var result in _kernelService.CompleteChatStreamingAsync(messages))
            {
                results.Add(result);
            }

            // Assert
            Assert.That(results.Count, Is.EqualTo(expectedResults.Count));
            for (int i = 0; i < expectedResults.Count; i++)
            {
                Assert.That(results[i], Is.EqualTo(expectedResults[i].ToString()));
            }

            // Verify that the kernel service called the chat completion service with the right parameters
            _mockChatCompletionService.Verify(s => s.GetStreamingChatMessageContentsAsync(
                It.Is<ChatHistory>(h => h.Count == messages.Count),
                It.IsAny<PromptExecutionSettings>(),
                It.Is<Kernel>(k => k == _kernel),
                It.IsAny<CancellationToken>()
            ), Times.Once);
        }

        [Test]
        public async Task CompleteChatStreamingAsync_ShouldHandleEmptyMessages()
        {
            // Arrange
            var messages = new List<ChatMessageContent>();
            var expectedResults = new List<StreamingChatMessageContent>();

            _mockChatCompletionService
                .Setup(s => s.GetStreamingChatMessageContentsAsync(
                    It.IsAny<ChatHistory>(),
                    It.IsAny<PromptExecutionSettings>(),
                    It.IsAny<Kernel>(),
                    It.IsAny<CancellationToken>()))
                .Returns(expectedResults.ToAsyncEnumerable());

            // Act
            var results = new List<string>();
            await foreach (var result in _kernelService.CompleteChatStreamingAsync(messages))
            {
                results.Add(result);
            }

            // Assert
            Assert.That(results, Is.Empty);
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

            _mockChatCompletionService
                .Setup(s => s.GetStreamingChatMessageContentsAsync(
                    It.IsAny<ChatHistory>(),
                    It.IsAny<PromptExecutionSettings>(),
                    It.IsAny<Kernel>(),
                    It.IsAny<CancellationToken>()))
                .Returns(expectedResults.ToAsyncEnumerable());

            // Act
            var results = new List<string>();
            await foreach (var result in _kernelService.CompleteChatStreamingAsync(messages))
            {
                results.Add(result);
            }

            // Assert
            Assert.That(results.Count, Is.EqualTo(expectedResults.Count));
            for (int i = 0; i < expectedResults.Count; i++)
            {
                Assert.That(results[i], Is.EqualTo(expectedResults[i].ToString()));
            }

            // Verify the chat history passed to the service contains all our original messages
            _mockChatCompletionService.Verify(s => s.GetStreamingChatMessageContentsAsync(
                It.Is<ChatHistory>(h => h.Count == messages.Count),
                It.IsAny<PromptExecutionSettings>(),
                It.Is<Kernel>(k => k == _kernel),
                It.IsAny<CancellationToken>()
            ), Times.Once);
        }
    }

    // Extension method to convert IEnumerable to IAsyncEnumerable for testing
    public static class EnumerableExtensions
    {
        public static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(this IEnumerable<T> source)
        {
            foreach (var item in source)
            {
                yield return item;
            }
        }
    }
}
