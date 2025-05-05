using InterviewAssistant.ApiService.Models;

using Microsoft.EntityFrameworkCore;

namespace InterviewAssistant.ApiService.Data;

public class InterviewDbContext : DbContext
{
    public InterviewDbContext(DbContextOptions<InterviewDbContext> options) : base(options) { }

    public DbSet<ResumeEntry> Resumes { get; set; } = null!;
    public DbSet<JobDescriptionEntry> JobDescriptions { get; set; } = null!;
}
