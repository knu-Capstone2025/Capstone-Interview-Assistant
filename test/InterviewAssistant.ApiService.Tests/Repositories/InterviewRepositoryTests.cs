using InterviewAssistant.ApiService.Data;
using InterviewAssistant.ApiService.Models;
using InterviewAssistant.ApiService.Repositories;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

using Shouldly;

namespace InterviewAssistant.Tests.Repositories
{
    public class InterviewRepositoryTests
    {
        private InterviewDbContext CreateSQLiteInMemoryDbContext(SqliteConnection connection)
        {
            var options = new DbContextOptionsBuilder<InterviewDbContext>()
                .UseSqlite(connection)
                .Options;

            var context = new InterviewDbContext(options);
            context.Database.EnsureCreated(); // 반드시 생성 필요
            return context;
        }

        private SqliteConnection CreateOpenConnection()
        {
            var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open(); // In-memory 유지하려면 반드시 Open 필요
            return connection;
        }

        [Test]
        public async Task SaveResumeAsync_Should_Insert_New_Resume()
        {
            using var connection = CreateOpenConnection();
            using var db = CreateSQLiteInMemoryDbContext(connection);
            var repo = new InterviewRepository(db);
            var resume = new ResumeEntry { Id = Guid.NewGuid(), Content = "이력서 내용" };

            await repo.SaveResumeAsync(resume);

            var result = await db.Resumes.FirstOrDefaultAsync(e => e.Id == resume.Id);
            result.ShouldNotBeNull();
            result!.Content.ShouldBe("이력서 내용");
        }

        [Test]
        public async Task GetResumeByIdAsync_Should_Return_Resume_When_Exists()
        {
            using var connection = CreateOpenConnection();
            using var db = CreateSQLiteInMemoryDbContext(connection);
            var resume = new ResumeEntry { Id = Guid.NewGuid(), Content = "테스트 이력서" };
            await db.Resumes.AddAsync(resume);
            await db.SaveChangesAsync();

            var repo = new InterviewRepository(db);
            var result = await repo.GetResumeByIdAsync(resume.Id);

            result.ShouldNotBeNull();
            result!.Content.ShouldBe("테스트 이력서");
        }

        [Test]
        public async Task SaveJobAsync_Should_Insert_New_Job()
        {
            using var connection = CreateOpenConnection();
            using var db = CreateSQLiteInMemoryDbContext(connection);
            var repo = new InterviewRepository(db);
            var job = new JobDescriptionEntry { Id = Guid.NewGuid(), Content = "채용공고 내용" };

            await repo.SaveJobAsync(job);

            var result = await db.JobDescriptions.FirstOrDefaultAsync(e => e.Id == job.Id);
            result.ShouldNotBeNull();
            result!.Content.ShouldBe("채용공고 내용");
        }

        [Test]
        public async Task GetJobByIdAsync_Should_Return_Job_When_Exists()
        {
            using var connection = CreateOpenConnection();
            using var db = CreateSQLiteInMemoryDbContext(connection);
            var job = new JobDescriptionEntry { Id = Guid.NewGuid(), Content = "테스트 채용공고" };
            await db.JobDescriptions.AddAsync(job);
            await db.SaveChangesAsync();

            var repo = new InterviewRepository(db);
            var result = await repo.GetJobByIdAsync(job.Id);

            result.ShouldNotBeNull();
            result!.Content.ShouldBe("테스트 채용공고");
        }
    }
}
