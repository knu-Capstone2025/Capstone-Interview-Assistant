using InterviewAssistant.Common.Models;
using InterviewAssistant.ApiService.Services;
using InterviewAssistant.ApiService.Repositories;
using InterviewAssistant.ApiService.Models;

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.AspNetCore.Mvc;

using ChatMessageContent = Microsoft.SemanticKernel.ChatMessageContent;

namespace InterviewAssistant.ApiService.Delegates;
/// <summary>
/// This represents the partial delegate entity that takes care of the chat completion endpoint.
/// </summary>
public static partial class ChatCompletionDelegate
{

    /// <summary>
    /// Invokes the chat completion endpoint.
    /// </summary>
    /// <param name="req"><see cref="ChatRequest"/> instance as a request payload.</param>
    /// <returns>Returns an asynchronous stream of <see cref="ChatResponse"/>.</returns>
    public static async IAsyncEnumerable<ChatResponse> PostChatCompletionAsync(
        [FromBody] ChatRequest req,
        IKernelService kernelService,
        IInterviewRepository repository)
    {

        ResumeEntry? resumeEntry = await repository.GetResumeByIdAsync(ResumeId);
        JobDescriptionEntry? jobDescriptionEntry = await repository.GetJobByIdAsync(JobDescriptionId);

        if (resumeEntry == null || jobDescriptionEntry == null)
        {
            yield return new ChatResponse { Message = "이력서 또는 채용공고 데이터가 없습니다." };
            yield break;
        }

        var messages = new List<ChatMessageContent>{};

        foreach (var msg in req.Messages)
        {
            ChatMessageContent message = msg.Role switch
            {
                MessageRoleType.User => new ChatMessageContent(AuthorRole.User, msg.Message),
                MessageRoleType.Assistant => new ChatMessageContent(AuthorRole.Assistant, msg.Message),
                MessageRoleType.System => new ChatMessageContent(AuthorRole.System, msg.Message),
                MessageRoleType.Tool => new ChatMessageContent(AuthorRole.Tool, msg.Message),
                _ => throw new ArgumentException($"Invalid role: {msg.Role}")
            };
            messages.Add(message);
        }
 
        await foreach (var text in kernelService.InvokeInterviewAgentAsync(
            resumeEntry.Content,
            jobDescriptionEntry.Content,
            messages))
        {
            yield return new ChatResponse { Message = text };
        }
    }
}
