using InterviewAssistant.Common.Models;

using Microsoft.AspNetCore.Mvc;

namespace InterviewAssistant.ApiService.Delegates;

/// <summary>
/// This represents the partial delegate entity that takes care of the chat completion endpoint.
/// </summary>
public static partial class ChatCompletionDelegate
{
    /// <summary>
    /// Invokes the chat interview data endpoint.
    /// </summary>
    /// <param name="req"><see cref="InterviewDataRequest"/> instance as a request payload.</param>
    /// <returns>Returns an asynchronous stream of <see cref="ChatResponse"/>.</returns>
    public static async IAsyncEnumerable<ChatResponse> PostInterviewDataAsync([FromBody] InterviewDataRequest req)
    {
        await Task.Delay(1000); // Simulate async work

        var responses = new List<ChatResponse>();
        foreach (var response in responses)
        {
            yield return response;
        }
    }
}
