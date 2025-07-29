using System.Reflection;
using System.Text.RegularExpressions;

using InterviewAssistant.ApiService.Repositories;
using InterviewAssistant.ApiService.Models;
using InterviewAssistant.Common.Models;

using System.Text;
using System.Text.Json; 

using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;

using ModelContextProtocol.Client;

namespace InterviewAssistant.ApiService.Services;

public interface IKernelService
{
    IAsyncEnumerable<string> InvokeInterviewAgentAsync(string resumeContent, string jobDescriptionContent, IEnumerable<ChatMessageContent>? messages = null);
    IAsyncEnumerable<string> PreprocessAndInvokeAsync(Guid resumeId, Guid jobId, string resumeUrl, string jobDescriptionUrl);

    Task<InterviewReportModel> GenerateReportAsync(IEnumerable<Common.Models.ChatMessage> messages);
}

public class KernelService(Kernel kernel, IMcpClient mcpClient, IInterviewRepository repository) : IKernelService
{
    private static readonly string AgentYamlPath = "Agents/InterviewAgents/InterviewAgent.yaml";

    private static readonly Regex GoogleDriveIdPattern = new Regex(@"https?://drive\.google\.com/.*(?:file/d/|id=)([^/&?#]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public async IAsyncEnumerable<string> InvokeInterviewAgentAsync(string resumeContent, string jobDescriptionContent, IEnumerable<ChatMessageContent>? messages = null)
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

    public async IAsyncEnumerable<string> PreprocessAndInvokeAsync(Guid resumeId, Guid jobId, string resumeUrl, string jobDescriptionUrl)
    {
        var resumeContent = await ConvertUriToMarkdownAsync(resumeUrl);
        var jobContent = await ConvertUriToMarkdownAsync(jobDescriptionUrl);

        var resumeEntry = new ResumeEntry
        {
            Id = resumeId,
            Content = resumeContent
        };
        var jobEntry = new JobDescriptionEntry
        {
            Id = jobId,
            Content = jobContent,
            ResumeEntryId = resumeEntry.Id
        };

        await repository.SaveResumeAsync(resumeEntry);
        await repository.SaveJobAsync(jobEntry);

        await foreach (var response in InvokeInterviewAgentAsync(resumeContent, jobContent))
        {
            yield return response;
        }
    }

    private async Task<string> ConvertUriToMarkdownAsync(string uri)
    {
        var normalizedUri = NormalizeUri(uri);

        var tools = await mcpClient.ListToolsAsync();
        var convertTool = tools.SingleOrDefault(t => t.Name == "convert_to_markdown")
            ?? throw new InvalidOperationException("MCP 서버에 convert_to_markdown 도구가 없습니다");

        var args = new AIFunctionArguments { { "uri", normalizedUri } };
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

    private static string NormalizeUri(string uri)
    {
        var match = GoogleDriveIdPattern.Match(uri);
        if (match.Success && match.Groups.Count > 1)
        {
            var fileId = match.Groups[1].Value;
            return $"https://drive.google.com/uc?export=download&id={fileId}";
        }

        return uri;
    }

    public async Task<InterviewReportModel> GenerateReportAsync(IEnumerable<Common.Models.ChatMessage> messages)
    {
        // 1. 대화 기록을 AI가 이해하기 쉬운 문자열로 변환
        var historyText = new StringBuilder();
        foreach (var message in messages)
        {
            historyText.AppendLine($"{message.Role}: {message.Message}");
        }

        // 2. AI에게 리포트 생성을 지시하는 프롬프트 정의
        var prompt = """
        You are an expert interview analyst. Your task is to analyze the following interview conversation and provide a structured report.
        The conversation is between a 'User' (the candidate) and an 'Assistant' (the interviewer).

        Based on the entire conversation, please provide:
        1.  A concise overall feedback.
        2.  A list of 3 key strengths.
        3.  A list of 3 key weaknesses or areas for improvement.
        4.  Categorize all questions asked by the 'Assistant' into one of three types: '기술(Technical)', '경험(Experience)', or '인성(Personality)' and provide a count for each.

        IMPORTANT: Your entire output must be a single, valid JSON object. Do not include any text outside of this JSON object.
        The JSON structure must be:
        {
          "overallFeedback": "...",
          "strengths": ["...", "...", "..."],
          "weaknesses": ["...", "...", "..."],
          "chartData": {
            "labels": ["기술", "경험", "인성"],
            "values": [count_of_technical, count_of_experience, count_of_personality]
          }
        }

        --- INTERVIEW HISTORY ---
        {{$history}}
        """;

        // 3. AI 호출 및 결과 처리
        try
        {
            var reportFunction = kernel.CreateFunctionFromPrompt(prompt);
            var arguments = new KernelArguments { { "history", historyText.ToString() } };
            var result = await kernel.InvokeAsync<string>(reportFunction, arguments);

            if (string.IsNullOrWhiteSpace(result))
            {
                return new InterviewReportModel { OverallFeedback = "AI로부터 응답을 받지 못했습니다." };
            }

            var jsonResponse = result.Trim();
            if (jsonResponse.StartsWith("```json"))
            {
                jsonResponse = jsonResponse.Substring(7);
            }
            if (jsonResponse.StartsWith("```"))
            {
                jsonResponse = jsonResponse.Substring(3);
            }
            if (jsonResponse.EndsWith("```"))
            {
                jsonResponse = jsonResponse.Substring(0, jsonResponse.Length - 3);
            }

            // 4. AI가 생성한 JSON 문자열을 C# 객체로 변환
            var report = JsonSerializer.Deserialize<InterviewReportModel>(result, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return report ?? new InterviewReportModel { OverallFeedback = "리포트 분석에 실패했습니다." };
        }
        catch (Exception ex)
        {
            // 예외 처리 (예: 로깅)
            Console.WriteLine($"Error generating report: {ex.Message}");
            return new InterviewReportModel { OverallFeedback = "리포트 생성 중 오류가 발생했습니다." };
        }
    }
}
