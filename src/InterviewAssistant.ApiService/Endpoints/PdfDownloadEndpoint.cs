using InterviewAssistant.ApiService.Delegates;
using InterviewAssistant.Common.Models;

namespace InterviewAssistant.ApiService.Endpoints;

/// <summary>
/// PDF 다운로드 엔드포인트
/// </summary>
public static class PdfDownloadEndpoint
{
    /// <summary>
    /// PDF 다운로드 엔드포인트를 등록합니다.
    /// </summary>
    /// <param name="app">웹 애플리케이션</param>
    /// <returns>라우트 그룹 빌더</returns>
    public static RouteGroupBuilder MapPdfDownloadEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/pdf");

        group.MapPost("/download-report", PdfDownloadDelegate.DownloadInterviewReportPdfAsync)
            .WithName("DownloadInterviewReportPdf")
            .WithDisplayName("Download Interview Report PDF")
            .WithDescription("면접 결과 리포트를 PDF로 다운로드합니다.")
            .WithOpenApi()
            .Produces<byte[]>(200, "application/pdf")
            .ProducesValidationProblem()
            .Accepts<PdfDownloadRequest>("application/json");

        return group;
    }
}
