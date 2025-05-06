namespace InterviewAssistant.ApiService.Models;

public class ResumeEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Content { get; set; } = string.Empty;
    public JobDescriptionEntry? JobDescription { get; set; }
}