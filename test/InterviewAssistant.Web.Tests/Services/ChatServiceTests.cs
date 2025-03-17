/*
ChatServiceTests.cs
ChatService 클래스의 기능을 검증하는 단위 테스트입니다.
HTTP 통신 부분을 모의 객체로 대체하여 네트워크 없이도 테스트합니다.
*/

using InterviewAssistant.Common.Models;
using InterviewAssistant.Web.Services;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace InterviewAssistant.Web.Tests.Services
{
    [TestFixture]
    public class ChatServiceTests
    {
        private MockHttpMessageHandler _mockHandler;
        private HttpClient _httpClient;
        private IChatService _chatService;

        [SetUp]
        public void Setup()
        {
            _mockHandler = new MockHttpMessageHandler();
            _httpClient = new HttpClient(_mockHandler)
            {
                BaseAddress = new Uri("https://fakechatapi.com/")
            };
            _chatService = new ChatService(_httpClient);
        }

        [TearDown]
        public void Cleanup()
        {
            _httpClient.Dispose();
            _mockHandler.Dispose(); // NUnit1032 경고 해결
        }

        // 테스트 1: 메시지 전송이 성공하는 경우
        [Test]
        public async Task SendMessageAsync_WhenSuccessful_ReturnsResponse()
        {
            // Arrange - 성공적인 응답 설정
            var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(new ChatResponse { Message = "테스트 응답입니다" }),
                    Encoding.UTF8,
                    "application/json"
                )
            };
            
            // 응답 설정
            _mockHandler.ResponseToReturn = responseMessage;
            
            // Act
            var result = await _chatService.SendMessageAsync("안녕하세요");
            
            // Assert
            result.ShouldNotBeNull();
            result.Message.ShouldBe("테스트 응답입니다");
            
            // 요청 호출 확인
            _mockHandler.NumberOfCalls.ShouldBe(1);
            _mockHandler.LastRequestUri.ShouldBe("https://fakechatapi.com/api/v1/chat");
        }
        
        // 테스트 2: API가 실패 응답을 보내는 경우
        [Test]
        public async Task SendMessageAsync_WhenApiFails_ReturnsNull()
        {
            // Arrange - 실패 응답 설정
            _mockHandler.ResponseToReturn = new HttpResponseMessage(HttpStatusCode.InternalServerError);
            
            // Act
            var result = await _chatService.SendMessageAsync("안녕하세요");
            
            // Assert
            result.ShouldBeNull(); // 실패 시 null을 반환하는지 확인
        }
        
        // 테스트 3: ResetChat 메서드가 올바르게 동작하는지 확인
        [Test]
        public async Task ResetChatAsync_CallsCorrectEndpoint()
        {
            // Arrange - 성공 응답 설정
            _mockHandler.ResponseToReturn = new HttpResponseMessage(HttpStatusCode.OK);
            
            // Act
            await _chatService.ResetChatAsync();
            
            // Assert
            _mockHandler.NumberOfCalls.ShouldBe(1);
            _mockHandler.LastRequestUri.ShouldBe("https://fakechatapi.com/api/v1/chat/reset");
        }
    }
    
    // 단순하고 효과적인 MockHttpMessageHandler 클래스
    public class MockHttpMessageHandler : HttpMessageHandler
    {
        public HttpResponseMessage ResponseToReturn { get; set; } = new HttpResponseMessage(HttpStatusCode.OK);
        public int NumberOfCalls { get; private set; }
        
        // CS8618 오류 해결: nullable로 선언
        public string? LastRequestUri { get; private set; }
        public HttpMethod? LastHttpMethod { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            NumberOfCalls++;
            
            // CS8602 오류 해결: null 체크 추가
            LastRequestUri = request.RequestUri?.ToString();
            LastHttpMethod = request.Method;
            
            return Task.FromResult(ResponseToReturn);
        }
        
        // HttpMessageHandler의 Dispose 메서드를 명시적으로 재정의
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // 관리되는 리소스 정리 (필요한 경우)
                ResponseToReturn?.Dispose();
            }
            
            base.Dispose(disposing);
        }
    }
}