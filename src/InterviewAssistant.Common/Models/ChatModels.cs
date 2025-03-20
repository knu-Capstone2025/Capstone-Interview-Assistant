using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace InterviewAssistant.Common.Models
{
    /// <summary>
    /// 채팅 요청 모델
    /// </summary>
    public class ChatRequest
    {
        public List<ChatMessage> Messages { get; set; } = [];
    }

    /// <summary>
    /// 채팅 메시지 모델
    /// </summary>
    public class ChatMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;
        
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// 채팅 응답 모델
    /// </summary>
    public class ChatResponse
    {
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;
    }
}