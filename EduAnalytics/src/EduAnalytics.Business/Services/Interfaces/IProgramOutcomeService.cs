using EduAnalytics.Business.Dtos;
using EduAnalytics.Core.Entities;

namespace EduAnalytics.Business.Services.Interfaces;

/// <summary>
/// Program Çıktısı (PÇ) yönetimi.
/// Hiyerarşi: Program → ProgramOutcome → LearningOutcome → Question.
/// </summary>
public interface IProgramOutcomeService
{
    Task<List<Program>> GetProgramsAsync();
    Task<List<ProgramOutcomeDto>> GetByProgramAsync(int programId);

    Task<int> CreateAsync(ProgramOutcomeCreateModel model);
    Task UpdateAsync(int programOutcomeId, ProgramOutcomeCreateModel model);
    Task DeleteAsync(int programOutcomeId);

    /// <summary>
    /// Program çıktısının başarı raporunu üretir:
    /// PÇ → Bağlı ÖÇ'ler → Soru sayısı → Tüm sınavlardaki ortalama başarı.
    /// </summary>
    Task<List<ProgramOutcomeReportDto>> GenerateReportAsync(int programId);

    Task LinkToLearningOutcomeAsync(int programOutcomeId, int learningOutcomeId, int contributionLevel);
    Task UnlinkFromLearningOutcomeAsync(int programOutcomeId, int learningOutcomeId);
}
