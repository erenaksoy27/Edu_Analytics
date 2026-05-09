using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EduAnalytics.Business.Dtos;
using EduAnalytics.Business.Services.Interfaces;
using EduAnalytics.UI.Services;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;

namespace EduAnalytics.UI.ViewModels;

public partial class ExamAnalysisViewModel : ObservableObject
{
    private readonly IExamAnalysisService _examService;
    private readonly IDistractorAnalysisService _distractorService;
    private readonly IStudentPerformanceService _studentService;
    private readonly ILearningOutcomePerformanceService _learningOutcomeService;
    private readonly IExamCancellationService _cancellationService;
    private readonly IExamBalanceCheckService _balanceService;
    private readonly IExamStatisticsService _statisticsService;
    private readonly IItemAnalysisService _itemAnalysisService;
    private readonly IThemeService _themeService;

    /// <summary>Parent (MainViewModel) bu event'i dinleyerek Cevap Girişi ekranına geçer.</summary>
    public event Action<int>? OpenAnswerEntryRequested;

    private int _currentExamId;

    [ObservableProperty] private ExamSummaryDto? _summary;
    [ObservableProperty] private ObservableCollection<LearningOutcomePerformanceDto> _learningOutcomes = new();
    [ObservableProperty] private ObservableCollection<QuestionAnalysisDto> _questions = new();
    [ObservableProperty] private ObservableCollection<QuestionAnalysisDto> _strongDistractors = new();
    [ObservableProperty] private ObservableCollection<StudentPerformanceDto> _students = new();

    // Öğrenci Filtreleme
    [ObservableProperty] private ObservableCollection<string> _availableClasses = new();
    [ObservableProperty] private string? _selectedClass;
    [ObservableProperty] private ObservableCollection<StudentPerformanceDto> _filteredStudents = new();

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string? _errorMessage;

    // Grafikler — Build* metotları ExamAnalysisViewModel.Charts.cs içinde
    [ObservableProperty] private ISeries[] _learningOutcomeChartSeries = Array.Empty<ISeries>();
    [ObservableProperty] private Axis[] _learningOutcomeChartXAxes = Array.Empty<Axis>();
    [ObservableProperty] private Axis[] _learningOutcomeChartYAxes = Array.Empty<Axis>();

    [ObservableProperty] private ISeries[] _questionSuccessSeries = Array.Empty<ISeries>();
    [ObservableProperty] private Axis[] _questionXAxes = Array.Empty<Axis>();
    [ObservableProperty] private Axis[] _questionYAxes = Array.Empty<Axis>();

    [ObservableProperty] private ISeries[] _scoreDistributionSeries = Array.Empty<ISeries>();
    [ObservableProperty] private Axis[] _scoreDistributionXAxes = Array.Empty<Axis>();

    [ObservableProperty] private ISeries[] _successRateDoughnutSeries = Array.Empty<ISeries>();

    [ObservableProperty] private ISeries[] _radarSeries = Array.Empty<ISeries>();
    [ObservableProperty] private PolarAxis[] _radarAxes = Array.Empty<PolarAxis>();

    [ObservableProperty] private ExamBalanceReportDto? _balanceReport;

    // ─── FAZ 6: İstatistik & Madde Analizi ───
    [ObservableProperty] private ExamStatisticsDto? _statistics;
    [ObservableProperty] private ObservableCollection<ItemAnalysisDto> _itemAnalysis = new();

    /// <summary>Kabul edilebilirlik indeksi (geçme eşiği). Varsayılan %50.</summary>
    [ObservableProperty] private double _acceptabilityIndex = 50.0;

    [ObservableProperty] private ISeries[] _difficultyChartSeries = Array.Empty<ISeries>();
    [ObservableProperty] private Axis[] _difficultyChartXAxes = Array.Empty<Axis>();
    [ObservableProperty] private Axis[] _difficultyChartYAxes = Array.Empty<Axis>();

    [ObservableProperty] private ISeries[] _discriminationChartSeries = Array.Empty<ISeries>();
    [ObservableProperty] private Axis[] _discriminationChartXAxes = Array.Empty<Axis>();
    [ObservableProperty] private Axis[] _discriminationChartYAxes = Array.Empty<Axis>();

    [ObservableProperty] private ISeries[] _upperLowerSeries = Array.Empty<ISeries>();
    [ObservableProperty] private Axis[] _upperLowerXAxes = Array.Empty<Axis>();
    [ObservableProperty] private Axis[] _upperLowerYAxes = Array.Empty<Axis>();

    [ObservableProperty] private ISeries[] _answerDistributionSeries = Array.Empty<ISeries>();
    [ObservableProperty] private Axis[] _answerDistributionXAxes = Array.Empty<Axis>();
    [ObservableProperty] private Axis[] _answerDistributionYAxes = Array.Empty<Axis>();

    public ExamAnalysisViewModel(
        IExamAnalysisService examService,
        IDistractorAnalysisService distractorService,
        IStudentPerformanceService studentService,
        ILearningOutcomePerformanceService learningOutcomeService,
        IExamCancellationService cancellationService,
        IExamBalanceCheckService balanceService,
        IExamStatisticsService statisticsService,
        IItemAnalysisService itemAnalysisService,
        IThemeService themeService)
    {
        _examService = examService;
        _distractorService = distractorService;
        _studentService = studentService;
        _learningOutcomeService = learningOutcomeService;
        _cancellationService = cancellationService;
        _balanceService = balanceService;
        _statisticsService = statisticsService;
        _itemAnalysisService = itemAnalysisService;
        _themeService = themeService;
        _themeService.ThemeChanged += () =>
        {
            if (_currentExamId > 0)
                System.Windows.Application.Current.Dispatcher.Invoke(RebuildCharts);
        };
    }

    private void RebuildCharts()
    {
        var outcomes = LearningOutcomes.ToList();
        var questions = Questions.ToList();
        var students  = Students.ToList();
        var items     = ItemAnalysis.ToList();
        BuildLearningOutcomeChart(outcomes);
        BuildQuestionChart(questions);
        BuildScoreDistributionHistogram(students, Summary?.MaxPossibleScore ?? 0);
        BuildSuccessRateDoughnut(Summary?.AverageSuccessRate ?? 0);
        BuildRadarChart(outcomes);
        BuildDifficultyChart(items);
        BuildDiscriminationChart(items);
        BuildUpperLowerChart(items);
        BuildAnswerDistributionChart(items);
    }

    public async Task LoadAsync(int examId)
    {
        _currentExamId = examId;
        IsLoading = true;
        ErrorMessage = null;

        Summary = null;
        BalanceReport = null;

        try
        {
            Summary = await _examService.GetSummaryAsync(examId);

            var learningOutcomeList = await _learningOutcomeService.AnalyzeExamAsync(examId);
            LearningOutcomes = new ObservableCollection<LearningOutcomePerformanceDto>(learningOutcomeList);

            var questionList = await _distractorService.AnalyzeExamAsync(examId);
            Questions = new ObservableCollection<QuestionAnalysisDto>(questionList);

            var strongList = await _distractorService.GetStrongDistractorsAsync(examId, 50);
            StrongDistractors = new ObservableCollection<QuestionAnalysisDto>(strongList);

            var studentList = await _studentService.GetExamRankingAsync(examId);
            Students = new ObservableCollection<StudentPerformanceDto>(studentList);

            BalanceReport = await _balanceService.AnalyzeAsync(examId);

            Statistics = await _statisticsService.ComputeAsync(examId, AcceptabilityIndex);

            var itemList = await _itemAnalysisService.AnalyzeAsync(examId);
            ItemAnalysis = new ObservableCollection<ItemAnalysisDto>(itemList);

            // Sınıf filtresini doldur
            var classes = Students.Select(s => s.ClassName).Where(c => !string.IsNullOrEmpty(c)).Distinct().OrderBy(c => c).ToList();
            var classList = new List<string> { "Tümü" };
            classList.AddRange(classes);
            AvailableClasses = new ObservableCollection<string>(classList);
            SelectedClass = "Tümü";

            BuildLearningOutcomeChart(learningOutcomeList);
            BuildQuestionChart(questionList);
            BuildScoreDistributionHistogram(studentList, Summary?.MaxPossibleScore ?? 0);
            BuildSuccessRateDoughnut(Summary?.AverageSuccessRate ?? 0);
            BuildRadarChart(learningOutcomeList);
            BuildDifficultyChart(itemList);
            BuildDiscriminationChart(itemList);
            BuildUpperLowerChart(itemList);
            BuildAnswerDistributionChart(itemList);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"HATA: {ex.GetType().Name}\n{ex.Message}" +
                           (ex.InnerException != null ? $"\n\nİç Hata: {ex.InnerException.Message}" : "");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task CancelQuestionAsync(QuestionAnalysisDto? q)
    {
        if (q == null || _currentExamId <= 0) return;

        var dialog = new EduAnalytics.UI.Views.QuestionCancelDialog
        {
            QuestionInfo = $"Soru {q.QuestionNumber}: {q.QuestionText[..Math.Min(q.QuestionText.Length, 100)]}",
            Owner = System.Windows.Application.Current.MainWindow
        };

        if (dialog.ShowDialog() != true) return;

        try
        {
            await _cancellationService.CancelQuestionAsync(_currentExamId, q.QuestionId, dialog.Reason);
            await LoadAsync(_currentExamId);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"İptal hatası: {ex.Message}";
        }
    }

    [RelayCommand]
    private void OpenAnswerEntry()
    {
        if (_currentExamId > 0)
            OpenAnswerEntryRequested?.Invoke(_currentExamId);
    }

    partial void OnAcceptabilityIndexChanged(double value)
    {
        if (_currentExamId > 0 && !IsLoading)
            _ = ReloadStatisticsAsync();
    }

    private async Task ReloadStatisticsAsync()
    {
        try
        {
            Statistics = await _statisticsService.ComputeAsync(_currentExamId, AcceptabilityIndex);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"İstatistik güncellenemedi: {ex.Message}";
        }
    }

    partial void OnSelectedClassChanged(string? value) => FilterStudents();

    private void FilterStudents()
    {
        if (string.IsNullOrEmpty(SelectedClass) || SelectedClass == "Tümü")
        {
            FilteredStudents = new ObservableCollection<StudentPerformanceDto>(Students);
        }
        else
        {
            var filtered = Students.Where(s => s.ClassName == SelectedClass).ToList();
            FilteredStudents = new ObservableCollection<StudentPerformanceDto>(filtered);
        }
    }
}
