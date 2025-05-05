using InterviewAssistant.ApiService.Data;
using InterviewAssistant.ApiService.Models;

using Microsoft.EntityFrameworkCore;

namespace InterviewAssistant.ApiService.Repositories;

public class InterviewRepository : IInterviewRepository
{
    private readonly InterviewDbContext _db;

    public InterviewRepository(InterviewDbContext db)
    {
        _db = db;
    }

    public async Task SaveResumeAsync(ResumeEntry entity)
    {
        var existing = await _db.Resumes.FirstOrDefaultAsync(e => e.Id == entity.Id);
        if (existing != null)
        {
            existing.Content = entity.Content;
        }
        else
        {
            await _db.Resumes.AddAsync(entity);
        }
        await _db.SaveChangesAsync();
    }

    public async Task<ResumeEntry?> GetResumeByIdAsync(Guid id)
    {
        return await _db.Resumes.FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task SaveJobAsync(JobDescriptionEntry entity)
    {
        var existing = await _db.JobDescriptions.FirstOrDefaultAsync(e => e.Id == entity.Id);
        if (existing != null)
        {
            existing.Content = entity.Content;
        }
        else
        {
            await _db.JobDescriptions.AddAsync(entity);
        }
        await _db.SaveChangesAsync();
    }

    public async Task<JobDescriptionEntry?> GetJobByIdAsync(Guid id)
    {
        return await _db.JobDescriptions.FirstOrDefaultAsync(e => e.Id == id);
    }
}