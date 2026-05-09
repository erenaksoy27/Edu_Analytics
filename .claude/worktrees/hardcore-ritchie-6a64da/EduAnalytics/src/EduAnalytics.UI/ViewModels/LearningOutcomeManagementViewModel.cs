using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EduAnalytics.Business.Dtos;
using EduAnalytics.Business.Services.Interfaces;
using EduAnalytics.Core.Entities;

namespace EduAnalytics.UI.ViewModels;

public partial class LearningOutcomeManagementViewModel : ObservableObject
{
    private readonly ILearningOutcomeService _loService;
    private readonly IProgramOutcomeService _poService;
    private readonly IExamCrudService _examService;

    /// <summary>DbContext'e paralel sorgu gitmesini engellemek için.</summary>
    private readonly SemaphoreSlim _opLock = new(1, 1);

    [ObservableProperty] private ObservableCollection<Course> _courses = new();
    [ObservableProperty] private Course? _selectedCourse;
    [ObservableProperty] private ObservableCollection<LearningOutcomeDto> _learningOutcomes = new();

    // Form alanları (kod artık otomatik üretilir, kullanıcıya gösterilmez)
    [ObservableProperty] private string _formName = string.Empty;
    [ObservableProperty] private string _formDescription = string.Empty;
    [ObservableProperty] private LearningOutcomeDto? _editingOutcome;

    /// <summary>Düzenlemede olan ÖÇ'nin kodu (sadece info amaçlı UI'da gösterilir).</summary>
    [ObservableProperty] private string _editingCodeInfo = string.Empty;

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private string? _successMessage;

    public LearningOutcomeManagementViewModel(
        ILearningOutcomeService loService,
        IProgramOutcomeService poService,
        IExamCrudService examService)
    {
        _loService = loService;
        _poService = poService;
        _examService = examService;
    }

    public async Task LoadAsync()
    {
        IsLoading = true;
        ErrorMessage = null;
        SuccessMessage = null;

        try
        {
            var courses = await _examService.GetCoursesAsync();
            Courses = new ObservableCollection<Course>(courses);

            if (SelectedCourse == null && courses.Count > 0)
                SelectedCourse = courses[0];
            else
                await ReloadOutcomesAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Yükleme hatası: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    partial void OnSelectedCourseChanged(Course? value)
    {
        _ = ReloadOutcomesAsync();
    }

    private async Task ReloadOutcomesAsync()
    {
        if (SelectedCourse == null) return;
        await _opLock.WaitAsync();
        try
        {
            var list = await _loService.GetByCourseAsync(SelectedCourse.Id);
            LearningOutcomes = new ObservableCollection<LearningOutcomeDto>(list);
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
    private void StartEdit(LearningOutcomeDto? lo)
    {
        if (lo == null) return;
        EditingOutcome = lo;
        EditingCodeInfo = lo.Code;
        FormName = lo.Name;
        FormDescription = lo.Description ?? string.Empty;
    }

    [RelayCommand]
    private void StartNew()
    {
        EditingOutcome = null;
        EditingCodeInfo = string.Empty;
        FormName = string.Empty;
        FormDescription = string.Empty;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (SelectedCourse == null)
        {
            ErrorMessage = "Önce ders seçin.";
            return;
        }
        if (string.IsNullOrWhiteSpace(FormName))
        {
            ErrorMessage = "ÖÇ adı zorunludur.";
            return;
        }

        await _opLock.WaitAsync();
        try
        {
            // Düzenleme mi yeni mi? Düzenlemede mevcut kod korunur, yenide otomatik üretilir.
            string code = EditingOutcome?.Code ?? GenerateNextCode();

            var model = new LearningOutcomeCreateModel
            {
                CourseId = SelectedCourse.Id,
                Code = code,
                Name = FormName.Trim(),
                Description = string.IsNullOrWhiteSpace(FormDescription) ? null : FormDescription.Trim()
            };

            if (EditingOutcome == null)
            {
                await _loService.CreateAsync(model);
                SuccessMessage = $"✓ Yeni ÖÇ eklendi ({code}).";
            }
            else
            {
                await _loService.UpdateAsync(EditingOutcome.Id, model);
                SuccessMessage = $"✓ {code} güncellendi.";
            }

            StartNew();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            _opLock.Release();
        }
        await ReloadOutcomesAsync();
    }

    /// <summary>
    /// Mevcut ÖÇ kodlarına bakarak boş olan en küçük "ÖÇ-N" numarasını üretir.
    /// Örn. ÖÇ-1, ÖÇ-2, ÖÇ-3 varsa → ÖÇ-4. ÖÇ-2 silinmişse → ÖÇ-2.
    /// </summary>
    private string GenerateNextCode()
    {
        var existingNumbers = LearningOutcomes
            .Select(lo => lo.Code)
            .Select(c =>
            {
                var m = Regex.Match(c, @"\d+");
                return m.Success ? int.Parse(m.Value) : -1;
            })
            .Where(n => n > 0)
            .ToHashSet();

        int n = 1;
        while (existingNumbers.Contains(n)) n++;
        return $"ÖÇ-{n}";
    }

    [RelayCommand]
    private async Task DeleteAsync(LearningOutcomeDto? lo)
    {
        if (lo == null) return;
        var confirm = System.Windows.MessageBox.Show(
            $"{lo.Code} — {lo.Name} silinsin mi?",
            "ÖÇ Sil",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question);

        if (confirm != System.Windows.MessageBoxResult.Yes) return;

        await _opLock.WaitAsync();
        try
        {
            await _loService.DeleteAsync(lo.Id);
            LearningOutcomes.Remove(lo);
            SuccessMessage = "ÖÇ silindi.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            _opLock.Release();
        }
    }
}
