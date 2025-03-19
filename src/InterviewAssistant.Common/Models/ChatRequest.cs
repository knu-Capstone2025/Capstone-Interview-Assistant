using System.Text.Json.Serialization;

namespace InterviewAssistant.Common.Models
{
    public class ChatResponse
    {
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;
    }
}
