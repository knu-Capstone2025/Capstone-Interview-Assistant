namespace InterviewAssistant.ApiService.Models;

public class JobDescriptionEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Content { get; set; } = string.Empty;
}
