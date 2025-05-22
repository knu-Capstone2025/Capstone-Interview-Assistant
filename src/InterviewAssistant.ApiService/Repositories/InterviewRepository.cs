using InterviewAssistant.ApiService.Data;
using InterviewAssistant.ApiService.Models;

using Microsoft.EntityFrameworkCore;

namespace InterviewAssistant.ApiService.Repositories;

public class InterviewRepository(InterviewDbContext db) : IInterviewRepository
{
    public async Task SaveResumeAsync(ResumeEntry entity)
    {
        // 해당 이력서가 정확히 하나라고 보장되어 있고 복수 존재함이 불가능
        var existing = await db.Resumes.SingleOrDefaultAsync(e => e.Id == entity.Id);
        // 이력서가 이미 존재하는 경우, 내용을 업데이트합니다
        if (existing != null)
        {
            existing.Content = entity.Content;
        }
        // 이력서가 없으면 새로 추가
        else
        {
            await db.Resumes.AddAsync(entity);
        }
        await db.SaveChangesAsync();
    }

    public async Task<ResumeEntry?> GetResumeByIdAsync(Guid id)
    {
        return await db.Resumes.FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task SaveJobAsync(JobDescriptionEntry entity)
    {
        var existing = await db.JobDescriptions.SingleOrDefaultAsync(e => e.Id == entity.Id);
        if (existing != null)
        {
            existing.Content = entity.Content;
            existing.ResumeEntryId = entity.ResumeEntryId;
        }
        else
        {
            await db.JobDescriptions.AddAsync(entity);
        }
        await db.SaveChangesAsync();
    }

    public async Task<JobDescriptionEntry?> GetJobByIdAsync(Guid id)
    {
        return await db.JobDescriptions.FirstOrDefaultAsync(e => e.Id == id);
    }
}
