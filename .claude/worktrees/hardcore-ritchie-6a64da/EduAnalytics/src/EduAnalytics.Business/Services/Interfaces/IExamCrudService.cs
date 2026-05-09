using EduAnalytics.Business.Dtos;
using EduAnalytics.Core.Entities;

namespace EduAnalytics.Business.Services.Interfaces;

/// <summary>
/// Sınav/Soru CRUD işlemleri.
/// </summary>
public interface IExamCrudService
{
    Task<List<Course>> GetCoursesAsync();
    Task<List<Course>> GetCoursesByProgramAsync(int programId);
    Task<List<Topic>> GetTopicsForCourseAsync(int courseId);
    Task<List<Student>> GetStudentsAsync();

    /// <summary>
    /// Yeni sorularla yeni sınav oluşturur. Sorular soru bankasına eklenir + ExamQuestion ile sınava bağlanır.
    /// </summary>
    Task<int> CreateExamAsync(ExamCreateModel model);

    /// <summary>
    /// Soru bankasından seçilmiş sorularla yeni sınav oluşturur. Yeni soru üretmez, sadece bağlar.
    /// </summary>
    Task<int> CreateExamFromBankAsync(ExamFromBankCreateModel model);

    /// <summary>
    /// Sistemdeki ilk (default) öğretmen kullanıcısının Id'si.
    /// İleride login sistemi eklendiğinde oturumdaki kullanıcıdan alınacak.
    /// </summary>
    Task<int> GetDefaultUserIdAsync();

    // ─── Sınav yönetimi (FAZ 7) ───

    /// <summary>Tüm sınavları listeleme görünümünde döner (yönetim ekranı için).</summary>
    Task<List<ExamListItemDto>> GetAllExamsAsync();

    /// <summary>Tek bir sınavın temel alanlarını döner (düzenleme formuna doldurmak için).</summary>
    Task<ExamUpdateModel?> GetExamForEditAsync(int examId);

    /// <summary>
    /// Sınavın üst düzey alanlarını günceller: başlık, tarih, süre, tip, kitapçık ayarı.
    /// Soruları/öğrencileri etkilemez.
    /// </summary>
    Task UpdateExamAsync(ExamUpdateModel model);

    /// <summary>
    /// Sınavı tamamen siler (cascade ile sorular, kitapçıklar, cevaplar, kriter puanları).
    /// </summary>
    Task DeleteExamAsync(int examId);
}
