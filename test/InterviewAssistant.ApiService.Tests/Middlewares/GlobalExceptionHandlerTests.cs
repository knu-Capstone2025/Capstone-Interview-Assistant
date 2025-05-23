using System.Net;
using System.Text.Json;

using InterviewAssistant.ApiService.Middleware;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

using NSubstitute;

using Shouldly;

namespace InterviewAssistant.ApiService.Tests.Middleware;

[TestFixture]
public class GlobalExceptionHandlerTests
{
    private ILogger<GlobalExceptionHandler> _logger;
    private GlobalExceptionHandler _handler;
    private DefaultHttpContext _httpContext;

    [SetUp]
    public void Setup()
    {
        _logger = Substitute.For<ILogger<GlobalExceptionHandler>>();
        _handler = new GlobalExceptionHandler(_logger);
        _httpContext = new DefaultHttpContext();
        _httpContext.Response.Body = new MemoryStream();
    }

    [Test]
    public async Task TryHandleAsync_WithException_ShouldReturnTrueAndSetCorrectResponse()
    {
        // Arrange
        var exception = new InvalidOperationException("테스트 예외");

        // Act
        var result = await _handler.TryHandleAsync(_httpContext, exception, CancellationToken.None);

        // Assert
        result.ShouldBeTrue();
        _httpContext.Response.StatusCode.ShouldBe(500);
        _httpContext.Response.ContentType.ShouldBe("application/json");

        // 응답 내용 확인
        _httpContext.Response.Body.Position = 0;
        var responseContent = await new StreamReader(_httpContext.Response.Body).ReadToEndAsync();
        var responseObject = JsonSerializer.Deserialize<JsonElement>(responseContent);

        responseObject.GetProperty("error").GetString().ShouldBe("서버 오류가 발생했습니다.");
        responseObject.GetProperty("message").GetString().ShouldBe("테스트 예외");
        responseObject.TryGetProperty("timestamp", out _).ShouldBeTrue();
    }

    [Test]
    [TestCase(typeof(NullReferenceException), "Null reference")]
    [TestCase(typeof(FileNotFoundException), "File not found")]
    [TestCase(typeof(UnauthorizedAccessException), "Access denied")]
    public async Task TryHandleAsync_WithDifferentExceptions_ShouldHandleCorrectly(Type exceptionType, string message)
    {
        // Arrange
        var exception = (Exception)Activator.CreateInstance(exceptionType, message)!;

        // Act
        var result = await _handler.TryHandleAsync(_httpContext, exception, CancellationToken.None);

        // Assert
        result.ShouldBeTrue();
        _httpContext.Response.StatusCode.ShouldBe(500);

        _httpContext.Response.Body.Position = 0;
        var responseContent = await new StreamReader(_httpContext.Response.Body).ReadToEndAsync();
        var responseObject = JsonSerializer.Deserialize<JsonElement>(responseContent);

        responseObject.GetProperty("message").GetString().ShouldBe(message);
    }

    [Test]
    public async Task TryHandleAsync_ShouldGenerateValidJsonResponse()
    {
        // Arrange
        var exception = new InvalidOperationException("JSON 검증");

        // Act
        await _handler.TryHandleAsync(_httpContext, exception, CancellationToken.None);

        // Assert
        _httpContext.Response.Body.Position = 0;
        var responseContent = await new StreamReader(_httpContext.Response.Body).ReadToEndAsync();

        // JSON 유효성 검증
        Should.NotThrow(() => JsonSerializer.Deserialize<JsonElement>(responseContent));

        var responseObject = JsonSerializer.Deserialize<JsonElement>(responseContent);
        responseObject.TryGetProperty("error", out _).ShouldBeTrue();
        responseObject.TryGetProperty("message", out _).ShouldBeTrue();
        responseObject.TryGetProperty("timestamp", out _).ShouldBeTrue();
    }
}
