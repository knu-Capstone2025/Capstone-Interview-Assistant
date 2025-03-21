namespace InterviewAssistant.Common.Models;

public class ChatRequest
{   
    public List<ChatMessage> Messages { get; set; } = [];
}

public class ChatMessage
{
    public string Role { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;
}