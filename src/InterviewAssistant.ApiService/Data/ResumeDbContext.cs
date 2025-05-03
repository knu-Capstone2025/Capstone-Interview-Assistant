using Microsoft.EntityFrameworkCore;
using InterviewAssistant.ApiService.Models;

namespace InterviewAssistant.ApiService.Data;

public class ResumeDbContext : DbContext
{
    public ResumeDbContext(DbContextOptions<ResumeDbContext> options) : base(options) { }

    public DbSet<ResumeEntry> ResumeEntries { get; set; } = null!;
}
