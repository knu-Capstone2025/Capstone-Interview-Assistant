using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace InterviewAssistant.Common.Models;

public class ApplicationDataRequest{

    [Required]
    [JsonPropertyName("resumeUrl")]
    public string ResumeUrl { get; set; } = string.Empty;

    [Required]
    [JsonPropertyName("jobPostingUrl")]
    public string JobPostingUrl { get; set; } = string.Empty;

}