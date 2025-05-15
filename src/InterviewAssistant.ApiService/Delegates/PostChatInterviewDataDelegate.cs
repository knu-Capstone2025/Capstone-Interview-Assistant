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
        IUrlContentDownloader downloader,
        IInterviewRepository repository,
        IKernelService kernelService)
    {
        if (string.IsNullOrEmpty(req.ResumeUrl) || string.IsNullOrEmpty(req.JobDescriptionUrl))
        {
            yield return new ChatResponse { Message = "이력서 URL과 채용공고 URL이 모두 필요합니다." };
            yield break;
        }

        IAsyncEnumerable<string>? interviewResults = null;
        bool hasError = false;
        string errorMessage = string.Empty;

        try
        {
            string resumeContent = await downloader.DownloadTextAsync(req.ResumeUrl);
            string jobDescriptionContent = await downloader.DownloadTextAsync(req.JobDescriptionUrl);

            var resumeEntry = new ResumeEntry
            {
                Id = ResumeId,
                Content = resumeContent
            };
            await repository.SaveOrUpdateResumeAsync(resumeEntry);

            var jobDescriptionEntry = new JobDescriptionEntry
            {
                Id = JobDescriptionId,
                Content = jobDescriptionContent,
                ResumeEntryId = ResumeId
            };
            await repository.SaveOrUpdateJobAsync(jobDescriptionEntry);

            interviewResults = kernelService.InvokeInterviewAgentAsync(resumeContent, jobDescriptionContent);
        }
        catch (Exception ex)
        {
            hasError = true;
            errorMessage = $"처리 중 오류가 발생했습니다: {ex.Message}";
        }

        if (hasError)
        {
            yield return new ChatResponse { Message = errorMessage };
            yield break;
        }

        await foreach (var text in interviewResults!)
        {
            yield return new ChatResponse { Message = text };
        }
    }
}
