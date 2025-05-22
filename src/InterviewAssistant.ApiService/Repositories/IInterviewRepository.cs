using InterviewAssistant.ApiService.Models;

namespace InterviewAssistant.ApiService.Repositories;

public interface IInterviewRepository
{
    Task SaveResumeAsync(ResumeEntry entity);
    Task<ResumeEntry?> GetResumeByIdAsync(Guid id);
    Task SaveJobAsync(JobDescriptionEntry entity);
    Task<JobDescriptionEntry?> GetJobByIdAsync(Guid id);
}
