namespace InterviewAssistant.ApiService.Models;

public class InterviewSession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ResumeId { get; set; }
    public Guid JobDescriptionId { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public bool IsCompleted { get; set; }
    public string QuestionsAndAnswers { get; set; } = "[]"; // JSON 형태로 저장
    public string? FinalFeedback { get; set; }
}
