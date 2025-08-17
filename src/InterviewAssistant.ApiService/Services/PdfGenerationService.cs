using InterviewAssistant.Common.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Markdig;
using System.Text.RegularExpressions;

namespace InterviewAssistant.ApiService.Services;

/// <summary>
/// PDF 생성 서비스 인터페이스
/// </summary>
public interface IPdfGenerationService
{
    /// <summary>
    /// 면접 결과 리포트를 PDF로 생성합니다.
    /// </summary>
    /// <param name="report">면접 결과 리포트</param>
    /// <param name="chatHistory">채팅 기록</param>
    /// <returns>PDF 바이트 배열</returns>
    Task<byte[]> GenerateInterviewReportPdfAsync(InterviewReportModel report, List<ChatMessage> chatHistory);
}

/// <summary>
/// PDF 생성 서비스 구현
/// </summary>
public class PdfGenerationService : IPdfGenerationService
{
    public async Task<byte[]> GenerateInterviewReportPdfAsync(InterviewReportModel report, List<ChatMessage> chatHistory)
    {
        // QuestPDF 라이선스 설정 (Community 라이선스)
        QuestPDF.Settings.License = LicenseType.Community;

        return await Task.FromResult(Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Header()
                    .Text("면접 결과 리포트")
                    .SemiBold().FontSize(20).FontColor(Colors.Blue.Medium);

                page.Content()
                    .PaddingVertical(1, Unit.Centimetre)
                    .Column(column =>
                    {
                        column.Spacing(20);

                        // 종합 분석 (마크다운 처리)
                        column.Item().Text("종합 분석").SemiBold().FontSize(16);
                        var overallFeedbackHtml = ConvertMarkdownToPlainText(report.OverallFeedback ?? "분석 결과가 없습니다.");
                        column.Item().PaddingLeft(10).Text(overallFeedbackHtml);

                        // 강점 (마크다운 처리)
                        column.Item().Text("강점").SemiBold().FontSize(16);
                        if (report.Strengths?.Any() == true)
                        {
                            foreach (var strength in report.Strengths)
                            {
                                var strengthText = ConvertMarkdownToPlainText(strength);
                                column.Item().PaddingLeft(10).Text($"• {strengthText}");
                            }
                        }
                        else
                        {
                            column.Item().PaddingLeft(10).Text("• 강점이 기록되지 않았습니다.");
                        }

                        // 개선점 (마크다운 처리)
                        column.Item().Text("개선점").SemiBold().FontSize(16);
                        if (report.Weaknesses?.Any() == true)
                        {
                            foreach (var weakness in report.Weaknesses)
                            {
                                var weaknessText = ConvertMarkdownToPlainText(weakness);
                                column.Item().PaddingLeft(10).Text($"• {weaknessText}");
                            }
                        }
                        else
                        {
                            column.Item().PaddingLeft(10).Text("• 개선점이 기록되지 않았습니다.");
                        }

                        // 질문 유형 분석 (원 그래프 형태로 표시)
                        if (report.ChartData?.Labels?.Any() == true && report.ChartData?.Values?.Any() == true)
                        {
                            column.Item().Text("질문 유형 분석").SemiBold().FontSize(16);
                            
                            // 시각적 차트 표현
                            column.Item().PaddingLeft(10).Column(chartColumn =>
                            {
                                var total = report.ChartData.Values.Sum();
                                if (total > 0)
                                {
                                    chartColumn.Item().Text("📊 질문 유형별 분포").FontSize(12).SemiBold();
                                    chartColumn.Item().PaddingVertical(5);

                                    for (int i = 0; i < Math.Min(report.ChartData.Labels.Count, report.ChartData.Values.Count); i++)
                                    {
                                        var percentage = CalculatePercentage(report.ChartData.Values[i], total);
                                        var barLength = (int)(percentage / 5); // 5%당 하나의 블록
                                        var bar = new string('█', Math.Max(1, barLength));
                                        
                                        chartColumn.Item().Row(row =>
                                        {
                                            row.ConstantItem(80).Text($"{report.ChartData.Labels[i]}:");
                                            row.ConstantItem(150).Text(bar).FontColor(GetChartColor(i));
                                            row.RelativeItem().Text($"{report.ChartData.Values[i]}개 ({percentage:F1}%)");
                                        });
                                    }
                                }
                            });
                        }

                        // 대화 기록 (마크다운 처리)
                        if (chatHistory?.Any() == true)
                        {
                            column.Item().Text("대화 기록").SemiBold().FontSize(16);
                            
                            foreach (var message in chatHistory)
                            {
                                var roleText = message.Role switch
                                {
                                    MessageRoleType.User => "지원자",
                                    MessageRoleType.Assistant => "면접관",
                                    _ => message.Role.ToString()
                                };

                                var messageText = ConvertMarkdownToPlainText(message.Message ?? "");

                                column.Item().PaddingLeft(10).Column(messageColumn =>
                                {
                                    messageColumn.Item().Text($"[{roleText}]").SemiBold().FontColor(
                                        message.Role == MessageRoleType.User ? Colors.Blue.Medium : Colors.Green.Medium);
                                    messageColumn.Item().PaddingLeft(10).Text(messageText);
                                    messageColumn.Item().PaddingVertical(5);
                                });
                            }
                        }
                    });

                page.Footer()
                    .AlignCenter()
                    .Text(x =>
                    {
                        x.Span("생성일: ");
                        x.Span(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")).SemiBold();
                    });
            });
        }).GeneratePdf());
    }

    /// <summary>
    /// 마크다운을 일반 텍스트로 변환합니다.
    /// </summary>
    private static string ConvertMarkdownToPlainText(string markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
            return string.Empty;

        // 마크다운을 HTML로 변환
        var html = Markdown.ToHtml(markdown);
        
        // HTML 태그 제거하여 일반 텍스트로 변환
        var plainText = Regex.Replace(html, "<.*?>", string.Empty);
        
        // HTML 엔티티 디코딩
        plainText = System.Net.WebUtility.HtmlDecode(plainText);
        
        return plainText.Trim();
    }

    /// <summary>
    /// 차트 색상을 반환합니다.
    /// </summary>
    private static string GetChartColor(int index)
    {
        var colors = new[] { Colors.Blue.Medium, Colors.Green.Medium, Colors.Orange.Medium, Colors.Red.Medium, Colors.Purple.Medium };
        return colors[index % colors.Length];
    }

    /// <summary>
    /// 퍼센티지를 계산합니다.
    /// </summary>
    private static double CalculatePercentage(int value, int total)
    {
        if (total == 0) return 0;
        return (double)value / total * 100;
    }
}
