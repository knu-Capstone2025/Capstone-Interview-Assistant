using InterviewAssistant.ApiService.Models;

using Microsoft.EntityFrameworkCore;

namespace InterviewAssistant.ApiService.Data;

public class InterviewDbContext(DbContextOptions<InterviewDbContext> options) : DbContext(options)
{
    public DbSet<ResumeEntry> Resumes { get; set; } = null!;
    public DbSet<JobDescriptionEntry> JobDescriptions { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ResumeEntry>()
                    .HasOne(r => r.JobDescription)
                    .WithOne(j => j.Resume)
                    .HasForeignKey<JobDescriptionEntry>(j => j.ResumeEntryId);
    }
}
