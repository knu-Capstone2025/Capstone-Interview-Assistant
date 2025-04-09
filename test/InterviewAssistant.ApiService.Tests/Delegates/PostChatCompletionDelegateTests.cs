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
        [Test]
        public async Task PostChatCompletionAsync_ShouldReturnStreamedChatResponses()
        {
            // Arrange
            var messages = new List<ChatMessage>
            {
                new ChatMessage { Role = MessageRoleType.User, Message = "Hello!" },
                new ChatMessage { Role = MessageRoleType.Assistant, Message = "Hi!" }
            };

            var request = new ChatRequest { Messages = messages };

            var mockService = Substitute.For<IKernelService>();
            var simulatedStream = GetFakeStreamedResponses("This is response 1", "And response 2");
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
            responses.Count.ShouldBe(2);
            responses[0].Message.ShouldBe("This is response 1");
            responses[1].Message.ShouldBe("And response 2");
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
