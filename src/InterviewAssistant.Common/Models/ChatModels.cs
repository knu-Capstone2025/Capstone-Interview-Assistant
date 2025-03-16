using System.Text.Json.Serialization;

namespace InterviewAssistant.Common.Models
{
    /// <summary>
    /// 채팅 요청 모델
    /// </summary>
    public class ChatRequest
    {
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

    /// <summary>
    /// 전역 응답 모델
    /// </summary>
    public class GlobalResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;
    }
}