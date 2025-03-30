using InterviewAssistant.ApiService.Services.Interfaces;
using OpenAI.Chat;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace InterviewAssistant.ApiService.Services.Implementations;

/// <summary>
/// OpenAI 채팅 클라이언트 구현체
/// </summary>
public class OpenAIChatClient : IChatClient
{
    private readonly ChatClient _chatClient;

    /// <summary>
    /// OpenAIChatClient 생성자
    /// </summary>
    /// <param name="chatClient">OpenAI ChatClient 인스턴스</param>
    public OpenAIChatClient(ChatClient chatClient)
    {
        _chatClient = chatClient ?? throw new ArgumentNullException(nameof(chatClient));
    }

    /// <inheritdoc/>
    public async Task<ChatCompletion> CompleteChatAsync(
        IEnumerable<ChatMessage> messages,
        ChatCompletionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var response = await _chatClient.CompleteChatAsync(messages, options, cancellationToken);
        return response.Value;
    }
}