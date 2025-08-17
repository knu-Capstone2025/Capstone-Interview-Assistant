using InterviewAssistant.ApiService.Services;
using InterviewAssistant.Common.Models;
using NUnit.Framework;
using Shouldly;

namespace InterviewAssistant.ApiService.Tests.Services;

[TestFixture]
public class PdfGenerationServiceTests
{
    private IPdfGenerationService _pdfService;

    [SetUp]
    public void Setup()
    {
        _pdfService = new PdfGenerationService();
    }

    [Test]
    public async Task GenerateInterviewReportPdfAsync_WithValidData_ShouldReturnPdfBytes()
    {
        // Arrange
        var report = new InterviewReportModel
        {
            OverallFeedback = "전반적으로 **좋은** 면접이었습니다.",
            Strengths = ["명확한 의사소통", "적극적인 태도"],
            Weaknesses = ["구체적 예시 부족"],
            ChartData = new ChartDataModel
            {
                Labels = ["기술", "경험", "인성"],
                Values = [5, 3, 2]
            }
        };

        var chatHistory = new List<ChatMessage>
        {
            new() { Role = MessageRoleType.Assistant, Message = "안녕하세요! 자기소개 부탁드립니다." },
            new() { Role = MessageRoleType.User, Message = "저는 **개발자**입니다." }
        };

        // Act
        var result = await _pdfService.GenerateInterviewReportPdfAsync(report, chatHistory);

        // Assert
        result.ShouldNotBeNull();
        result.Length.ShouldBeGreaterThan(0);
        
        // PDF 헤더 확인 (PDF 파일은 %PDF로 시작)
        var pdfHeader = System.Text.Encoding.ASCII.GetString(result.Take(4).ToArray());
        pdfHeader.ShouldBe("%PDF");
    }

    [Test]
    public async Task GenerateInterviewReportPdfAsync_WithEmptyData_ShouldStillGeneratePdf()
    {
        // Arrange
        var report = new InterviewReportModel();
        var chatHistory = new List<ChatMessage>();

        // Act
        var result = await _pdfService.GenerateInterviewReportPdfAsync(report, chatHistory);

        // Assert
        result.ShouldNotBeNull();
        result.Length.ShouldBeGreaterThan(0);
    }

    [Test]
    public async Task GenerateInterviewReportPdfAsync_WithMarkdownContent_ShouldProcessMarkdown()
    {
        // Arrange
        var report = new InterviewReportModel
        {
            OverallFeedback = "**굵은 글씨**와 *기울임*이 포함된 텍스트입니다.",
            Strengths = ["# 제목이 있는 강점", "- 리스트 형태의 강점"],
            Weaknesses = ["`코드`가 포함된 개선점"]
        };

        var chatHistory = new List<ChatMessage>
        {
            new() { Role = MessageRoleType.User, Message = "## 제목\n\n이것은 **마크다운** 텍스트입니다." }
        };

        // Act
        var result = await _pdfService.GenerateInterviewReportPdfAsync(report, chatHistory);

        // Assert
        result.ShouldNotBeNull();
        result.Length.ShouldBeGreaterThan(0);
    }

    [Test]
    public async Task GenerateInterviewReportPdfAsync_WithChartData_ShouldIncludeChartVisualization()
    {
        // Arrange
        var report = new InterviewReportModel
        {
            OverallFeedback = "차트가 포함된 리포트입니다.",
            ChartData = new ChartDataModel
            {
                Labels = ["기술", "경험", "인성", "문제해결"],
                Values = [10, 5, 3, 2]
            }
        };

        var chatHistory = new List<ChatMessage>();

        // Act
        var result = await _pdfService.GenerateInterviewReportPdfAsync(report, chatHistory);

        // Assert
        result.ShouldNotBeNull();
        result.Length.ShouldBeGreaterThan(0);
    }

    [Test]
    public async Task GenerateInterviewReportPdfAsync_WithNullReport_ShouldThrowNullReferenceException()
    {
        // Arrange
        InterviewReportModel? report = null;
        var chatHistory = new List<ChatMessage>();

        // Act & Assert
        var exception = await Should.ThrowAsync<NullReferenceException>(
            async () => await _pdfService.GenerateInterviewReportPdfAsync(report!, chatHistory));
        
        exception.ShouldNotBeNull();
    }
}
