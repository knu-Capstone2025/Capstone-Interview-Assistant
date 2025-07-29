namespace InterviewAssistant.Common.Models;

/// <summary>
/// 면접 결과 리포트의 데이터 구조
/// </summary>
public class InterviewReportModel
{
  public string OverallFeedback { get; set; } = string.Empty;
  public List<string> Strengths { get; set; } = [];
  public List<string> Weaknesses { get; set; } = [];
  public ChartDataModel ChartData { get; set; } = new();
}

/// <summary>
/// 차트 데이터 구조
/// </summary>
public class ChartDataModel
{
  public List<string> Labels { get; set; } = [];
  public List<int> Values { get; set; } = [];
}