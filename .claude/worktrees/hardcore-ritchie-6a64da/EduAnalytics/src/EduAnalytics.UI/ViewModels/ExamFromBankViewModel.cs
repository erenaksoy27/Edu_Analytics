using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EduAnalytics.Business.Dtos;
using EduAnalytics.Business.Services.Interfaces;
using EduAnalytics.Core.Entities;
using EduAnalytics.Core.Enums;

namespace EduAnalytics.UI.ViewModels;

/// <summary>
/// Soru bankasından seçerek sınav oluşturma akışı.
/// Yeni soru üretmez; sadece havuzdan seçilen soruları sınava bağlar.
/// </summary>
public partial class ExamFromBankViewModel : ObservableObject
{
    private readonly IExamCrudService _examService;
    private readonly IQuestionBankService _bankService;
    private readonly ILearningOutcomeService _loService;
    private readonly IExamBalanceCheckService _balanceService;
    private readonly SemaphoreSlim _opLock = new(1, 1);

    public event Action? ExamSaved;

    [ObservableProperty] private ObservableCollection<Course> _courses = new();
    [ObservableProperty] private Course? _selectedCourse;

    // Sınav alanları
    [ObservableProperty] private string _title = string.Empty;
    [ObservableProperty] private DateTime _examDate = DateTime.Today;
    [ObservableProperty] private int _durationMinutes = 60;
    [ObservableProperty] private ExamType _examType = ExamType.Midterm;
    [ObservableProperty] private int _bookletCount = 1;
    [ObservableProperty] private bool _shuffleOptions = false;
    [ObservableProperty] private int? _cutoffWeek = 7;

    // Soru havuzu (sol kolon)
    [ObservableProperty] private ObservableCollection<QuestionBankItemDto> _bankPool = new();
    [ObservableProperty] private string _bankSearchText = string.Empty;
    [ObservableProperty] private ObservableCollection<LearningOutcomeDto> _availableOutcomes = new();

    // Seçili sorular (sağ kolon)
    [ObservableProperty] private ObservableCollection<QuestionBankItemDto> _selectedQuestions = new();

    // Denge önizlemesi
    [ObservableProperty] private ExamBalanceReportDto? _balancePreview;

    [ObservableProperty] private bool _isSaving;
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private string? _successMessage;

    public ExamFromBankViewModel(
        IExamCrudService examService,
        IQuestionBankService bankService,
        ILearningOutcomeService loService,
        IExamBalanceCheckService balanceService)
    {
        _examService = examService;
        _bankService = bankService;
        _loService = loService;
        _balanceService = balanceService;
    }

    public async Task LoadAsync()
    {
        ErrorMessage = null;
        SuccessMessage = null;
        try
        {
            var courses = await _examService.GetCoursesAsync();
            Courses = new ObservableCollection<Course>(courses);
            if (SelectedCourse == null && courses.Count > 0)
                SelectedCourse = courses[0];
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Yükleme hatası: {ex.Message}";
        }
    }

    partial void OnSelectedCourseChanged(Course? value)
    {
        SelectedQuestions.Clear();
        _ = ReloadAfterCourseChangeAsync();
    }

    /// <summary>Sıralı async — DbContext'e paralel sorgu gitmeyecek şekilde.</summary>
    private async Task ReloadAfterCourseChangeAsync()
    {
        await ReloadPoolAsync();
        await ReloadOutcomesAsync();
    }

    partial void OnExamTypeChanged(ExamType value)
    {
        _ = ReloadOutcomesAsync();
    }

    private async Task ReloadOutcomesAsync()
    {
        if (SelectedCourse == null) return;
        await _opLock.WaitAsync();
        try
        {
            var list = await _loService.GetAvailableForExamAsync(SelectedCourse.Id, ExamType, CutoffWeek);
            AvailableOutcomes = new ObservableCollection<LearningOutcomeDto>(list);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"ÖÇ listesi yüklenemedi: {ex.Message}";
        }
        finally
        {
            _opLock.Release();
        }
    }

    [RelayCommand]
    private async Task SearchPoolAsync()
    {
        if (SelectedCourse == null) return;
        await _opLock.WaitAsync();
        try
        {
            var filter = new QuestionBankFilter
            {
                CourseId = SelectedCourse.Id,
                IsActive = true,
                SearchText = string.IsNullOrWhiteSpace(BankSearchText) ? null : BankSearchText
            };
            var result = await _bankService.SearchAsync(filter);

            // Zaten seçilenleri havuzdan filtrele
            var selectedIds = SelectedQuestions.Select(q => q.Id).ToHashSet();
            BankPool = new ObservableCollection<QuestionBankItemDto>(
                result.Where(q => !selectedIds.Contains(q.Id)));
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Havuz aranamadı: {ex.Message}";
        }
        finally
        {
            _opLock.Release();
        }
    }

    private Task ReloadPoolAsync() => SearchPoolAsync();

    [RelayCommand]
    private async Task AddToExamAsync(QuestionBankItemDto? q)
    {
        if (q == null) return;
        SelectedQuestions.Add(q);
        BankPool.Remove(q);
        await UpdateBalancePreviewAsync();
    }

    [RelayCommand]
    private async Task RemoveFromExamAsync(QuestionBankItemDto? q)
    {
        if (q == null) return;
        SelectedQuestions.Remove(q);
        BankPool.Add(q);
        await UpdateBalancePreviewAsync();
    }

    [RelayCommand]
    private void MoveUp(QuestionBankItemDto? q)
    {
        if (q == null) return;
        var idx = SelectedQuestions.IndexOf(q);
        if (idx > 0) SelectedQuestions.Move(idx, idx - 1);
    }

    [RelayCommand]
    private void MoveDown(QuestionBankItemDto? q)
    {
        if (q == null) return;
        var idx = SelectedQuestions.IndexOf(q);
        if (idx >= 0 && idx < SelectedQuestions.Count - 1)
            SelectedQuestions.Move(idx, idx + 1);
    }

    /// <summary>
    /// Tüm seçili sorulara 100/N puan paylaştırır. Save'de bu değer
    /// OverrideMaxPoints olarak kaydedilir; orijinal soru bankasındaki
    /// MaxPoints korunur.
    /// </summary>
    [RelayCommand]
    private void DistributePointsTo100()
    {
        if (SelectedQuestions.Count == 0)
        {
            ErrorMessage = "Puan dağıtımı için önce soru seçmelisiniz.";
            return;
        }

        decimal pointsPerQuestion = Math.Round(100m / SelectedQuestions.Count, 2);

        // QuestionBankItemDto INPC desteklemediği için koleksiyonu yeniden oluştur.
        var snapshot = SelectedQuestions.ToList();
        foreach (var q in snapshot)
            q.MaxPoints = pointsPerQuestion;
        SelectedQuestions = new ObservableCollection<QuestionBankItemDto>(snapshot);

        SuccessMessage = $"✓ {snapshot.Count} soruya {pointsPerQuestion} puan paylaştırıldı.";
        ErrorMessage = null;
    }

    private async Task UpdateBalancePreviewAsync()
    {
        if (SelectedCourse == null || SelectedQuestions.Count == 0)
        {
            BalancePreview = null;
            return;
        }
        try
        {
            BalancePreview = await _balanceService.AnalyzeDraftAsync(
                SelectedCourse.Id,
                SelectedQuestions.Select(q => q.Id).ToList());
        }
        catch
        {
            BalancePreview = null;
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        ErrorMessage = null;
        SuccessMessage = null;

        if (SelectedCourse == null) { ErrorMessage = "Ders seçmelisiniz."; return; }
        if (string.IsNullOrWhiteSpace(Title)) { ErrorMessage = "Sınav başlığı boş olamaz."; return; }
        if (SelectedQuestions.Count == 0) { ErrorMessage = "En az bir soru seçilmeli."; return; }

        IsSaving = true;
        try
        {
            var userId = await _examService.GetDefaultUserIdAsync();
            var model = new ExamFromBankCreateModel
            {
                CourseId = SelectedCourse.Id,
                Title = Title,
                ExamDate = ExamDate,
                DurationMinutes = DurationMinutes,
                ExamType = ExamType,
                BookletCount = BookletCount,
                ShuffleOptions = ShuffleOptions,
                CreatedByUserId = userId,
                SelectedQuestions = SelectedQuestions
                    .Select((q, i) => new ExamBankQuestionRef
                    {
                        QuestionId = q.Id,
                        OrderInExam = i + 1,
                        // Soru bankasındaki orijinal puan değiştirildiyse (örn. "100 Puana Eşitle"
                        // butonu) override olarak gönder. Aksi takdirde null = soru bankasındaki
                        // varsayılan puan kullanılır.
                        OverrideMaxPoints = q.MaxPoints
                    })
                    .ToList()
            };

            var newId = await _examService.CreateExamFromBankAsync(model);
            SuccessMessage = $"✓ Sınav oluşturuldu (Id: {newId}). Dengeli skor: {BalancePreview?.BalanceScore:0.0}/100";

            Title = string.Empty;
            SelectedQuestions.Clear();
            BalancePreview = null;
            await ReloadPoolAsync();

            ExamSaved?.Invoke();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Kayıt hatası: {ex.Message}" +
                           (ex.InnerException != null ? $" — {ex.InnerException.Message}" : "");
        }
        finally
        {
            IsSaving = false;
        }
    }
}
