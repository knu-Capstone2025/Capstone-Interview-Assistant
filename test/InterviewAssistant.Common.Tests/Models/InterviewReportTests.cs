using InterviewAssistant.Common.Models;

using NUnit.Framework;

using Shouldly;

namespace InterviewAssistant.Common.Tests.Models;

[TestFixture]
public class InterviewReportTests
{
    [Test]
    public void InterviewReport_DefaultValues_ShouldBeInitialized()
    {
        // Arrange & Act
        var report = new InterviewReport();

        // Assert
        report.CandidateName.ShouldBe(string.Empty);
        report.Position.ShouldBe(string.Empty);
        report.InterviewDate.ShouldBeGreaterThan(DateTime.MinValue);
        report.InterviewDate.ShouldBeLessThanOrEqualTo(DateTime.Now);
        report.QuestionsAndAnswers.ShouldNotBeNull();
        report.QuestionsAndAnswers.ShouldBeEmpty();
        report.OverallFeedback.ShouldBe(string.Empty);
        report.Strengths.ShouldNotBeNull();
        report.Strengths.ShouldBeEmpty();
        report.ImprovementAreas.ShouldNotBeNull();
        report.ImprovementAreas.ShouldBeEmpty();
        report.FitnessScore.ShouldBe(0);
        report.FinalAssessment.ShouldBe(string.Empty);
    }

    [Test]
    public void InterviewReport_MarkdownContent_ShouldPreserveFormatting()
    {
        // Arrange
        var interviewDate = new DateTime(2024, 12, 25, 14, 30, 0);
        var questionsAndAnswers = new List<QuestionAnswer>
        {
            new QuestionAnswer
            {
                Question = "**강점**은 무엇인가요?",
                Answer = "저의 *주요 강점*은:\n- 문제 해결 능력\n- 팀워크"
            }
        };

        var strengths = new List<string> { "**기술적 역량**", "*커뮤니케이션*" };
        var improvementAreas = new List<string> { "### 개선 필요", "- 시간 관리" };

        // Act
        var report = new InterviewReport
        {
            CandidateName = "김테스트",
            Position = "시니어 개발자",
            InterviewDate = interviewDate,
            QuestionsAndAnswers = questionsAndAnswers,
            OverallFeedback = "### 종합 피드백\n\n**우수한** 후보자입니다.",
            Strengths = strengths,
            ImprovementAreas = improvementAreas,
            FitnessScore = 8,
            FinalAssessment = "*추천* 합니다."
        };

        // Assert
        report.CandidateName.ShouldBe("김테스트");
        report.Position.ShouldBe("시니어 개발자");
        report.InterviewDate.ShouldBe(interviewDate);
        report.QuestionsAndAnswers.ShouldBe(questionsAndAnswers);
        report.QuestionsAndAnswers.Count.ShouldBe(1);
        report.OverallFeedback.ShouldBe("### 종합 피드백\n\n**우수한** 후보자입니다.");
        report.Strengths.ShouldBe(strengths);
        report.ImprovementAreas.ShouldBe(improvementAreas);
        report.FitnessScore.ShouldBe(8);
        report.FinalAssessment.ShouldBe("*추천* 합니다.");
    }

    [Test]
    public void InterviewReport_WithSpecialCharacters_ShouldHandleCorrectly()
    {
        // Arrange
        var specialContent = "특수문자: @#$%^&*()[]{}|\\:;\"'<>?,./`~";
        var unicodeContent = "유니코드: 한글, 中文, 日本語, Français, العربية";

        // Act
        var report = new InterviewReport
        {
            CandidateName = specialContent,
            Position = unicodeContent,
            OverallFeedback = $"{specialContent}\n{unicodeContent}",
            Strengths = new List<string> { specialContent },
            ImprovementAreas = new List<string> { unicodeContent },
            FinalAssessment = "특수문자와 유니코드 테스트 완료"
        };

        // Assert
        report.CandidateName.ShouldBe(specialContent);
        report.Position.ShouldBe(unicodeContent);
        report.OverallFeedback.ShouldContain(specialContent);
        report.OverallFeedback.ShouldContain(unicodeContent);
        report.Strengths.ShouldContain(specialContent);
        report.ImprovementAreas.ShouldContain(unicodeContent);
        report.FinalAssessment.ShouldBe("특수문자와 유니코드 테스트 완료");
    }

    [Test]
    public void InterviewReport_LargeContent_ShouldHandleCorrectly()
    {
        // Arrange
        var largeContent = string.Join("\n", Enumerable.Repeat("매우 긴 텍스트 내용입니다. ", 1000));
        var manyQuestions = Enumerable.Range(1, 50)
            .Select(i => new QuestionAnswer
            {
                Question = $"질문 {i}: {largeContent.Substring(0, 100)}",
                Answer = $"답변 {i}: {largeContent.Substring(0, 200)}"
            })
            .ToList();

        // Act
        var report = new InterviewReport
        {
            QuestionsAndAnswers = manyQuestions,
            OverallFeedback = largeContent,
            Strengths = Enumerable.Range(1, 20).Select(i => $"강점 {i}").ToList(),
            ImprovementAreas = Enumerable.Range(1, 20).Select(i => $"개선영역 {i}").ToList()
        };

        // Assert
        report.QuestionsAndAnswers.Count.ShouldBe(50);
        report.OverallFeedback.Length.ShouldBeGreaterThan(10000);
        report.Strengths.Count.ShouldBe(20);
        report.ImprovementAreas.Count.ShouldBe(20);
    }

    [Test]
    public void InterviewReport_NullSafety_ShouldHandleNullValues()
    {
        // Arrange & Act
        var report = new InterviewReport
        {
            QuestionsAndAnswers = new List<QuestionAnswer>(),
            Strengths = new List<string>(),
            ImprovementAreas = new List<string>()
        };

        // Assert
        report.QuestionsAndAnswers.ShouldNotBeNull();
        report.Strengths.ShouldNotBeNull();
        report.ImprovementAreas.ShouldNotBeNull();
    }

    [Test]
    [TestCase(0)]
    [TestCase(5)]
    [TestCase(10)]
    [TestCase(-1)] // 경계값 테스트
    [TestCase(15)] // 경계값 테스트
    public void InterviewReport_FitnessScore_ShouldAcceptAllValues(int score)
    {
        // Arrange & Act
        var report = new InterviewReport { FitnessScore = score };

        // Assert
        report.FitnessScore.ShouldBe(score);
        report.FitnessScore.ShouldBeOfType<int>();
    }

    [Test]
    public void QuestionAnswer_DefaultValues_ShouldBeInitialized()
    {
        // Arrange & Act
        var qa = new QuestionAnswer();

        // Assert
        qa.Question.ShouldBe(string.Empty);
        qa.Answer.ShouldBe(string.Empty);
    }

    [Test]
    public void QuestionAnswer_WithValues_ShouldStoreCorrectly()
    {
        // Arrange
        var question = "**중요한** 질문입니다.";
        var answer = "*자세한* 답변입니다.";

        // Act
        var qa = new QuestionAnswer
        {
            Question = question,
            Answer = answer
        };

        // Assert
        qa.Question.ShouldBe(question);
        qa.Answer.ShouldBe(answer);
    }

    [Test]
    public void QuestionAnswer_MarkdownFormatting_ShouldPreserve()
    {
        // Arrange
        var questionWithMarkdown = "### 질문\n- **항목 1**\n- *항목 2*";
        var answerWithMarkdown = "#### 답변\n1. 첫 번째\n2. 두 번째\n\n`코드 예시`";

        // Act
        var qa = new QuestionAnswer
        {
            Question = questionWithMarkdown,
            Answer = answerWithMarkdown
        };

        // Assert
        qa.Question.ShouldContain("### 질문");
        qa.Question.ShouldContain("**항목 1**");
        qa.Question.ShouldContain("*항목 2*");
        qa.Answer.ShouldContain("#### 답변");
        qa.Answer.ShouldContain("`코드 예시`");
    }

    [Test]
    public void QuestionAnswer_LongContent_ShouldHandle()
    {
        // Arrange
        var longQuestion = string.Join(" ", Enumerable.Repeat("매우 긴 질문 내용", 100));
        var longAnswer = string.Join("\n", Enumerable.Repeat("매우 긴 답변 내용", 100));

        // Act
        var qa = new QuestionAnswer
        {
            Question = longQuestion,
            Answer = longAnswer
        };

        // Assert
        qa.Question.Length.ShouldBeGreaterThan(1000);
        qa.Answer.Length.ShouldBeGreaterThan(1000);
        qa.Question.ShouldStartWith("매우 긴 질문 내용");
        qa.Answer.ShouldStartWith("매우 긴 답변 내용");
    }

    [Test]
    public void QuestionAnswer_SpecialCharacters_ShouldPreserve()
    {
        // Arrange
        var specialQuestion = "질문: \"따옴표\", '작은따옴표', <태그>, &amp; 특수문자";
        var specialAnswer = "답변: JSON { \"key\": \"value\" }, XML <root>content</root>";

        // Act
        var qa = new QuestionAnswer
        {
            Question = specialQuestion,
            Answer = specialAnswer
        };

        // Assert
        qa.Question.ShouldBe(specialQuestion);
        qa.Answer.ShouldBe(specialAnswer);
        qa.Question.ShouldContain("\"따옴표\"");
        qa.Answer.ShouldContain("{ \"key\": \"value\" }");
    }
}
