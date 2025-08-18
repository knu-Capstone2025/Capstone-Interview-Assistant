namespace InterviewAssistant.Common.Models;

/// <summary>
/// PDF 다운로드 요청 모델
/// </summary>
public class PdfDownloadRequest
{
    /// <summary>
    /// 면접 결과 리포트
    /// </summary>
    public InterviewReportModel Report { get; set; } = new();

    /// <summary>
    /// 채팅 기록
    /// </summary>
    public List<ChatMessage> ChatHistory { get; set; } = new();
}
