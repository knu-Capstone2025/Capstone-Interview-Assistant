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

    /// <summary>
    /// Invokes the chat interview data endpoint.
    /// </summary>
    /// <param name="req"><see cref="InterviewDataRequest"/> instance as a request payload.</param>
    /// <returns>Returns an asynchronous stream of <see cref="ChatResponse"/>.</returns>
    public static async IAsyncEnumerable<ChatResponse> PostInterviewDataAsync(
        [FromBody] InterviewDataRequest req,
        IUrlContentDownloader downloader,
        IInterviewRepository repository,
        IKernelService kernelService)
    {

        string resumeContent = await downloader.DownloadTextAsync(req.ResumeUrl);
        string jobDescriptionContent = await downloader.DownloadTextAsync(req.JobDescriptionUrl);

        ResumeEntry resumeEntry = new()
        {
            Id = req.ResumeId,
            Content = resumeContent
        };
        await repository.SaveResumeAsync(resumeEntry);

        JobDescriptionEntry jobDescriptionEntry = new()
        {
            Id = req.JobDescriptionId,
            Content = jobDescriptionContent,
            ResumeEntryId = req.ResumeId
        };
        await repository.SaveJobAsync(jobDescriptionEntry);

        await foreach (var text in kernelService.InvokeInterviewAgentAsync(resumeContent, jobDescriptionContent))
        {
            yield return new ChatResponse { Message = text };
        }
    }
}
