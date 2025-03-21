namespace InterviewAssistant.Common.Models;

/// <summary>
/// This represents the response entity for the chat service. It contains a message that is sent from the assistant to the user.
/// </summary>
public class ChatResponse
{
    /// <summary>
    /// Gets or sets the message that is sent from the assistant to the user.
    /// </summary>
    public string Message { get; set; } = string.Empty;
}