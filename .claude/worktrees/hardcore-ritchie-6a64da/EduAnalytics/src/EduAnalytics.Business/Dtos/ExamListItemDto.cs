using EduAnalytics.Core.Enums;

namespace EduAnalytics.Business.Dtos;

/// <summary>
/// Sınav yönetimi listesinde gösterilecek özet sınav kartı.
/// </summary>
public class ExamListItemDto
{
    public int Id { get; set; }
    public string Title { get; set; } = null!;
    public string CourseName { get; set; } = null!;
    public int CourseId { get; set; }
    public DateTime ExamDate { get; set; }
    public int DurationMinutes { get; set; }
    public ExamType ExamType { get; set; }
    public int BookletCount { get; set; }
    public bool ShuffleOptions { get; set; }
    public int TotalQuestions { get; set; }
    public int TotalAnswers { get; set; }
    /// <summary>True ise bu sınava zaten cevap girilmiş — ders/soru değiştirilemez.</summary>
    public bool HasAnswers => TotalAnswers > 0;
}

/// <summary>
/// Mevcut bir sınavın metadatasını (başlık/tarih/süre/tip/kitapçık) güncellemek için.
/// Soru ekleme/çıkarma bu DTO ile yapılmaz; sadece üst düzey alanlar.
/// </summary>
public class ExamUpdateModel
{
    public int Id { get; set; }
    public string Title { get; set; } = null!;
    public DateTime ExamDate { get; set; }
    public int DurationMinutes { get; set; }
    public ExamType ExamType { get; set; }
}
