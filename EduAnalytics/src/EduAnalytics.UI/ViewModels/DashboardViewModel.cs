using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EduAnalytics.Business.Dtos;
using EduAnalytics.Business.Services.Interfaces;

namespace EduAnalytics.UI.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly IExamAnalysisService _examAnalysis;
    private readonly IExamStatisticsService _statisticsService;
    private readonly IItemAnalysisService _itemAnalysisService;
    private readonly ILearningOutcomePerformanceService _learningOutcomeService;
    private readonly IStudentService _studentService;

    public event Action<int>? OpenAnalysisRequested;

    [ObservableProperty]
    private ObservableCollection<ExamSummaryDto> _exams = new();

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private int _studentCount;

    [ObservableProperty]
    private double _overallAverageSuccessRate;

    [ObservableProperty]
    private string _lastActivityDate = "—";

    public DashboardViewModel(
        IExamAnalysisService examAnalysis,
        IExamStatisticsService statisticsService,
        IItemAnalysisService itemAnalysisService,
        ILearningOutcomePerformanceService learningOutcomeService,
        IStudentService studentService)
    {
        _examAnalysis = examAnalysis;
        _statisticsService = statisticsService;
        _itemAnalysisService = itemAnalysisService;
        _learningOutcomeService = learningOutcomeService;
        _studentService = studentService;
    }

    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var listTask    = _examAnalysis.GetAllExamSummariesAsync();
            var studentsTask = _studentService.GetAllAsync();

            var list = await listTask;
            foreach (var exam in list)
                await EnrichDashboardMetricsAsync(exam);

            Exams = new ObservableCollection<ExamSummaryDto>(list);

            StudentCount = (await studentsTask).Count;

            OverallAverageSuccessRate = Exams.Count > 0
                ? Math.Round(Exams.Average(e => e.AverageSuccessRate), 1)
                : 0;

            var latest = Exams.MaxBy(e => e.ExamDate);
            LastActivityDate = latest is not null
                ? latest.ExamDate.ToString("dd MMM yyyy")
                : "—";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task EnrichDashboardMetricsAsync(ExamSummaryDto exam)
    {
        var statistics = await _statisticsService.ComputeAsync(exam.ExamId, 50);
        if (statistics != null)
        {
            exam.MedianScore = statistics.Median;
            exam.CronbachAlpha = statistics.CronbachAlpha;
            exam.CronbachAlphaInterpretation = statistics.CronbachAlphaInterpretation;
            exam.PassingScore = statistics.PassingScore;
            exam.PassRate = statistics.PassRate;
        }

        var itemAnalysis = await _itemAnalysisService.AnalyzeAsync(exam.ExamId);
        if (itemAnalysis.Count > 0)
        {
            exam.AverageDifficultyIndex = Math.Round(itemAnalysis.Average(i => i.DifficultyIndex), 3);
            exam.AverageDiscriminationIndex = Math.Round(itemAnalysis.Average(i => i.DiscriminationIndex), 3);
        }

        var outcomes = await _learningOutcomeService.AnalyzeExamAsync(exam.ExamId);
        var lowestOutcome = outcomes.OrderBy(o => o.SuccessRate).FirstOrDefault();
        if (lowestOutcome != null)
        {
            exam.LowestLearningOutcomeName = lowestOutcome.OutcomeName;
            exam.LowestLearningOutcomeSuccessRate = Math.Round(lowestOutcome.SuccessRate, 1);
        }
    }

    [RelayCommand]
    private void OpenAnalysis(ExamSummaryDto? exam)
    {
        if (exam != null)
            OpenAnalysisRequested?.Invoke(exam.ExamId);
    }
}
