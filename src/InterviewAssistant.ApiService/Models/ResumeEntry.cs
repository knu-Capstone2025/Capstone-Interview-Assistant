namespace InterviewAssistant.ApiService.Models;

public class ResumeEntry
{
    public int Id { get; set; }
    public string Type { get; set; } = ""; // "resume" or "job"
    public string Content { get; set; } = "";
}
