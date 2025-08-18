using InterviewAssistant.Common.Models;
using InterviewAssistant.ApiService.Services;
using Microsoft.AspNetCore.Mvc;

namespace InterviewAssistant.ApiService.Delegates;

/// <summary>
/// PDF 다운로드를 처리하는 Delegate
/// </summary>
public static class PdfDownloadDelegate
{
    /// <summary>
    /// 면접 결과 리포트를 PDF로 다운로드합니다.
    /// </summary>
    /// <param name="request">PDF 생성 요청 데이터</param>
    /// <param name="pdfService">PDF 생성 서비스</param>
    /// <returns>PDF 파일</returns>
    public static async Task<IResult> DownloadInterviewReportPdfAsync(
        [FromBody] PdfDownloadRequest request,
        IPdfGenerationService pdfService)
    {
        try
        {
            var pdfBytes = await pdfService.GenerateInterviewReportPdfAsync(request.Report, request.ChatHistory);
            
            var fileName = $"면접결과_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
            
            return Results.File(pdfBytes, "application/pdf", fileName);
        }
        catch (Exception ex)
        {
            return Results.Problem($"PDF 생성 중 오류가 발생했습니다: {ex.Message}");
        }
    }
}
