/*
ChatServiceTests.cs
ChatService 클래스의 비즈니스 로직을 검증하는 단위 테스트입니다.
IChatApiClient를 대체 객체로 설정하여 실제 API 호출 없이도 테스트합니다.
*/

using InterviewAssistant.Common.Models;
using InterviewAssistant.Web.Services;
using InterviewAssistant.Web.Clients;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace InterviewAssistant.Web.Tests.Services
{
    [TestFixture]
    public class ChatServiceTests
    {
        private IChatApiClient _apiClient;
        private ILoggerFactory _loggerFactory;
        private IChatService _chatService;

        [SetUp]
        public void Setup()
        {
            // API 클라이언트와 로거를 대체 객체로 생성
            _apiClient = Substitute.For<IChatApiClient>();
            _loggerFactory = Substitute.For<ILoggerFactory>();
            
            // 테스트할 ChatService 인스턴스 생성
            _chatService = new ChatService(_apiClient, _loggerFactory);
        }

        // TearDown 메서드 추가 - ILoggerFactory 정리
        [TearDown]
        public void Cleanup()
        {
            _loggerFactory?.Dispose();
        }

        // 테스트 1: 메시지 전송이 성공할 때 응답을 올바르게 반환하는지 검증
        [Test]
        public async Task SendMessageAsync_WhenSuccessful_ReturnsResponse()
        {
            // Arrange - 대체 API 클라이언트가 성공적인 응답을 반환하도록 설정
            var expectedResponse = new ChatResponse { Message = "테스트 응답입니다" };
            _apiClient.SendMessageAsync(Arg.Any<ChatRequest>()).Returns(expectedResponse);
            
            // Act - ChatService의 메시지 전송 메서드 호출
            ChatResponse? result = await _chatService.SendMessageAsync("안녕하세요");
            
            // Assert - 결과 검증
            result.ShouldNotBeNull();
            result.Message.ShouldBe("테스트 응답입니다");
            
            // API 클라이언트가 올바른 요청으로 호출되었는지 확인
            await _apiClient.Received(1).SendMessageAsync(Arg.Is<ChatRequest>(
                req => req.Messages.Count == 1 && 
                      req.Messages[0].Role == MessageRoleType.User && 
                      req.Messages[0].Message == "안녕하세요"
            ));
        }
        
        // 테스트 2: API 호출이 실패할 때 null을 반환하는지 검증
        [Test]
        public async Task SendMessageAsync_WhenApiFails_ReturnsNull()
        {
            // Arrange - 대체 API 클라이언트가 null을 반환하도록 설정
            _apiClient.SendMessageAsync(Arg.Any<ChatRequest>()).Returns(Task.FromResult<ChatResponse?>(null));
            
            // Act - ChatService의 메시지 전송 메서드 호출
            ChatResponse? result = await _chatService.SendMessageAsync("안녕하세요");
            
            // Assert - 결과가 null인지 확인
            result.ShouldBeNull();
        }
        
        // 테스트 3: 공백 메시지 전송 시 null을 반환하는지 검증
        [Test]
        public async Task SendMessageAsync_WithEmptyMessage_ReturnsNull()
        {
            // Arrange - 빈 메시지 준비
            string emptyMessage = "   ";
            
            // Act - ChatService의 메시지 전송 메서드 호출
            ChatResponse? result = await _chatService.SendMessageAsync(emptyMessage);
            
            // Assert - 결과가 null인지 확인
            result.ShouldBeNull();
            
            // API 클라이언트가 호출되지 않았는지 확인
            await _apiClient.DidNotReceive().SendMessageAsync(Arg.Any<ChatRequest>());
        }
        
    }
}