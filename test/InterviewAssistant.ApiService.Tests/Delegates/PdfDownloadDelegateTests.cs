using InterviewAssistant.ApiService.Delegates;
using InterviewAssistant.ApiService.Services;
using InterviewAssistant.Common.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;

namespace InterviewAssistant.ApiService.Tests.Delegates;

[TestFixture]
public class PdfDownloadDelegateTests
{
    private IPdfGenerationService _pdfService;
    private IServiceProvider _serviceProvider;

    [SetUp]
    public void Setup()
    {
        _pdfService = Substitute.For<IPdfGenerationService>();
        
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IServiceProvider>(sp => sp);
        _serviceProvider = services.BuildServiceProvider();
    }

    [TearDown]
    public void TearDown()
    {
        if (_serviceProvider is IDisposable disposableServiceProvider)
        {
            disposableServiceProvider.Dispose();
        }
    }

    private HttpContext CreateHttpContext()
    {
        return new DefaultHttpContext
        {
            RequestServices = _serviceProvider
        };
    }

    [Test]
    public async Task DownloadInterviewReportPdfAsync_WithValidRequest_ShouldReturnPdfFile()
    {
        // Arrange
        var request = new PdfDownloadRequest
        {
            Report = new InterviewReportModel
            {
                OverallFeedback = "테스트 피드백",
                Strengths = ["강점1", "강점2"],
                Weaknesses = ["개선점1"]
            },
            ChatHistory = new List<ChatMessage>
            {
                new() { Role = MessageRoleType.User, Message = "안녕하세요" }
            }
        };

        var expectedPdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 }; // %PDF
        _pdfService.GenerateInterviewReportPdfAsync(request.Report, request.ChatHistory)
            .Returns(expectedPdfBytes);

        // Act
        var result = await PdfDownloadDelegate.DownloadInterviewReportPdfAsync(request, _pdfService);

        // Assert
        result.ShouldNotBeNull();
        
        var httpContext = CreateHttpContext();
        await result.ExecuteAsync(httpContext);
        
        httpContext.Response.StatusCode.ShouldBe(200);
        httpContext.Response.ContentType.ShouldBe("application/pdf");
    }

    [Test]
    public async Task DownloadInterviewReportPdfAsync_WithEmptyRequest_ShouldStillWork()
    {
        // Arrange
        var request = new PdfDownloadRequest
        {
            Report = new InterviewReportModel(),
            ChatHistory = new List<ChatMessage>()
        };

        var expectedPdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 };
        _pdfService.GenerateInterviewReportPdfAsync(request.Report, request.ChatHistory)
            .Returns(expectedPdfBytes);

        // Act
        var result = await PdfDownloadDelegate.DownloadInterviewReportPdfAsync(request, _pdfService);

        // Assert
        result.ShouldNotBeNull();
        
        var httpContext = CreateHttpContext();
        await result.ExecuteAsync(httpContext);
        
        httpContext.Response.StatusCode.ShouldBe(200);
    }

    [Test]
    public async Task DownloadInterviewReportPdfAsync_WhenServiceThrowsException_ShouldReturnProblem()
    {
        // Arrange
        var request = new PdfDownloadRequest
        {
            Report = new InterviewReportModel(),
            ChatHistory = new List<ChatMessage>()
        };

        _pdfService.GenerateInterviewReportPdfAsync(request.Report, request.ChatHistory)
            .Throws(new InvalidOperationException("PDF 생성 오류"));

        // Act
        var result = await PdfDownloadDelegate.DownloadInterviewReportPdfAsync(request, _pdfService);

        // Assert
        result.ShouldNotBeNull();
        
        var httpContext = CreateHttpContext();
        await result.ExecuteAsync(httpContext);
        
        httpContext.Response.StatusCode.ShouldBe(500);
    }
}
