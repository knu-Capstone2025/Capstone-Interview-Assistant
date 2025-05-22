using InterviewAssistant.ApiService.Data;
using InterviewAssistant.ApiService.Models;

using Microsoft.EntityFrameworkCore;

namespace InterviewAssistant.ApiService.Repositories;

public class InterviewRepository(InterviewDbContext db) : IInterviewRepository
{
    public async Task SaveResumeAsync(ResumeEntry entity)
    {
        await db.Resumes.AddAsync(entity);
        await db.SaveChangesAsync();
    }

    public async Task<ResumeEntry?> GetResumeByIdAsync(Guid id)
    {
        return await db.Resumes.FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task SaveJobAsync(JobDescriptionEntry entity)
    {
        await db.JobDescriptions.AddAsync(entity);
        await db.SaveChangesAsync();
    }

    public async Task<JobDescriptionEntry?> GetJobByIdAsync(Guid id)
    {
        return await db.JobDescriptions.FirstOrDefaultAsync(e => e.Id == id);
    }
}
