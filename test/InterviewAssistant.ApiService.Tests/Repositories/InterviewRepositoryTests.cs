using InterviewAssistant.ApiService.Data;
using InterviewAssistant.ApiService.Models;
using InterviewAssistant.ApiService.Repositories;

using Microsoft.EntityFrameworkCore;

using Shouldly;

namespace InterviewAssistant.Tests.Repositories
{
    public class InterviewRepositoryTests
    {
        private InterviewDbContext CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<InterviewDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // 고유 DB로 테스트 격리
                .Options;

            return new InterviewDbContext(options);
        }

        [Test]
        public async Task SaveResumeAsync_Should_Insert_New_Resume()
        {
            // Arrange
            var db = CreateInMemoryDbContext();
            var repo = new InterviewRepository(db);
            var resume = new ResumeEntry { Id = Guid.NewGuid(), Content = "이력서 내용" };

            // Act
            await repo.SaveResumeAsync(resume);

            // Assert
            var result = await db.Resumes.FirstOrDefaultAsync(e => e.Id == resume.Id);
            result.ShouldNotBeNull();
            result!.Content.ShouldBe("이력서 내용");
        }

        [Test]
        public async Task GetResumeByIdAsync_Should_Return_Resume_When_Exists()
        {
            // Arrange
            var db = CreateInMemoryDbContext();
            var resume = new ResumeEntry { Id = Guid.NewGuid(), Content = "테스트 이력서" };
            await db.Resumes.AddAsync(resume);
            await db.SaveChangesAsync();

            var repo = new InterviewRepository(db);

            // Act
            var result = await repo.GetResumeByIdAsync(resume.Id);

            // Assert
            result.ShouldNotBeNull();
            result!.Content.ShouldBe("테스트 이력서");
        }

        [Test]
        public async Task SaveJobAsync_Should_Insert_New_Job()
        {
            // Arrange
            var db = CreateInMemoryDbContext();
            var repo = new InterviewRepository(db);
            var job = new JobDescriptionEntry { Id = Guid.NewGuid(), Content = "채용공고 내용" };

            // Act
            await repo.SaveJobAsync(job);

            // Assert
            var result = await db.JobDescriptions.FirstOrDefaultAsync(e => e.Id == job.Id);
            result.ShouldNotBeNull();
            result!.Content.ShouldBe("채용공고 내용");
        }

        [Test]
        public async Task GetJobByIdAsync_Should_Return_Job_When_Exists()
        {
            // Arrange
            var db = CreateInMemoryDbContext();
            var job = new JobDescriptionEntry { Id = Guid.NewGuid(), Content = "테스트 채용공고" };
            await db.JobDescriptions.AddAsync(job);
            await db.SaveChangesAsync();

            var repo = new InterviewRepository(db);

            // Act
            var result = await repo.GetJobByIdAsync(job.Id);

            // Assert
            result.ShouldNotBeNull();
            result!.Content.ShouldBe("테스트 채용공고");
        }
    }
}
