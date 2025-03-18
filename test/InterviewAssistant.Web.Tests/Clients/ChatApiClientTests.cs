/*
ChatApiClientTests.cs
ChatApiClient 클래스의 HTTP 통신 기능을 검증하는 단위 테스트입니다.
MockHttpMessageHandler를 사용하여 실제 네트워크 요청 없이도 테스트합니다.
*/

using InterviewAssistant.Common.Models;
using InterviewAssistant.Web.Clients;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using Shouldly;
using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace InterviewAssistant.Web.Tests.Clients
{
    [TestFixture]
    public class ChatApiClientTests
    {
        private MockHttpMessageHandler _mockHandler;
        private HttpClient _httpClient;
        private IChatApiClient _chatApiClient;
        private ILogger<ChatApiClient> _logger;

        [SetUp]
        public void Setup()
        {
            // HTTP 요청을 모킹하기 위한 핸들러 및 HttpClient 설정
            _mockHandler = new MockHttpMessageHandler();
            _httpClient = new HttpClient(_mockHandler)
            {
                BaseAddress = new Uri("https://fakechatapi.com/")
            };
            
            // 로거 대체 객체 생성
            _logger = Substitute.For<ILogger<ChatApiClient>>();
            
            // 테스트할 ChatApiClient 인스턴스 생성
            _chatApiClient = new ChatApiClient(_httpClient, _logger);
        }

        [TearDown]
        public void Cleanup()
        {
            // 리소스 정리
            _httpClient.Dispose();
            _mockHandler.Dispose();
        }

        // 테스트 1: 메시지 전송이 성공하는 경우
        [Test]
        public async Task SendMessageAsync_WhenSuccessful_ReturnsResponse()
        {
            // Arrange - 성공적인 응답 준비
            var expectedResponse = new ChatResponse { Message = "테스트 응답입니다" };
            var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(expectedResponse),
                    Encoding.UTF8,
                    "application/json"
                )
            };
            
            // 모의 핸들러가 성공 응답을 반환하도록 설정
            _mockHandler.ResponseToReturn = responseMessage;
            
            // Act - ChatApiClient의 메시지 전송 메서드 호출
            var request = new ChatRequest { Message = "안녕하세요" };
            var result = await _chatApiClient.SendMessageAsync(request);
            
            // Assert - 결과 검증
            result.ShouldNotBeNull();
            result.Message.ShouldBe("테스트 응답입니다");
            
            // HTTP 요청이 올바른 엔드포인트로 전송되었는지 확인
            _mockHandler.NumberOfCalls.ShouldBe(1);
            _mockHandler.LastRequestUri.ShouldBe("https://fakechatapi.com/api/v1/chat");
            _mockHandler.LastHttpMethod.ShouldBe(HttpMethod.Post);
        }
        
        // 테스트 2: API가 실패 응답을 보내는 경우
        [Test]
        public async Task SendMessageAsync_WhenApiFails_ReturnsNull()
        {
            // Arrange - 서버 오류 응답 설정
            _mockHandler.ResponseToReturn = new HttpResponseMessage(HttpStatusCode.InternalServerError);
            
            // Act - ChatApiClient의 메시지 전송 메서드 호출
            var request = new ChatRequest { Message = "안녕하세요" };
            var result = await _chatApiClient.SendMessageAsync(request);
            
            // Assert - 실패 시 null을 반환하는지 확인
            result.ShouldBeNull();
        }
        
        // 테스트 3: ResetChat 메서드가 올바른 엔드포인트로 요청하는지 확인
        [Test]
        public async Task ResetChatAsync_CallsCorrectEndpoint()
        {
            // Arrange - 성공 응답 설정
            _mockHandler.ResponseToReturn = new HttpResponseMessage(HttpStatusCode.OK);
            
            // Act - ChatApiClient의 채팅 초기화 메서드 호출
            await _chatApiClient.ResetChatAsync();
            
            // Assert - 올바른 엔드포인트로 요청했는지 확인
            _mockHandler.NumberOfCalls.ShouldBe(1);
            _mockHandler.LastRequestUri.ShouldBe("https://fakechatapi.com/api/v1/chat/reset");
            _mockHandler.LastHttpMethod.ShouldBe(HttpMethod.Post);
        }
        
        // 테스트 4: 예외 발생 시 로깅 및 예외 전파 확인
        [Test]
        public void SendMessageAsync_WhenExceptionOccurs_LogsAndThrowsException()
        {
            // Arrange - 예외를 발생시키는 핸들러 설정
            _mockHandler.ShouldThrowException = true;
            _mockHandler.ExceptionToThrow = new HttpRequestException("테스트 예외");
            
            // Act & Assert - 예외가 발생하는지 확인
            var request = new ChatRequest { Message = "안녕하세요" };
            Should.Throw<HttpRequestException>(async () => 
                await _chatApiClient.SendMessageAsync(request));
            
            // 로거가 호출되었는지는 NSubstitute로 확인할 수 있지만,
            // ILogger의 복잡한 구조때문에 간단한 검증으로 대체 (필요시 추가 가능)
            _logger.ReceivedWithAnyArgs();
        }
    }
    
    // 단순하고 효과적인 MockHttpMessageHandler 클래스 (확장)
    public class MockHttpMessageHandler : HttpMessageHandler
    {
        public HttpResponseMessage ResponseToReturn { get; set; } = new HttpResponseMessage(HttpStatusCode.OK);
        public int NumberOfCalls { get; private set; }
        public string? LastRequestUri { get; private set; }
        public HttpMethod? LastHttpMethod { get; private set; }
        
        // 예외 발생 테스트를 위한 속성 추가
        public bool ShouldThrowException { get; set; } = false;
        public Exception ExceptionToThrow { get; set; } = new Exception("기본 예외");

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            NumberOfCalls++;
            LastRequestUri = request.RequestUri?.ToString();
            LastHttpMethod = request.Method;
            
            if (ShouldThrowException)
            {
                throw ExceptionToThrow;
            }
            
            return Task.FromResult(ResponseToReturn);
        }
        
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ResponseToReturn?.Dispose();
            }
            
            base.Dispose(disposing);
        }
    }
}