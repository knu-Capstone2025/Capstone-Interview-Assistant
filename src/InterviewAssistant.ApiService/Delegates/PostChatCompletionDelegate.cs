using InterviewAssistant.Common.Models;

using Microsoft.AspNetCore.Mvc;

namespace InterviewAssistant.ApiService.Delegates;

public static partial class ChatCompletionDelegate
{
    public static async IAsyncEnumerable<ChatResponse> PostChatCompletionAsync([FromBody] IEnumerable<ChatRequest> req)
    {
        await Task.Delay(1000); // Simulate async work

        var responses = new List<ChatResponse>();
        foreach (var response in responses)
        {
            yield return response;
        }
    }
}
