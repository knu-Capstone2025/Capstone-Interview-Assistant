using System.Reflection;

using InterviewAssistant.ApiService.Repositories;
using InterviewAssistant.ApiService.Models;

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;

using ModelContextProtocol;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Protocol.Transport;

namespace InterviewAssistant.ApiService.Services;


public interface IKernelService
{
    Task EnsureInitializedAsync();
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
    
    private bool _initialized = false;
    public async Task EnsureInitializedAsync()
    {
        if (_initialized) return;
        await InitializeMcpToolsAsync();
        _initialized = true;
    }
    private async Task InitializeMcpToolsAsync()
    {
        try
        {
            var tools = await mcpClient.ListToolsAsync();
            kernel.Plugins.AddFromFunctions("Markitdown", tools.Select(t => t.AsKernelFunction()));
        }
        catch (Exception ex)
        {
                Console.WriteLine($"[MCP 초기화 실패] {ex.Message}");
            }
        }
    public async IAsyncEnumerable<string> PreprocessAndInvokeAsync(string resumeUrl, string jobDescriptionUrl)
    {
        await EnsureInitializedAsync();

        var plugin = kernel.Plugins["Markitdown"];
        var convertFn = plugin["convert_to_markdown"];

        var resumeArgs = new KernelArguments { ["uri"] = resumeUrl };
        var resumeMarkdown = (await kernel.InvokeAsync(convertFn, resumeArgs)).ToString();

        var jobArgs = new KernelArguments { ["uri"] = jobDescriptionUrl };
        var jobMarkdown = (await kernel.InvokeAsync(convertFn, jobArgs)).ToString();

        // 저장
        var resumeEntry = new ResumeEntry { Id = ResumeId, Content = resumeMarkdown };
        var jobEntry = new JobDescriptionEntry
        {
            Id = JobDescriptionId,
            Content = jobMarkdown,
            ResumeEntryId = ResumeId
        };

        await repository.SaveOrUpdateResumeAsync(resumeEntry);
        await repository.SaveOrUpdateJobAsync(jobEntry);

        await foreach (var response in InvokeInterviewAgentAsync(resumeMarkdown, jobMarkdown))
        {
            yield return response;
        }
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
