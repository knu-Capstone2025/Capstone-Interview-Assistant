using System.Reflection;
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
}

public class KernelService(Kernel kernel, Task<IMcpClient> mcpClientTask) : IKernelService
{

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
           var mcpClient = await mcpClientTask; 
           var tools = await mcpClient.ListToolsAsync();
           kernel.Plugins.AddFromFunctions("Markitdown", tools.Select(t => t.AsKernelFunction()));
       }
       catch (Exception ex)
       {
            Console.WriteLine($"[MCP 초기화 실패] {ex.Message}");
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
