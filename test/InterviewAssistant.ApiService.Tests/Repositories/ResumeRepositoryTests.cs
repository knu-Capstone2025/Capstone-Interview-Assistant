using NUnit.Framework;
using Microsoft.EntityFrameworkCore;
using InterviewAssistant.ApiService.Data;
using InterviewAssistant.ApiService.Models;
using InterviewAssistant.ApiService.Repositories;
using Shouldly;

namespace InterviewAssistant.Tests.Repositories
{
    public class ResumeRepositoryTests
    {
        private ResumeDbContext CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<ResumeDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new ResumeDbContext(options);
        }

        [Test]
        public void Save_Should_Insert_New_ResumeEntry()
        {
            // Arrange
            var db = CreateInMemoryDbContext();
            var repo = new ResumeRepository(db);

            // Act
            repo.Save("resume", "이력서 내용");

            // Assert
            var result = db.ResumeEntries.FirstOrDefault(e => e.Type == "resume");
            result.ShouldNotBeNull();
            result!.Content.ShouldBe("이력서 내용");
        }

        [Test]
        public void Save_Should_Update_Existing_Entry()
        {
            // Arrange
            var db = CreateInMemoryDbContext();
            db.ResumeEntries.Add(new ResumeEntry { Type = "job", Content = "기존 내용" });
            db.SaveChanges();

            var repo = new ResumeRepository(db);

            // Act
            repo.Save("job", "수정된 내용");

            // Assert
            var result = db.ResumeEntries.FirstOrDefault(e => e.Type == "job");
            result!.Content.ShouldBe("수정된 내용");
        }

        [Test]
        public void Get_Should_Return_Correct_Resume_Content()
        {
            // Arrange
            var db = CreateInMemoryDbContext();
            db.ResumeEntries.Add(new ResumeEntry { Type = "resume", Content = "테스트 이력서" });
            db.SaveChanges();

            var repo = new ResumeRepository(db);

            // Act
            var result = repo.Get("resume");

            // Assert
            result.ShouldBe("테스트 이력서");
        }
    }
}