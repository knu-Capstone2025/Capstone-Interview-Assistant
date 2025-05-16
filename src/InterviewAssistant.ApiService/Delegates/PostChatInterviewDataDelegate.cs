using InterviewAssistant.Common.Models;
using InterviewAssistant.ApiService.Services;
using InterviewAssistant.ApiService.Repositories;
using InterviewAssistant.ApiService.Models;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace InterviewAssistant.ApiService.Delegates;

/// <summary>
/// This represents the partial delegate entity that takes care of the chat completion endpoint.
/// </summary>
public static partial class ChatCompletionDelegate
{
    // 고정 ID 정의
    private static readonly Guid ResumeId = new Guid("11111111-1111-1111-1111-111111111111");
    private static readonly Guid JobDescriptionId = new Guid("22222222-2222-2222-2222-222222222222");
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
