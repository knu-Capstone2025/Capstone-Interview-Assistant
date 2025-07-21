using InterviewAssistant.Common.Models;
using InterviewAssistant.Web.Services;

using Microsoft.Extensions.Logging;

using NSubstitute.ExceptionExtensions;

using System.Net;
using System.Text;
using System.Text.Json;

namespace InterviewAssistant.Web.Tests.Services;

[TestFixture]
public class ReportServiceTests
{
    private HttpClient _httpClient;
    private TestableHttpMessageHandler _messageHandler;
    private IReportService _reportService;
    private InterviewReport _testReport;

    [SetUp]
    public void Setup()
    {
        // HttpMessageHandler 대체 객체 생성
        _messageHandler = new TestableHttpMessageHandler();
        _httpClient = new HttpClient(_messageHandler);
        _httpClient.BaseAddress = new Uri("https://localhost:5001/");

        // 테스트할 ReportService 인스턴스 생성
        _reportService = new ReportService(_httpClient);

        // 테스트용 리포트 데이터 준비
        _testReport = new InterviewReport
        {
            CandidateName = "김테스트",
            Position = "개발자",
            InterviewDate = new DateTime(2025, 7, 22),
            FitnessScore = 8,
            QuestionsAndAnswers = new List<QuestionAnswer>
            {
                new QuestionAnswer
                {
                    Question = "**자기소개**를 해주세요.",
                    Answer = "저는 *5년 경력*의 개발자입니다.",
                    Feedback = "**좋은 답변**입니다."
                }
            },
            OverallFeedback = "### 종합 피드백\n\n**강점:**\n- 기술 역량 우수\n\n**개선점:**\n- 리더십 경험 필요",
            Strengths = new List<string> { "**기술적 전문성**", "*문제해결능력*" },
            ImprovementAreas = new List<string> { "**리더십 경험**", "*커뮤니케이션*" },
            FinalAssessment = "**최종 평가:** 우수한 후보자"
        };
    }

    [TearDown]
    public void TearDown()
    {
        _httpClient?.Dispose();
        _messageHandler?.Dispose();
    }

    [Test]
    public async Task GeneratePdfReportAsync_WithValidReport_ShouldReturnPdfBytes()
    {
        // Arrange
        var expectedPdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 }; // %PDF 시그니처
        _messageHandler.SetResponse(HttpStatusCode.OK, expectedPdfBytes);

        // Act
        var result = await _reportService.GeneratePdfReportAsync(_testReport);

        // Assert
        result.ShouldBe(expectedPdfBytes);
        _messageHandler.RequestUri.ShouldBe("/api/report/generate-pdf");
        _messageHandler.Method.ShouldBe(HttpMethod.Post);
    }

    [Test]
    public async Task GeneratePdfReportAsync_WithNullReport_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(
            async () => await _reportService.GeneratePdfReportAsync(null!)
        );
    }

    [Test]
    public async Task GeneratePdfReportAsync_WhenApiReturnsError_ShouldThrowHttpRequestException()
    {
        // Arrange
        _messageHandler.SetResponse(HttpStatusCode.InternalServerError, "서버 오류");

        // Act & Assert
        await Should.ThrowAsync<HttpRequestException>(
            async () => await _reportService.GeneratePdfReportAsync(_testReport)
        );
    }

    [Test]
    public async Task GeneratePdfReportAsync_WithEmptyReport_ShouldCallApi()
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

        var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 };
        _messageHandler.SetResponse(HttpStatusCode.OK, pdfBytes);

        // Act
        var result = await _reportService.GeneratePdfReportAsync(emptyReport);

        // Assert
        result.ShouldNotBeNull();
        _messageHandler.RequestUri.ShouldBe("/api/report/generate-pdf");
    }

    [Test]
    public async Task GeneratePdfReportAsync_WithMarkdownContent_ShouldSendCorrectData()
    {
        // Arrange
        var markdownReport = new InterviewReport
        {
            CandidateName = "마크다운 테스트",
            Position = "**개발자**",
            InterviewDate = DateTime.Now,
            QuestionsAndAnswers = new List<QuestionAnswer>
            {
                new QuestionAnswer
                {
                    Question = "### 기술 질문\n- **경험**이 있나요?",
                    Answer = "**네**, 다음과 같은 경험이 있습니다:\n- 웹 개발 *3년*",
                    Feedback = "*좋은 답변*입니다."
                }
            },
            OverallFeedback = "### 전체 평가\n\n**강점:**\n- 기술 역량",
            Strengths = new List<string> { "**뛰어난 기술력**", "*문제 해결 능력*" },
            ImprovementAreas = new List<string> { "**리더십 스킬**", "*프로젝트 관리*" },
            FitnessScore = 8,
            FinalAssessment = "**최종 결론:** *우수한* 후보자"
        };

        var expectedPdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x34 };
        _messageHandler.SetResponse(HttpStatusCode.OK, expectedPdfBytes);

        // Act
        var result = await _reportService.GeneratePdfReportAsync(markdownReport);

        // Assert
        result.ShouldBe(expectedPdfBytes);
        _messageHandler.RequestContent.ShouldNotBeNull();

        // JSON 옵션 설정 (camelCase 등)
        var jsonOptions = new JsonSerializerOptions 
        { 
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
        };
        var sentData = JsonSerializer.Deserialize<InterviewReport>(_messageHandler.RequestContent, jsonOptions);
        sentData.ShouldNotBeNull();
        sentData.CandidateName.ShouldBe("마크다운 테스트");
        sentData.Position.ShouldBe("**개발자**");
    }

    [Test]
    public async Task GeneratePdfReportAsync_WithLargeContent_ShouldHandleSuccessfully()
    {
        // Arrange
        var largeContent = string.Join("\n", Enumerable.Repeat("**매우 긴** *마크다운* 텍스트 내용입니다.", 100));
        var largeReport = new InterviewReport
        {
            CandidateName = "대용량 테스트",
            Position = "시니어 개발자",
            InterviewDate = DateTime.Now,
            QuestionsAndAnswers = Enumerable.Range(1, 10).Select(i => new QuestionAnswer
            {
                Question = $"**질문 {i}:** {largeContent.Substring(0, Math.Min(200, largeContent.Length))}",
                Answer = $"*답변 {i}:* {largeContent.Substring(0, Math.Min(300, largeContent.Length))}",
                Feedback = $"### 피드백 {i}"
            }).ToList(),
            OverallFeedback = largeContent,
            Strengths = Enumerable.Range(1, 5).Select(i => $"**강점 {i}:** 상세 설명").ToList(),
            ImprovementAreas = Enumerable.Range(1, 3).Select(i => $"*개선점 {i}:* 상세 설명").ToList(),
            FitnessScore = 9,
            FinalAssessment = $"### 최종 평가\n\n상세한 평가 내용"
        };

        var largePdfBytes = new byte[10000];
        _messageHandler.SetResponse(HttpStatusCode.OK, largePdfBytes);

        // Act
        var result = await _reportService.GeneratePdfReportAsync(largeReport);

        // Assert
        result.ShouldBe(largePdfBytes);
        result.Length.ShouldBe(10000);
    }
}

// HttpMessageHandler 테스트용 구현
public class TestableHttpMessageHandler : HttpMessageHandler
{
    public HttpStatusCode ResponseStatusCode { get; set; } = HttpStatusCode.OK;
    public byte[]? ResponseBytes { get; set; }
    public string ResponseContent { get; set; } = "";
    public string? RequestContent { get; private set; }
    public string? RequestUri { get; private set; }
    public HttpMethod? Method { get; private set; }

    public void SetResponse(HttpStatusCode statusCode, byte[] responseBytes)
    {
        ResponseStatusCode = statusCode;
        ResponseBytes = responseBytes;
    }

    public void SetResponse(HttpStatusCode statusCode, string responseContent)
    {
        ResponseStatusCode = statusCode;
        ResponseContent = responseContent;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Method = request.Method;
        RequestUri = request.RequestUri?.PathAndQuery;

        if (request.Content != null)
        {
            RequestContent = await request.Content.ReadAsStringAsync(cancellationToken);
        }

        var response = new HttpResponseMessage(ResponseStatusCode);

        if (ResponseBytes != null)
        {
            response.Content = new ByteArrayContent(ResponseBytes);
        }
        else
        {
            response.Content = new StringContent(ResponseContent, Encoding.UTF8, "application/json");
        }

        return response;
    }
}
