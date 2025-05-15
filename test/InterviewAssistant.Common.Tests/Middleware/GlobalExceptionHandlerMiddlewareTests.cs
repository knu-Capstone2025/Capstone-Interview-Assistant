using System.Net;
using System.Text.Json;

using InterviewAssistant.Common.Middleware;
using InterviewAssistant.Common.Models;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

using NSubstitute;

using Shouldly;

namespace InterviewAssistant.Common.Tests.Middleware
{
    [TestFixture]
    public class GlobalExceptionHandlerMiddlewareTests
    {
        private GlobalExceptionHandlerMiddleware _middleware;
        private ILogger<GlobalExceptionHandlerMiddleware> _logger;
        private RequestDelegate _next;
        private HttpContext _httpContext;
        private MemoryStream _responseBodyStream;

        [SetUp]
        public void Setup()
        {
            // 로거 목 설정
            _logger = Substitute.For<ILogger<GlobalExceptionHandlerMiddleware>>();
            
            // 요청 처리 대리자 목 설정 (기본적으로 예외를 발생시킴)
            _next = Substitute.For<RequestDelegate>();
            
            // HTTP 컨텍스트 설정
            _httpContext = new DefaultHttpContext();
            _responseBodyStream = new MemoryStream();
            _httpContext.Response.Body = _responseBodyStream;
            
            // 테스트할 미들웨어 인스턴스 생성
            _middleware = new GlobalExceptionHandlerMiddleware(_next, _logger);
        }

        [TearDown]
        public void TearDown()
        {
            _responseBodyStream.Dispose();
        }

        [Test]
        public async Task InvokeAsync_NoException_ShouldCallNextMiddleware()
        {
            // Arrange
            _next.Invoke(Arg.Any<HttpContext>()).Returns(Task.CompletedTask);

            // Act
            await _middleware.InvokeAsync(_httpContext);

            // Assert
            await _next.Received(1).Invoke(_httpContext);
            _httpContext.Response.StatusCode.ShouldBe((int)HttpStatusCode.OK);
        }

        [Test]
        public async Task InvokeAsync_ArgumentException_ShouldReturnBadRequest()
        {
            // Arrange
            var expectedMessage = "잘못된 인자입니다";
            _next.Invoke(Arg.Any<HttpContext>()).Returns(_ => throw new ArgumentException(expectedMessage));

            // Act
            await _middleware.InvokeAsync(_httpContext);

            // Assert
            _httpContext.Response.StatusCode.ShouldBe((int)HttpStatusCode.BadRequest);
            _responseBodyStream.Position = 0;
            
            using var reader = new StreamReader(_responseBodyStream);
            var responseBody = await reader.ReadToEndAsync();
            var response = JsonSerializer.Deserialize<ChatResponse>(responseBody);
            
            response.ShouldNotBeNull();
            response!.Message.ShouldBe(expectedMessage);
            _httpContext.Response.ContentType.ShouldBe("application/json");
        }

        [Test]
        public async Task InvokeAsync_FileNotFoundException_ShouldReturnNotFound()
        {
            // Arrange
            var expectedMessage = "파일을 찾을 수 없습니다";
            _next.Invoke(Arg.Any<HttpContext>()).Returns(_ => throw new FileNotFoundException(expectedMessage));

            // Act
            await _middleware.InvokeAsync(_httpContext);

            // Assert
            _httpContext.Response.StatusCode.ShouldBe((int)HttpStatusCode.NotFound);
            _responseBodyStream.Position = 0;
            
            using var reader = new StreamReader(_responseBodyStream);
            var responseBody = await reader.ReadToEndAsync();
            var response = JsonSerializer.Deserialize<ChatResponse>(responseBody);
            
            response.ShouldNotBeNull();
            response!.Message.ShouldBe(expectedMessage);
        }

        [Test]
        public async Task InvokeAsync_UnauthorizedAccessException_ShouldReturnUnauthorized()
        {
            // Arrange
            var expectedMessage = "접근 권한이 없습니다";
            _next.Invoke(Arg.Any<HttpContext>()).Returns(_ => throw new UnauthorizedAccessException(expectedMessage));

            // Act
            await _middleware.InvokeAsync(_httpContext);

            // Assert
            _httpContext.Response.StatusCode.ShouldBe((int)HttpStatusCode.Unauthorized);
            _responseBodyStream.Position = 0;
            
            using var reader = new StreamReader(_responseBodyStream);
            var responseBody = await reader.ReadToEndAsync();
            var response = JsonSerializer.Deserialize<ChatResponse>(responseBody);
            
            response.ShouldNotBeNull();
            response!.Message.ShouldBe(expectedMessage);
        }

        [Test]
        public async Task InvokeAsync_HttpRequestException_ShouldReturnAppropriateStatusCode()
        {
            // Arrange
            var expectedMessage = "외부 API 호출 중 오류가 발생했습니다";
            _next.Invoke(Arg.Any<HttpContext>()).Returns(_ => 
                throw new HttpRequestException(expectedMessage, null, HttpStatusCode.BadGateway));

            // Act
            await _middleware.InvokeAsync(_httpContext);

            // Assert
            _httpContext.Response.StatusCode.ShouldBe((int)HttpStatusCode.InternalServerError);
            _responseBodyStream.Position = 0;
            
            using var reader = new StreamReader(_responseBodyStream);
            var responseBody = await reader.ReadToEndAsync();
            var response = JsonSerializer.Deserialize<ChatResponse>(responseBody);
            
            response.ShouldNotBeNull();
            response!.Message.ShouldBe(expectedMessage);
        }

        [Test]
        public async Task InvokeAsync_GenericException_ShouldReturnInternalServerError()
        {
            // Arrange
            var expectedMessage = "일반적인 오류가 발생했습니다";
            _next.Invoke(Arg.Any<HttpContext>()).Returns(_ => throw new Exception(expectedMessage));

            // Act
            await _middleware.InvokeAsync(_httpContext);

            // Assert
            _httpContext.Response.StatusCode.ShouldBe((int)HttpStatusCode.InternalServerError);
            _responseBodyStream.Position = 0;
            
            using var reader = new StreamReader(_responseBodyStream);
            var responseBody = await reader.ReadToEndAsync();
            var response = JsonSerializer.Deserialize<ChatResponse>(responseBody);
            
            response.ShouldNotBeNull();
            response!.Message.ShouldBe(expectedMessage);
        }

    }
}
