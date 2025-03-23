using InterviewAssistant.ApiService.Delegates;
using InterviewAssistant.Common.Models;

namespace InterviewAssistant.ApiService.Endpoints;

public static class ChatCompletionEndpoint
{
    public static IEndpointRouteBuilder MapChatCompletionEndpoint(this IEndpointRouteBuilder routeBuilder)
    {
        // Chat Completion API 그룹
        var api = routeBuilder.MapGroup("api/chat").WithTags("Chat");

        // 채팅 메시지 전송 엔드포인트
        api.MapPost("complete", ChatCompletionDelegate.PostChatCompletionAsync)
           .Accepts<ChatRequest>(contentType: "application/json")
           .Produces<IEnumerable<ChatResponse>>(statusCode: StatusCodes.Status200OK, contentType: "application/json")
           .WithName("PostChatCompletion")
           .WithOpenApi();

        // 채용공고문 전송 엔드포인트
        api.MapPost("interview-data", ChatCompletionDelegate.PostInterviewDataAsync)
           .Accepts<InterviewDataRequest>(contentType: "application/json")
           .Produces<IEnumerable<ChatResponse>>(statusCode: StatusCodes.Status200OK, contentType: "application/json")
           .WithName("PostInterviewData")
           .WithOpenApi();

        return routeBuilder;
    }
}
