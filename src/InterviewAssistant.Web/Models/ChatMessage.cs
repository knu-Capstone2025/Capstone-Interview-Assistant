namespace InterviewAssistant.Web.Models;

/// <summary>
/// 챗 메시지 모델
/// </summary>
public class ChatMessage
{
    /// <summary>
    /// 사용자 메시지 여부를 나타냅니다.
    /// </summary>
    public bool IsUser { get; set; }

    /// <summary>
    /// 메시지 내용
    /// </summary>
    public string Content { get; set; } = string.Empty;
}
