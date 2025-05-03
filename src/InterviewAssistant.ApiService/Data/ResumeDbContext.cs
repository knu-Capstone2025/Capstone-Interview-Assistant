using InterviewAssistant.ApiService.Models;

using Microsoft.EntityFrameworkCore;

namespace InterviewAssistant.ApiService.Data;

public class ResumeDbContext : DbContext
{
    public ResumeDbContext(DbContextOptions<ResumeDbContext> options) : base(options) { }

    public DbSet<ResumeEntry> ResumeEntries { get; set; } = null!;
}
