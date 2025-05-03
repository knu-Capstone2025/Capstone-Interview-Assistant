using InterviewAssistant.ApiService.Data;
using InterviewAssistant.ApiService.Models;

namespace InterviewAssistant.ApiService.Repositories;

public class ResumeRepository : IResumeRepository
{
    private readonly ResumeDbContext _db;

    public ResumeRepository(ResumeDbContext db)
    {
        _db = db;
    }

    public void Save(string type, string content)
    {
        var existing = _db.ResumeEntries.FirstOrDefault(e => e.Type == type);
        if (existing != null)
        {
            existing.Content = content;
        }
        else
        {
            _db.ResumeEntries.Add(new ResumeEntry { Type = type, Content = content });
        }
        _db.SaveChanges();
    }

    public string? Get(string type)
    {
        return _db.ResumeEntries.FirstOrDefault(e => e.Type == type)?.Content;
    }
}
