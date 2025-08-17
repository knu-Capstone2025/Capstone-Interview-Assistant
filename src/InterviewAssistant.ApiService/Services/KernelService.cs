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
        당신은 전문 면접 분석가입니다. 당신의 임무는 다음 면접 대화를 분석하고 구조화된 보고서를 제공하는 것입니다.
        대화는 'User'(지원자)와 'Assistant'(면접관) 사이에서 이루어졌습니다.

        전체 대화 내용을 바탕으로 다음을 제공해 주세요:
        1.  간결한 종합 피드백.
        2.  주요 강점 3가지 목록.
        3.  주요 약점 또는 개선이 필요한 부분 3가지 목록.
        4.  'Assistant'(면접관)가 한 모든 질문을 '기술(Technical)', '경험(Experience)', '인성(Personality)' 세 가지 유형 중 하나로 분류하고 각 유형의 개수를 제공해 주세요.

        중요: 전체 출력은 유효한 단일 JSON 객체여야 합니다. 이 JSON 객체 외부에 어떤 텍스트도 포함하지 마세요.
        JSON 구조는 다음과 같아야 합니다:
        {
        "overallFeedback": "...",
        "strengths": ["...", "...", "..."],
        "weaknesses": ["...", "...", "..."],
        "chartData": {
            "labels": ["기술", "경험", "인성"],
            "values": [기술_질문_수, 경험_질문_수, 인성_질문_수]
        }
        }

        --- 면접 기록 ---
        {{$history}}
        """;

        // 3. AI 호출 및 결과 처리
        string? result = null; // AI의 원본 응답을 catch 블록에서도 사용할 수 있도록 변경
        try
        {
            var reportFunction = kernel.CreateFunctionFromPrompt(prompt);
            var arguments = new KernelArguments { { "history", historyText.ToString() } };
            result = await kernel.InvokeAsync<string>(reportFunction, arguments);

            if (string.IsNullOrWhiteSpace(result))
            {
                return new InterviewReportModel { OverallFeedback = "AI로부터 응답을 받지 못했습니다." };
            }

            var jsonResponse = result.Trim();
            if (jsonResponse.StartsWith("```json"))
            {
                jsonResponse = jsonResponse.Substring(7);
            }
            if (jsonResponse.EndsWith("```"))
            {
                jsonResponse = jsonResponse.Substring(0, jsonResponse.Length - 3);
            }

            var report = JsonSerializer.Deserialize<InterviewReportModel>(jsonResponse, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return report ?? new InterviewReportModel { OverallFeedback = "리포트 분석에 실패했습니다." };
        }
        catch (System.Text.Json.JsonException jsonEx) // 1. JSON 파싱 오류만 특정
        {
            // 2. 실패 시 AI의 원본 응답과 함께 상세 로그 기록
            Console.WriteLine($"AI response JSON parsing failed: {jsonEx.Message}. Raw response from AI: {result}");
            // 3. 사용자에게 더 구체적인 오류 메시지 반환
            return new InterviewReportModel { OverallFeedback = "AI가 분석 결과를 잘못된 형식으로 반환했습니다. 잠시 후 다시 시도해 주세요." };
        }
        catch (Exception ex) // 그 외 모든 오류
        {
            Console.WriteLine($"An unexpected error occurred while generating the report: {ex}"); // 전체 예외 정보 로깅
            return new InterviewReportModel { OverallFeedback = "리포트 생성 중 예상치 못한 오류가 발생했습니다." };
        }
    }
}
