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
        IInterviewRepository repository,
        IKernelService kernelService,
        Kernel kernel)
    {
        await kernelService.EnsureInitializedAsync();

        var markitdownPlugin = kernel.Plugins["Markitdown"];
        var convertFunction = markitdownPlugin["convert_to_markdown"];

        var resumeArgs = new KernelArguments { ["uri"] = req.ResumeUrl };
        var resumeResult = await kernel.InvokeAsync(convertFunction, resumeArgs);
        var resumeMarkdown = resumeResult.ToString();

        Console.WriteLine("[MCP 변환 완료] 이력서:");
        Console.WriteLine(resumeMarkdown[..Math.Min(500, resumeMarkdown.Length)]);



        var jobArgs = new KernelArguments { ["uri"] = req.JobDescriptionUrl };
        var jobResult = await kernel.InvokeAsync(convertFunction, jobArgs);
        var jobMarkdown = jobResult.ToString();

        Console.WriteLine("[MCP 변환 완료] 채용공고:");
        Console.WriteLine(jobMarkdown[..Math.Min(500, jobMarkdown.Length)]);

        ResumeEntry resumeEntry = new()
        {
            Id = ResumeId,
            Content = resumeMarkdown
        };
        await repository.SaveOrUpdateResumeAsync(resumeEntry);

        JobDescriptionEntry jobDescriptionEntry = new()
        {
            Id = JobDescriptionId,
            Content = jobMarkdown,
            ResumeEntryId = ResumeId
        };
        await repository.SaveOrUpdateJobAsync(jobDescriptionEntry);

        

        await foreach (var text in kernelService.InvokeInterviewAgentAsync(resumeMarkdown, jobMarkdown))
        {
            yield return new ChatResponse { Message = text };
        }
    }
}
