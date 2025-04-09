using InterviewAssistant.ApiService.Delegates;
using InterviewAssistant.Common.Models;
using InterviewAssistant.ApiService.Services;

using NSubstitute;

using Shouldly;

using ChatMessageContent = Microsoft.SemanticKernel.ChatMessageContent;

namespace InterviewAssistant.Tests
{
    [TestFixture]
    public class ChatCompletionDelegateTests
    {
        [TestCase("This is response 1", "And response 2")]
        public async Task PostChatCompletionAsync_ShouldReturnStreamedChatResponses(params string[] expectedResponses)
        {
            // Arrange
            var messages = new List<ChatMessage>
            {
                new ChatMessage { Role = MessageRoleType.User, Message = "Hello!" },
                new ChatMessage { Role = MessageRoleType.Assistant, Message = "Hi!" }
            };

            var request = new ChatRequest { Messages = messages };

            var mockService = Substitute.For<IKernelService>();
            var simulatedStream = GetFakeStreamedResponses(expectedResponses);
            mockService.CompleteChatStreamingAsync(Arg.Any<IEnumerable<ChatMessageContent>>())
                       .Returns(simulatedStream);

            // Act
            var result = ChatCompletionDelegate.PostChatCompletionAsync(request, mockService);

            var responses = new List<ChatResponse>();
            await foreach (var response in result)
            {
                responses.Add(response);
            }

            // Assert
            responses.Count.ShouldBe(expectedResponses.Length);
            for (var i = 0; i < expectedResponses.Length; i++)
            {
                responses[i].Message.ShouldBe(expectedResponses[i]);
            }
        }

        private async IAsyncEnumerable<string> GetFakeStreamedResponses(params string[] responses)
        {
            foreach (var response in responses)
            {
                yield return response;
                await Task.Yield(); // mimic async behavior
            }
        }
    }
}