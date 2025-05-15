namespace InterviewAssistant.Common.Models;

/// <summary>
/// This represents the request entity for the chat service. It contains a list of messages that are exchanged between the user and the assistant.
/// </summary>
public class ChatRequest
{
    /// <summary>
    /// Gets or sets the unique identifier for the resume being referenced in the chat.
    /// </summary>
    public Guid ResumeId { get; set; } = Guid.Empty;

    /// <summary>
    /// Gets or sets the unique identifier for the job description being referenced in the chat.
    /// </summary>
    public Guid JobDescriptionId { get; set; } = Guid.Empty;

    /// <summary>
    /// Gets or sets the list of messages exchanged between the user and the assistant.
    /// </summary>
    public List<ChatMessage> Messages { get; set; } = [];
}

/// <summary>
/// This represents the message entity for the chat service. It contains the role of the sender (user or assistant) and the message content.
/// </summary>
public class ChatMessage
{
    /// <summary>
    /// Gets or sets the role of the message. It should be "System", "User" or "Assistant".
    /// </summary>
    public MessageRoleType Role { get; set; } = MessageRoleType.None;

    /// <summary>
    /// Gets or sets the content of the message.
    /// </summary>
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// This defines the role of the message.
/// </summary>
public enum MessageRoleType
{
    /// <summary>
    /// Identifies no specific role.
    /// </summary>
    None,

    /// <summary>
    /// Identifies the system role.
    /// </summary>
    System,

    /// <summary>
    /// Identifies the user role.
    /// </summary>
    User,

    /// <summary>
    /// Identifies the assistant role.
    /// </summary>
    Assistant,

    /// <summary>
    /// Identifies the tool role.
    /// </summary>
    Tool
}
