using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using InterviewAssistant.Common.Models;
using System.Text.RegularExpressions;

namespace InterviewAssistant.ApiService.Services;

public class PdfReportService : IPdfReportService
{
    static PdfReportService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] GenerateInterviewReport(InterviewReport report)
    {
        // Null 체크 및 기본값 설정
        if (report == null)
        {
            throw new ArgumentNullException(nameof(report));
        }

        // 기본값 설정
        report.CandidateName ??= "알 수 없음";
        report.Position ??= "알 수 없음";
        report.QuestionsAndAnswers ??= new List<QuestionAnswer>();
        report.OverallFeedback ??= "";
        report.Strengths ??= new List<string>();
        report.ImprovementAreas ??= new List<string>();
        report.FinalAssessment ??= "";

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(11).FontFamily(Fonts.Arial));

                page.Header()
                    .Text("AI 면접 리포트")
                    .SemiBold().FontSize(20).FontColor(Colors.Blue.Medium);

                page.Content()
                    .PaddingVertical(1, Unit.Centimetre)
                    .Column(x =>
                    {
                        x.Spacing(15);

                        // 기본 정보
                        x.Item().Element(ComposeBasicInfo);

                        // 질문과 답변
                        x.Item().Element(ComposeQuestionsAndAnswers);

                        // 종합 피드백
                        x.Item().Element(ComposeOverallFeedback);

                        // 강점과 개선점
                        x.Item().Element(ComposeStrengthsAndImprovements);

                        // 최종 평가
                        x.Item().Element(ComposeFinalAssessment);
                    });

                page.Footer()
                    .AlignCenter()
                    .Text(x =>
                    {
                        x.Span("생성일: ");
                        x.Span($"{DateTime.Now:yyyy년 MM월 dd일}").SemiBold();
                    });
            });
        }).GeneratePdf();

        void ComposeBasicInfo(IContainer container)
        {
            container.Background(Colors.Grey.Lighten3).Padding(15).Column(column =>
            {
                column.Item().Text("기본 정보").FontSize(16).SemiBold().FontColor(Colors.Blue.Darken2);
                column.Item().PaddingTop(10).Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text($"지원자: {report.CandidateName}").FontSize(12);
                        col.Item().Text($"지원 직무: {report.Position}").FontSize(12);
                    });
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text($"면접일: {report.InterviewDate:yyyy년 MM월 dd일}").FontSize(12);
                        col.Item().Text($"적합성 점수: {report.FitnessScore}/10").FontSize(12).SemiBold();
                    });
                });
            });
        }

        void ComposeQuestionsAndAnswers(IContainer container)
        {
            container.Column(column =>
            {
                column.Item().Text("질문 및 답변").FontSize(16).SemiBold().FontColor(Colors.Blue.Darken2);
                
                if (report.QuestionsAndAnswers != null && report.QuestionsAndAnswers.Count > 0)
                {
                    for (int i = 0; i < report.QuestionsAndAnswers.Count; i++)
                    {
                        var qa = report.QuestionsAndAnswers[i];
                        if (qa != null)
                        {
                            column.Item().PaddingTop(15).Column(qaColumn =>
                            {
                                qaColumn.Item().Text($"질문 {i + 1}").FontSize(14).SemiBold();
                                qaColumn.Item().PaddingTop(5).Element(c => RenderMarkdownText(c, qa.Question ?? "", 12));
                                
                                qaColumn.Item().PaddingTop(10).Text("답변").FontSize(12).SemiBold().FontColor(Colors.Green.Darken1);
                                qaColumn.Item().PaddingTop(5).Element(c => RenderMarkdownText(c, qa.Answer ?? "", 11));
                                
                                if (!string.IsNullOrEmpty(qa.Feedback))
                                {
                                    qaColumn.Item().PaddingTop(10).Text("피드백").FontSize(12).SemiBold().FontColor(Colors.Orange.Darken1);
                                    qaColumn.Item().PaddingTop(5).Element(c => RenderMarkdownText(c, qa.Feedback, 11));
                                }
                            });
                        }
                    }
                }
                else
                {
                    column.Item().PaddingTop(10).Text("질문과 답변이 없습니다.").FontSize(12).Italic();
                }
            });
        }

        void ComposeOverallFeedback(IContainer container)
        {
            container.Column(column =>
            {
                column.Item().Text("종합 피드백").FontSize(16).SemiBold().FontColor(Colors.Blue.Darken2);
                column.Item().PaddingTop(10).Element(c => RenderMarkdownText(c, report.OverallFeedback, 12));
            });
        }

        void ComposeStrengthsAndImprovements(IContainer container)
        {
            container.Row(row =>
            {
                row.RelativeItem().Column(column =>
                {
                    column.Item().Text("주요 강점").FontSize(14).SemiBold().FontColor(Colors.Green.Darken2);
                    if (report.Strengths != null && report.Strengths.Count > 0)
                    {
                        foreach (var strength in report.Strengths)
                        {
                            if (!string.IsNullOrEmpty(strength))
                            {
                                column.Item().PaddingTop(5).Row(strengthRow =>
                                {
                                    strengthRow.ConstantItem(15).Text("•").FontColor(Colors.Green.Medium);
                                    strengthRow.RelativeItem().Element(c => RenderMarkdownText(c, strength, 11));
                                });
                            }
                        }
                    }
                    else
                    {
                        column.Item().PaddingTop(5).Text("강점이 기록되지 않았습니다.").FontSize(11).Italic();
                    }
                });

                row.ConstantItem(20);

                row.RelativeItem().Column(column =>
                {
                    column.Item().Text("개선 영역").FontSize(14).SemiBold().FontColor(Colors.Orange.Darken2);
                    if (report.ImprovementAreas != null && report.ImprovementAreas.Count > 0)
                    {
                        foreach (var improvement in report.ImprovementAreas)
                        {
                            if (!string.IsNullOrEmpty(improvement))
                            {
                                column.Item().PaddingTop(5).Row(improvementRow =>
                                {
                                    improvementRow.ConstantItem(15).Text("•").FontColor(Colors.Orange.Medium);
                                    improvementRow.RelativeItem().Element(c => RenderMarkdownText(c, improvement, 11));
                                });
                            }
                        }
                    }
                    else
                    {
                        column.Item().PaddingTop(5).Text("개선 영역이 기록되지 않았습니다.").FontSize(11).Italic();
                    }
                });
            });
        }

        void ComposeFinalAssessment(IContainer container)
        {
            container.Background(Colors.Blue.Lighten4).Padding(15).Column(column =>
            {
                column.Item().Text("최종 평가").FontSize(16).SemiBold().FontColor(Colors.Blue.Darken2);
                column.Item().PaddingTop(10).Element(c => RenderMarkdownText(c, report.FinalAssessment, 12));
            });
        }
    }

    // 마크다운 텍스트를 QuestPDF 서식으로 렌더링하는 헬퍼 메서드
    private static void RenderMarkdownText(IContainer container, string markdownText, int fontSize)
    {
        if (string.IsNullOrEmpty(markdownText))
        {
            container.Text("").FontSize(fontSize);
            return;
        }

        // 마크다운을 파싱하여 구조화된 텍스트로 변환
        var parsedContent = ParseMarkdown(markdownText);
        
        container.Column(column =>
        {
            foreach (var element in parsedContent)
            {
                switch (element.Type)
                {
                    case MarkdownElementType.Text:
                        if (element.IsBold)
                            column.Item().Text(element.Content).FontSize(fontSize).SemiBold();
                        else
                            column.Item().Text(element.Content).FontSize(fontSize);
                        break;
                        
                    case MarkdownElementType.ListItem:
                        column.Item().PaddingTop(3).Row(row =>
                        {
                            row.ConstantItem(15).Text("•").FontSize(fontSize);
                            if (element.IsBold)
                                row.RelativeItem().Text(element.Content).FontSize(fontSize).SemiBold();
                            else
                                row.RelativeItem().Text(element.Content).FontSize(fontSize);
                        });
                        break;
                        
                    case MarkdownElementType.Header:
                        column.Item().PaddingTop(10).Text(element.Content)
                            .FontSize(fontSize + 2).SemiBold();
                        break;
                        
                    case MarkdownElementType.Paragraph:
                        if (element.IsBold)
                            column.Item().PaddingTop(5).Text(element.Content).FontSize(fontSize).SemiBold();
                        else
                            column.Item().PaddingTop(5).Text(element.Content).FontSize(fontSize);
                        break;
                }
            }
        });
    }

    // 마크다운 파싱 메서드
    private static List<MarkdownElement> ParseMarkdown(string markdown)
    {
        var elements = new List<MarkdownElement>();
        var lines = markdown.Split('\n', StringSplitOptions.None);
        
        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();
            
            if (string.IsNullOrEmpty(trimmedLine))
                continue;
                
            // 헤더 처리 (### 헤더)
            if (trimmedLine.StartsWith("###"))
            {
                var headerText = trimmedLine.Substring(3).Trim();
                elements.Add(new MarkdownElement 
                { 
                    Type = MarkdownElementType.Header, 
                    Content = CleanText(headerText) 
                });
            }
            // 리스트 아이템 처리 (- 또는 * 시작)
            else if (trimmedLine.StartsWith("- ") || trimmedLine.StartsWith("* "))
            {
                var listText = trimmedLine.Substring(2).Trim();
                var (content, isBold, isItalic) = ProcessInlineFormatting(listText);
                elements.Add(new MarkdownElement 
                { 
                    Type = MarkdownElementType.ListItem, 
                    Content = content,
                    IsBold = isBold,
                    IsItalic = isItalic
                });
            }
            // 일반 텍스트 처리
            else
            {
                var (content, isBold, isItalic) = ProcessInlineFormatting(trimmedLine);
                elements.Add(new MarkdownElement 
                { 
                    Type = MarkdownElementType.Paragraph, 
                    Content = content,
                    IsBold = isBold,
                    IsItalic = isItalic
                });
            }
        }
        
        return elements;
    }

    // 인라인 서식 처리 (볼드, 이탤릭)
    private static (string content, bool isBold, bool isItalic) ProcessInlineFormatting(string text)
    {
        var content = text;
        var isBold = false;
        var isItalic = false;
        
        // 볼드 처리 (**text** -> text)
        if (content.Contains("**"))
        {
            isBold = true;
            content = Regex.Replace(content, @"\*\*(.*?)\*\*", "$1");
        }
        
        // 이탤릭 처리 (*text* -> text)
        if (content.Contains("*") && !content.Contains("**"))
        {
            isItalic = true;
            content = Regex.Replace(content, @"\*(.*?)\*", "$1");
        }
        
        return (CleanText(content), isBold, isItalic);
    }

    // 텍스트 정리 (불필요한 마크다운 제거)
    private static string CleanText(string text)
    {
        return text
            .Replace("**", "")
            .Replace("*", "")
            .Replace("#", "")
            .Trim();
    }

    // 마크다운 요소 클래스
    private class MarkdownElement
    {
        public MarkdownElementType Type { get; set; }
        public string Content { get; set; } = string.Empty;
        public bool IsBold { get; set; }
        public bool IsItalic { get; set; }
    }

    // 마크다운 요소 타입 열거형
    private enum MarkdownElementType
    {
        Text,
        Paragraph,
        Header,
        ListItem
    }
}
