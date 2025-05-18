using System.Reflection;

using InterviewAssistant.ApiService.Repositories;
using InterviewAssistant.ApiService.Models;

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.Extensions.AI;

using ModelContextProtocol.Client;

namespace InterviewAssistant.ApiService.Services;

public interface IKernelService
{
    IAsyncEnumerable<string> InvokeInterviewAgentAsync(
        string resumeContent,
        string jobDescriptionContent,
        IEnumerable<ChatMessageContent>? messages = null);
    IAsyncEnumerable<string> PreprocessAndInvokeAsync(string resumeUrl, string jobDescriptionUrl);
}

public class KernelService(Kernel kernel, IMcpClient mcpClient, IInterviewRepository repository) : IKernelService
{
    private static readonly Guid ResumeId = new Guid("11111111-1111-1111-1111-111111111111");
    private static readonly Guid JobDescriptionId = new Guid("22222222-2222-2222-2222-222222222222");

    private static readonly string AgentYamlPath = "Agents/InterviewAgents/InterviewAgent.yaml";
    
    public async IAsyncEnumerable<string> PreprocessAndInvokeAsync(string resumeUrl, string jobDescriptionUrl)
    {
        var resumeContent = await ConvertUriToMarkdownAsync(resumeUrl);
        var jobContent = await ConvertUriToMarkdownAsync(jobDescriptionUrl);

        var resumeEntry = new ResumeEntry { Id = ResumeId, Content = resumeContent };
        var jobEntry = new JobDescriptionEntry
        {
            Id = JobDescriptionId,
            Content = jobContent,
            ResumeEntryId = ResumeId
        };

        await repository.SaveOrUpdateResumeAsync(resumeEntry);
        await repository.SaveOrUpdateJobAsync(jobEntry);

        await foreach (var response in InvokeInterviewAgentAsync(resumeContent, jobContent))
        {
            yield return response;
        }
    }
    private async Task<string> ConvertUriToMarkdownAsync(string uri)
    {
        var tools = await mcpClient.ListToolsAsync();
        var convertTool = tools.FirstOrDefault(t => t.Name == "convert_to_markdown")
            ?? throw new InvalidOperationException("MCP 서버에 convert_to_markdown 도구가 없습니다");

        var args = new AIFunctionArguments { { "uri", uri } };
        var result = await convertTool.InvokeAsync(args);

        return result?.ToString() ?? string.Empty;
    }

    private ChatCompletionAgent GetInterviewAgent()
    {
        var filepath = Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!,
            AgentYamlPath);

        if (!File.Exists(filepath))
        {
            throw new FileNotFoundException($"에이전트 YAML 파일을 찾을 수 없습니다: {filepath}");
        }

        var definition = File.ReadAllText(filepath);
        var template = KernelFunctionYaml.ToPromptTemplateConfig(definition);

        var agent = new ChatCompletionAgent(template, new KernelPromptTemplateFactory())
        {
            Kernel = kernel
        };

        return agent;
    }

    private PromptExecutionSettings GetExecutionSettings()
    {
        return new PromptExecutionSettings()
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
        };
    }

    public async IAsyncEnumerable<string> InvokeInterviewAgentAsync(
        string resumeContent,
        string jobDescriptionContent,
        IEnumerable<ChatMessageContent>? messages = null)
    {
        var agent = GetInterviewAgent();
        var messagesList = messages?.ToList() ?? [];

        var arguments = new KernelArguments(GetExecutionSettings())
        {
            { "resume", resumeContent },
            { "jobDescription", jobDescriptionContent }
        };

        ChatMessageContent userMessage;
        if (messagesList.Count > 0 && messagesList[^1].Role == AuthorRole.User)
        {
            userMessage = messagesList[^1];
            arguments["userMessage"] = userMessage.Content;
        }
        else
        {
            userMessage = new ChatMessageContent(AuthorRole.User, "면접을 시작합니다");
        }

        var chatHistory = new ChatHistory();
        if (messagesList.Count > 0)
        {
            foreach (var msg in messagesList.Take(messagesList.Count - 1))
            {
                chatHistory.Add(msg);
            }
        }

        var agentThread = new ChatHistoryAgentThread(chatHistory);

        var options = new AgentInvokeOptions
        {
            KernelArguments = arguments
        };

        await foreach (var response in agent.InvokeStreamingAsync(userMessage, agentThread, options))
        {
            if (response != null)
            {
                yield return response.Message.ToString()!;
            }
        }
    }
}
