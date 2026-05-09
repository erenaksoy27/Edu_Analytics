namespace EduAnalytics.Business.Dtos;

/// <summary>
/// Sınavda soruların konu/ÖÇ/hafta dağılımının analizi.
/// "Hocam 3. ve 5. haftaya yığılmışsın, 8. haftadan hiç sormamışsın" tespiti.
/// </summary>
public class ExamBalanceReportDto
{
    public int ExamId { get; set; }
    public string ExamTitle { get; set; } = null!;
    public int TotalQuestions { get; set; }

    public List<TopicCoverageDto> TopicCoverage { get; set; } = new();
    public List<LearningOutcomeCoverageDto> LearningOutcomeCoverage { get; set; } = new();

    /// <summary>Üretilen uyarı listesi (boş ise sınav dengelenmiş demektir).</summary>
    public List<BalanceWarningDto> Warnings { get; set; } = new();

    /// <summary>0-100 arası dengelilik skoru. 100 = mükemmel, 0 = aşırı dengesiz.</summary>
    public double BalanceScore { get; set; }

    /// <summary>Gini-katsayısı benzeri eşitsizlik göstergesi (0 = tam eşit, 1 = aşırı eşitsiz).</summary>
    public double DistributionInequality { get; set; }
}

public class TopicCoverageDto
{
    public int TopicId { get; set; }
    public int WeekNumber { get; set; }
    public string TopicTitle { get; set; } = null!;
    public int QuestionCount { get; set; }
    public double Percentage { get; set; }
}

public class LearningOutcomeCoverageDto
{
    public int LearningOutcomeId { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public int QuestionCount { get; set; }
    public double Percentage { get; set; }
}

public class BalanceWarningDto
{
    /// <summary>"Concentration" | "MissingTopic" | "MissingOutcome" | "TypeImbalance"</summary>
    public string WarningType { get; set; } = null!;
    /// <summary>"Info" | "Warning" | "Critical"</summary>
    public string Severity { get; set; } = "Warning";
    public string Message { get; set; } = null!;
}
