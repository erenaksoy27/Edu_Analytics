using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EduAnalytics.Business.Dtos;
using EduAnalytics.Business.Services.Interfaces;

namespace EduAnalytics.UI.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly IExamAnalysisService _examAnalysis;

    public event Action<int>? OpenAnalysisRequested;

    [ObservableProperty]
    private ObservableCollection<ExamSummaryDto> _exams = new();

    [ObservableProperty]
    private bool _isLoading;

    public DashboardViewModel(IExamAnalysisService examAnalysis)
    {
        _examAnalysis = examAnalysis;
    }

    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var list = await _examAnalysis.GetAllExamSummariesAsync();
            Exams = new ObservableCollection<ExamSummaryDto>(list);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void OpenAnalysis(ExamSummaryDto? exam)
    {
        if (exam != null)
            OpenAnalysisRequested?.Invoke(exam.ExamId);
    }
}
