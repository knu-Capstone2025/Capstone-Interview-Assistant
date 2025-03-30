using OpenAI.Chat;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace InterviewAssistant.ApiService.Services.Interfaces;

/// <summary>
/// 채팅 클라이언트 인터페이스
/// </summary>
public interface IChatClient
{
    /// <summary>
    /// 채팅 완성 요청을 비동기적으로 실행합니다.
    /// </summary>
    /// <param name="messages">채팅 메시지 목록</param>
    /// <param name="options">채팅 완성 옵션</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>채팅 완성 응답</returns>
    Task<ChatCompletion> CompleteChatAsync(
        IEnumerable<ChatMessage> messages,
        ChatCompletionOptions? options = null,
        CancellationToken cancellationToken = default);
}