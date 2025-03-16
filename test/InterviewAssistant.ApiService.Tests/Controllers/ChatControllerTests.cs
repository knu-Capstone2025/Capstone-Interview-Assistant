using InterviewAssistant.ApiService.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Moq;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace InterviewAssistant.ApiService.Tests
{
    public class ChatControllerTests
    {
        private readonly Mock<ILogger<ChatController>> _mockLogger;
        private readonly Kernel _kernel;

        public ChatControllerTests()
        {
            _mockLogger = new Mock<ILogger<ChatController>>();
            
            // Kernel 생성 및 구성
            var builder = Kernel.CreateBuilder();
            _kernel = builder.Build();
        }

        [Fact]
        public async Task PostMessage_WithEmptyMessage_ReturnsBadRequest()
        {
            // Arrange
            var request = new ChatRequest { Message = "" };
            var controller = new ChatController(_mockLogger.Object, _kernel);

            // Act
            var result = await controller.PostMessage(request);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task ResetChat_ClearsConversation()
        {
            // Arrange
            var controller = new ChatController(_mockLogger.Object, _kernel);

            // Act
            var resetResult = controller.ResetChat();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(resetResult);
            
            // JsonElement을 사용하여 동적 속성 접근
            var jsonElement = JsonSerializer.Deserialize<JsonElement>(
                JsonSerializer.Serialize(okResult.Value));
            
            // message 속성이 있는지 확인
            Assert.True(jsonElement.TryGetProperty("message", out JsonElement messageElement));
            Assert.Equal("대화가 초기화되었습니다.", messageElement.GetString());
        }
    }
}