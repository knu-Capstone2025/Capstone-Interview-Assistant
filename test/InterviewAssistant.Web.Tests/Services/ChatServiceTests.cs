/*
ChatService 클래스의 기능을 검증하는 단위 테스트입니다.
HTTP 통신 부분을 모의 객체(Mock)로 대체하여 네트워크 없이도 테스트합니다.
*/

using InterviewAssistant.Common.Models;
using InterviewAssistant.Web.Services;
using Moq;
using Moq.Protected;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Xunit;
using FluentAssertions;

namespace InterviewAssistant.Web.Tests.Services
{
    public class ChatServiceTests
    {
        // 테스트 1: 메시지 전송이 성공하는 경우
        [Fact]
        public async Task SendMessageAsync_WhenSuccessful_ReturnsResponse()
        {
            // Arrange (준비) - 테스트에 필요한 모든 것을 준비합니다
            // 1. HttpClient가 예상대로 동작하도록 "가짜(Mock)" 응답을 설정합니다
            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            
            // 성공적인 응답 설정
            var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(new ChatResponse { Message = "테스트 응답입니다" }),
                    Encoding.UTF8,
                    "application/json"
                )
            };
            
            // Mock 핸들러가 어떤 요청이 와도 미리 준비한 응답을 반환하도록 설정
            mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(responseMessage);
            
            // 가짜 HttpMessageHandler로 실제 HttpClient 생성
            var httpClient = new HttpClient(mockHttpMessageHandler.Object)
            {
                BaseAddress = new Uri("https://fakechatapi.com/")
            };
            
            // 테스트할 서비스 생성
            var chatService = new ChatService(httpClient);
            
            // Act (실행) - 테스트하려는 메서드를 실행합니다
            var result = await chatService.SendMessageAsync("안녕하세요");
            
            // Assert (검증) - 예상한 결과가 나왔는지 확인합니다
            // FluentAssertions 사용하도록 변경
            result.Should().NotBeNull();
            result.Message.Should().Be("테스트 응답입니다");
            
            // HttpClient가 올바른 URL로 POST 요청을 보냈는지 확인
            mockHttpMessageHandler
                .Protected()
                .Verify(
                    "SendAsync",
                    Times.Once(),
                    ItExpr.Is<HttpRequestMessage>(req => 
                        req.Method == HttpMethod.Post && 
                        req.RequestUri != null && // null 체크 추가
                        req.RequestUri.ToString() == "https://fakechatapi.com/api/v1/chat"),
                    ItExpr.IsAny<CancellationToken>()
                );
        }
        
        // 테스트 2: API가 실패 응답을 보내는 경우
        [Fact]
        public async Task SendMessageAsync_WhenApiFails_ReturnsNull()
        {
            // Arrange - 실패 응답 설정
            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            
            mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.InternalServerError));
            
            var httpClient = new HttpClient(mockHttpMessageHandler.Object)
            {
                BaseAddress = new Uri("https://fakechatapi.com/")
            };
            
            var chatService = new ChatService(httpClient);
            
            // Act
            var result = await chatService.SendMessageAsync("안녕하세요");
            
            // Assert - FluentAssertions 사용하도록 변경
            result.Should().BeNull(); // 실패 시 null을 반환하는지 확인
        }
        
        // 테스트 3: ResetChat 메서드가 올바르게 동작하는지 확인
        [Fact]
        public async Task ResetChatAsync_CallsCorrectEndpoint()
        {
            // Arrange
            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            
            mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));
            
            var httpClient = new HttpClient(mockHttpMessageHandler.Object)
            {
                BaseAddress = new Uri("https://fakechatapi.com/")
            };
            
            var chatService = new ChatService(httpClient);
            
            // Act
            await chatService.ResetChatAsync();
            
            // Assert - reset 엔드포인트로 POST 요청이 갔는지 확인
            mockHttpMessageHandler
                .Protected()
                .Verify(
                    "SendAsync",
                    Times.Once(),
                    ItExpr.Is<HttpRequestMessage>(req => 
                        req.Method == HttpMethod.Post && 
                        req.RequestUri != null && // null 체크 추가
                        req.RequestUri.ToString() == "https://fakechatapi.com/api/v1/chat/reset"),
                    ItExpr.IsAny<CancellationToken>()
                );
        }
    }
}