using System.Text.Json.Serialization;

namespace InterviewAssistant.ApiService.Models;


public class ChatRequest
{
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}