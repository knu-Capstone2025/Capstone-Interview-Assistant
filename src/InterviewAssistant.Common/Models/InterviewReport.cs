namespace InterviewAssistant.Common.Models;

/// <summary>
/// 면접 리포트 모델
/// </summary>
public class InterviewReport
{
    /// <summary>
    /// 면접 세션 ID
    /// </summary>
    public Guid SessionId { get; set; } = Guid.NewGuid();

    /// <summary>
    /// 면접 날짜
    /// </summary>
    public DateTime InterviewDate { get; set; } = DateTime.Now;

    /// <summary>
    /// 지원자 이름 (이력서에서 추출)
    /// </summary>
    public string CandidateName { get; set; } = string.Empty;

    /// <summary>
    /// 지원 직무
    /// </summary>
    public string Position { get; set; } = string.Empty;

    /// <summary>
    /// 면접 질문과 답변 목록
    /// </summary>
    public List<QuestionAnswer> QuestionsAndAnswers { get; set; } = new();

    /// <summary>
    /// 종합 피드백
    /// </summary>
    public string OverallFeedback { get; set; } = string.Empty;

    /// <summary>
    /// 주요 강점
    /// </summary>
    public List<string> Strengths { get; set; } = new();

    /// <summary>
    /// 개선 영역
    /// </summary>
    public List<string> ImprovementAreas { get; set; } = new();

    /// <summary>
    /// 직무 적합성 점수 (1-10)
    /// </summary>
    public int FitnessScore { get; set; }

    /// <summary>
    /// 종합 평가
    /// </summary>
    public string FinalAssessment { get; set; } = string.Empty;
}

/// <summary>
/// 질문과 답변 쌍
/// </summary>
public class QuestionAnswer
{
    /// <summary>
    /// 질문
    /// </summary>
    public string Question { get; set; } = string.Empty;

    /// <summary>
    /// 답변
    /// </summary>
    public string Answer { get; set; } = string.Empty;

    /// <summary>
    /// 개별 피드백
    /// </summary>
    public string Feedback { get; set; } = string.Empty;
}
