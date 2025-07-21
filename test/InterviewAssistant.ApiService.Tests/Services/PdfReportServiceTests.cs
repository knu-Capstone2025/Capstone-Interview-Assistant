using InterviewAssistant.ApiService.Services;
using InterviewAssistant.Common.Models;

using Shouldly;

namespace InterviewAssistant.ApiService.Tests.Services;

[TestFixture]
public class PdfReportServiceTests
{
    private PdfReportService _pdfReportService;
    private InterviewReport _testReport;

    [SetUp]
    public void Setup()
    {
        _pdfReportService = new PdfReportService();
        _testReport = new InterviewReport
        {
            CandidateName = "김지원",
            Position = "백엔드 개발자",
            InterviewDate = new DateTime(2025, 7, 22),
            FitnessScore = 8,
            QuestionsAndAnswers = new List<QuestionAnswer>
            {
                new QuestionAnswer
                {
                    Question = "**자기소개**를 해주세요.",
                    Answer = "안녕하세요. 저는 *3년 경력*의 백엔드 개발자입니다.",
                    Feedback = "**좋은 답변**입니다. 더 구체적인 경험을 추가하면 좋겠습니다."
                },
                new QuestionAnswer
                {
                    Question = "### 기술적 경험\n- C# 경험이 있나요?\n- 어떤 프로젝트를 했나요?",
                    Answer = "네, **5년간** C#으로 개발했습니다.\n- 웹 API 개발\n- 마이크로서비스 아키텍처",
                    Feedback = ""
                }
            },
            OverallFeedback = "### 종합 피드백\n\n**강점:**\n- 기술적 역량이 우수함\n- 커뮤니케이션 능력이 좋음\n\n**개선점:**\n- 더 구체적인 사례 필요\n- 리더십 경험 보완 필요",
            Strengths = new List<string>
            {
                "**기술적 전문성**이 뛰어남",
                "*문제 해결 능력*이 우수함",
                "팀워크가 좋음"
            },
            ImprovementAreas = new List<string>
            {
                "**리더십 경험** 부족",
                "*프로젝트 관리* 스킬 향상 필요",
                "### 도메인 지식 확장 필요"
            },
            FinalAssessment = "**최종 평가:** 전반적으로 우수한 후보자입니다.\n\n- 기술적 역량: **8/10**\n- 커뮤니케이션: *7/10*\n- 성장 가능성: **9/10**"
        };
    }

    [Test]
    public void GenerateInterviewReport_ShouldReturnValidPdfBytes()
    {
        // Act
        var result = _pdfReportService.GenerateInterviewReport(_testReport);

        // Assert
        result.ShouldNotBeNull();
        result.Length.ShouldBeGreaterThan(0);
        
        // PDF 파일 시그니처 확인 (%PDF)
        result[0].ShouldBe((byte)'%');
        result[1].ShouldBe((byte)'P');
        result[2].ShouldBe((byte)'D');
        result[3].ShouldBe((byte)'F');
    }

    [Test]
    public void GenerateInterviewReport_WithEmptyReport_ShouldNotThrow()
    {
        // Arrange
        var emptyReport = new InterviewReport
        {
            CandidateName = "",
            Position = "",
            InterviewDate = DateTime.Now,
            QuestionsAndAnswers = new List<QuestionAnswer>(),
            OverallFeedback = "",
            Strengths = new List<string>(),
            ImprovementAreas = new List<string>(),
            FitnessScore = 0,
            FinalAssessment = ""
        };

        // Act & Assert
        Should.NotThrow(() => _pdfReportService.GenerateInterviewReport(emptyReport));
    }

    [Test]
    public void GenerateInterviewReport_WithMarkdownContent_ShouldProcessCorrectly()
    {
        // Arrange
        var markdownReport = new InterviewReport
        {
            CandidateName = "테스트 지원자",
            Position = "테스트 포지션",
            InterviewDate = DateTime.Now,
            QuestionsAndAnswers = new List<QuestionAnswer>
            {
                new QuestionAnswer
                {
                    Question = "**볼드 텍스트**와 *이탤릭 텍스트*가 포함된 질문입니다.",
                    Answer = "### 헤더\n- 리스트 항목 1\n- 리스트 항목 2\n\n**강조된 답변**입니다.",
                    Feedback = "*피드백*에도 **마크다운**이 포함될 수 있습니다."
                }
            },
            OverallFeedback = "### 전체 피드백\n\n**우수한 점:**\n- 항목 1\n- 항목 2",
            Strengths = new List<string> { "**강점 1**", "*강점 2*" },
            ImprovementAreas = new List<string> { "### 개선점 1", "- 개선점 2" },
            FitnessScore = 7,
            FinalAssessment = "**최종:** 합격 추천"
        };

        // Act
        var result = _pdfReportService.GenerateInterviewReport(markdownReport);

        // Assert
        result.ShouldNotBeNull();
        result.Length.ShouldBeGreaterThan(0);
    }

    [Test]
    public void GenerateInterviewReport_WithNullReport_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => _pdfReportService.GenerateInterviewReport(null!));
    }

    [Test]
    public void GenerateInterviewReport_WithNullProperties_ShouldHandleGracefully()
    {
        // Arrange
        var reportWithNulls = new InterviewReport
        {
            CandidateName = null!,
            Position = null!,
            InterviewDate = DateTime.Now,
            QuestionsAndAnswers = null!,
            OverallFeedback = null!,
            Strengths = null!,
            ImprovementAreas = null!,
            FitnessScore = 5,
            FinalAssessment = null!
        };

        // Act & Assert
        Should.NotThrow(() => _pdfReportService.GenerateInterviewReport(reportWithNulls));
    }

    [Test]
    public void GenerateInterviewReport_WithLargeContent_ShouldGenerateSuccessfully()
    {
        // Arrange
        var largeContent = string.Join("\n", Enumerable.Repeat("**매우 긴 텍스트** 내용이 포함된 *마크다운* 텍스트입니다. ### 헤더도 있고 - 리스트도 있습니다.", 100));
        
        var largeReport = new InterviewReport
        {
            CandidateName = "대용량 테스트 지원자",
            Position = "대용량 테스트 포지션",
            InterviewDate = DateTime.Now,
            QuestionsAndAnswers = Enumerable.Range(1, 20).Select(i => new QuestionAnswer
            {
                Question = $"**질문 {i}:** {largeContent.Substring(0, Math.Min(200, largeContent.Length))}",
                Answer = $"**답변 {i}:** {largeContent.Substring(0, Math.Min(300, largeContent.Length))}",
                Feedback = $"*피드백 {i}:* {largeContent.Substring(0, Math.Min(150, largeContent.Length))}"
            }).ToList(),
            OverallFeedback = largeContent,
            Strengths = Enumerable.Range(1, 10).Select(i => $"**강점 {i}:** {largeContent.Substring(0, Math.Min(100, largeContent.Length))}").ToList(),
            ImprovementAreas = Enumerable.Range(1, 10).Select(i => $"*개선점 {i}:* {largeContent.Substring(0, Math.Min(100, largeContent.Length))}").ToList(),
            FitnessScore = 6,
            FinalAssessment = largeContent
        };

        // Act
        var result = _pdfReportService.GenerateInterviewReport(largeReport);

        // Assert
        result.ShouldNotBeNull();
        result.Length.ShouldBeGreaterThan(1000); // 큰 파일이어야 함
    }

    [Test]
    public void GenerateInterviewReport_WithSpecialCharacters_ShouldHandleCorrectly()
    {
        // Arrange
        var specialCharReport = new InterviewReport
        {
            CandidateName = "김영희 (한글 이름)",
            Position = "소프트웨어 엔지니어 & 데이터 사이언티스트",
            InterviewDate = DateTime.Now,
            QuestionsAndAnswers = new List<QuestionAnswer>
            {
                new QuestionAnswer
                {
                    Question = "**특수문자** 테스트: @#$%^&*()_+{}|:<>?[]\\;'\",./ 및 한글 🚀",
                    Answer = "*이모지* 포함: 👍 💻 🎯 및 특수문자: `code` & <html>",
                    Feedback = "### 피드백: ★☆♡♢♠♣♤♥"
                }
            },
            OverallFeedback = "**전체 평가:** 한글과 English mixed content 🌟",
            Strengths = new List<string> { "다국어 지원 ✅", "특수문자 처리 능력 🎉" },
            ImprovementAreas = new List<string> { "인코딩 이슈 해결 ⚠️", "문자 렌더링 개선 📝" },
            FitnessScore = 8,
            FinalAssessment = "**최종 결과:** 특수문자 테스트 완료 ✨"
        };

        // Act & Assert
        Should.NotThrow(() => _pdfReportService.GenerateInterviewReport(specialCharReport));
    }

    [TearDown]
    public void TearDown()
    {
        // Clean up resources if needed
    }
}
