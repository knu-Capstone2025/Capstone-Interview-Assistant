using InterviewAssistant.Common.Models;
using InterviewAssistant.ApiService.Services;

using Microsoft.AspNetCore.Mvc;

namespace InterviewAssistant.ApiService.Delegates;

/// <summary>
/// This represents the partial delegate entity that takes care of the chat completion endpoint.
/// </summary>
public static partial class ChatCompletionDelegate
{
    // 고정 ID 정의
    /// <summary>
    /// Invokes the chat interview data endpoint.
    /// </summary>
    /// <param name="req"><see cref="InterviewDataRequest"/> instance as a request payload.</param>
    /// <returns>Returns an asynchronous stream of <see cref="ChatResponse"/>.</returns>
    public static async IAsyncEnumerable<ChatResponse> PostInterviewDataAsync(
        [FromBody] InterviewDataRequest req,
        IKernelService kernelService)
    {
        await foreach (var text in kernelService.PreprocessAndInvokeAsync(req.ResumeUrl, req.JobDescriptionUrl))
        {
            yield return new ChatResponse { Message = text };
        }
    }
}
