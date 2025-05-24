using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;

namespace InterviewAssistant.ApiService.Middlewares;

/// <summary>
/// 전역 예외 처리기
/// </summary>
public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, 
        Exception exception, 
        CancellationToken cancellationToken)
    {
        logger.LogError(exception, "예외 발생: {Message}", exception.Message);

        var response = new
        {
            error = "서버 오류가 발생했습니다.",
            message = exception.Message,
            timestamp = DateTime.UtcNow
        };

        httpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
        httpContext.Response.ContentType = "application/json";

        await httpContext.Response.WriteAsync(JsonSerializer.Serialize(response), cancellationToken);
        
        return true;
    }
}
