/*
ChatApiClientTests.cs
ChatApiClient 클래스의 기능을 검증하는 단위 테스트입니다.
*/

using InterviewAssistant.Common.Models;
using InterviewAssistant.Web.Clients;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace InterviewAssistant.Web.Tests.Clients
{
    [TestFixture]
    public class ChatApiClientTests
    {
        private IChatApiClient _chatApiClient;
        private ILoggerFactory _loggerFactory;
        private const string HardcodedResponse = "Lorem ipsum dolor sit amet, consectetur adipiscing elit.";


        [SetUp]
        public void Setup()
        {
            // 로거 팩토리 대체 객체 생성
            _loggerFactory = Substitute.For<ILoggerFactory>();
            
            // 테스트할 ChatApiClient 인스턴스 생성
            _chatApiClient = new ChatApiClient(_loggerFactory);
        }

        [TearDown]
        public void Cleanup()
        {
            // 리소스 정리
            _loggerFactory.Dispose();
        }

        // 테스트 1: 메시지 전송이 하드코딩된 응답을 반환하는지 확인
        [Test]
        public async Task SendMessageAsync_ReturnsHardcodedResponse()
        {
            // Arrange
            var request = new ChatRequest { 
                Messages = new List<ChatMessage> { 
                    new ChatMessage { Role = MessageRoleType.User, Message = "안녕하세요" } 
                } 
            };
            
            // Act
            var result = await _chatApiClient.SendMessageAsync(request);
            
            // Assert
            result.ShouldNotBeNull();
            result.Message.ShouldBe(HardcodedResponse);
        }
        
        // 테스트 2: 빈 메시지 목록에서도 응답을 정상적으로 반환하는지 확인
        [Test]
        public async Task SendMessageAsync_WithEmptyMessages_StillReturnsResponse()
        {
            // Arrange
            var request = new ChatRequest { 
                Messages = new List<ChatMessage>() // 빈 목록 사용
            };
            
            // Act
            var result = await _chatApiClient.SendMessageAsync(request);
            
            // Assert
            result.ShouldNotBeNull();
            result.Message.ShouldBe(HardcodedResponse);
        }
    }
}