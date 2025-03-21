namespace InterviewAssistant.Common.Models;

/// <summary>
/// This represents the request entity for the interview data service. It contains the URL of the resume and the job description that are used to generate interview questions and answers.
/// </summary>
public class InterviewDataRequest
{
    /// <summary>
    /// Gets or sets the URL of the resume document.
    /// </summary>
    public string ResumeUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the URL of the job description document.
    /// </summary>
    public string JobDescriptionUrl { get; set; } = string.Empty;
}