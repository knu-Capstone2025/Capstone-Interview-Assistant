using InterviewAssistant.Common.Models;

namespace InterviewAssistant.ApiService.Services;

public interface IPdfReportService
{
    /// <summary>
    /// 면접 리포트를 PDF로 생성합니다.
    /// </summary>
    /// <param name="report">면접 리포트 데이터</param>
    /// <returns>PDF 바이트 배열</returns>
    byte[] GenerateInterviewReport(InterviewReport report);
}
