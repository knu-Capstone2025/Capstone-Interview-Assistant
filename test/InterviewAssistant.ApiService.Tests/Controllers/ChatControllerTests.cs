using InterviewAssistant.ApiService.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace InterviewAssistant.ApiService.Tests
{
    [TestFixture]
    public class ChatControllerTests
    {
        private Mock<ILogger<ChatController>> _mockLogger;
        private Kernel _kernel;

        [SetUp]
        public void Setup()
        {
            _mockLogger = new Mock<ILogger<ChatController>>();
            
            // Kernel 생성 및 구성
            var builder = Kernel.CreateBuilder();
            _kernel = builder.Build();
        }

        [Test]
        public async Task PostMessage_WithEmptyMessage_ReturnsBadRequest()
        {
            // Arrange
            var request = new ChatRequest { Message = "" };
            var controller = new ChatController(_mockLogger.Object, _kernel);

            // Act
            var result = await controller.PostMessage(request);

            // Assert
            Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public void ResetChat_ClearsConversation()
        {
            // Arrange
            var controller = new ChatController(_mockLogger.Object, _kernel);

            // Act
            var resetResult = controller.ResetChat();

            // Assert
            Assert.That(resetResult, Is.InstanceOf<OkObjectResult>());
            
            // NUnit에서 JsonElement를 사용하여 동적 속성 접근
            var okResult = (OkObjectResult)resetResult;
            var jsonElement = JsonSerializer.Deserialize<JsonElement>(
                JsonSerializer.Serialize(okResult.Value));
            
            // message 속성이 있는지 확인
            Assert.That(jsonElement.TryGetProperty("message", out JsonElement messageElement), Is.True);
            Assert.That(messageElement.GetString(), Is.EqualTo("대화가 초기화되었습니다."));
        }

    }
}