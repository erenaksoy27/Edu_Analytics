namespace EduAnalytics.Business.Dtos;

/// <summary>
/// Bir konunun (Bologna çıktısı) sınıf genelinde ne kadar öğrenildiğini gösterir.
/// </summary>
public class TopicPerformanceDto
{
    public int TopicId { get; set; }
    public int WeekNumber { get; set; }
    public string TopicTitle { get; set; } = null!;
    public string? LearningOutcome { get; set; }

    public int RelatedQuestionCount { get; set; }     // Bu konuyu ölçen soru sayısı
    public int TotalAnswers { get; set; }             // Öğrenci_sayısı × ilgili_soru_sayısı
    public int CorrectAnswers { get; set; }
    public double SuccessRate { get; set; }           // 0-100

    /// <summary>
    /// Zayıflık seviyesi: 0-40 arası KRİTİK, 40-60 ZAYIF, 60-80 ORTA, 80+ İYİ.
    /// </summary>
    public string PerformanceLevel => SuccessRate switch
    {
        < 40 => "KRİTİK",
        < 60 => "ZAYIF",
        < 80 => "ORTA",
        _ => "İYİ"
    };

    /// <summary>
    /// Konu güçlük indeksi p = SuccessRate / 100  (0..1).
    /// 1 = çok kolay, 0 = çok zor.
    /// </summary>
    public double DifficultyIndex => Math.Round(SuccessRate / 100.0, 3);

    /// <summary>
    /// Konu güçlük kategorisi (madde analizinden sözlü karşılığa):
    /// p &lt; 0.20 → Çok Zor, &lt; 0.40 → Zor, &lt; 0.60 → Orta,
    /// &lt; 0.80 → Kolay, ≥ 0.80 → Çok Kolay.
    /// </summary>
    public string DifficultyCategory => DifficultyIndex switch
    {
        < 0.20 => "Çok Zor",
        < 0.40 => "Zor",
        < 0.60 => "Orta",
        < 0.80 => "Kolay",
        _ => "Çok Kolay"
    };
}
