using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EduAnalytics.UI.Services;
using Microsoft.Extensions.DependencyInjection;

namespace EduAnalytics.UI.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IServiceProvider _services;

    /// <summary>Sağ alt köşede bildirim göstermek için. Tüm VM'ler bunu kullanır.</summary>
    public ToastService Toasts { get; }

    [ObservableProperty]
    private ObservableObject? _currentView;

    [ObservableProperty]
    private string _activeMenu = "Dashboard";

    public MainViewModel(IServiceProvider services, ToastService toasts)
    {
        _services = services;
        Toasts = toasts;
        NavigateToDashboard();
    }

    /// <summary>
    /// Yeni view'a geçmeden önce eski view'in tüm event aboneliklerini temizler.
    /// MainViewModel uzun ömürlüdür; her navigation yeni VM oluşturur, eski VM'lere bağlı
    /// handler'lar GC'ye gitmez ve memory leak olur.
    /// </summary>
    private void DetachCurrentView()
    {
        switch (CurrentView)
        {
            case DashboardViewModel d:
                d.OpenAnalysisRequested -= OnOpenAnalysisRequested;
                break;
            case ExamFromBankViewModel efb:
                efb.ExamSaved -= OnExamSaved;
                break;
            case ExamManagementViewModel em:
                em.OpenAnalysisRequested -= OnOpenAnalysisRequested;
                break;
            case ExamAnalysisViewModel ea:
                ea.OpenAnswerEntryRequested -= OnOpenAnswerEntryRequested;
                break;
            case QuestionBankViewModel qb:
                qb.QuestionCreateRequested -= OnQuestionCreateRequested;
                break;
            case SingleQuestionCreateViewModel sq:
                sq.QuestionSaved -= OnSingleQuestionSaved;
                break;
            case AnswerEntryViewModel ae:
                ae.BackRequested -= OnAnswerEntryBackRequested;
                break;
        }
    }

    [RelayCommand]
    private void NavigateToDashboard()
    {
        DetachCurrentView();
        ActiveMenu = "Dashboard";
        var vm = _services.GetRequiredService<DashboardViewModel>();
        vm.OpenAnalysisRequested += OnOpenAnalysisRequested;
        _ = vm.LoadAsync();
        CurrentView = vm;
    }

    [RelayCommand]
    private void NavigateToExamFromBank()
    {
        DetachCurrentView();
        ActiveMenu = "ExamFromBank";
        var vm = _services.GetRequiredService<ExamFromBankViewModel>();
        vm.ExamSaved += OnExamSaved;
        _ = vm.LoadAsync();
        CurrentView = vm;
    }

    [RelayCommand]
    private void NavigateToExamManagement()
    {
        DetachCurrentView();
        ActiveMenu = "ExamManagement";
        var vm = _services.GetRequiredService<ExamManagementViewModel>();
        vm.OpenAnalysisRequested += OnOpenAnalysisRequested;
        _ = vm.LoadAsync();
        CurrentView = vm;
    }

    [RelayCommand]
    private void NavigateToCommonStem()
    {
        DetachCurrentView();
        ActiveMenu = "CommonStem";
        var vm = _services.GetRequiredService<QuestionGroupEditorViewModel>();
        _ = vm.LoadAsync();
        CurrentView = vm;
    }

    [RelayCommand]
    private void NavigateToQuestionBank()
    {
        DetachCurrentView();
        ActiveMenu = "QuestionBank";
        var vm = _services.GetRequiredService<QuestionBankViewModel>();
        vm.QuestionCreateRequested += OnQuestionCreateRequested;
        _ = vm.LoadAsync();
        CurrentView = vm;
    }

    private void OnQuestionCreateRequested()
    {
        DetachCurrentView();
        ActiveMenu = "SingleQuestionCreate";
        var vm = _services.GetRequiredService<SingleQuestionCreateViewModel>();
        vm.QuestionSaved += OnSingleQuestionSaved;
        _ = vm.LoadAsync();
        CurrentView = vm;
    }

    [RelayCommand]
    private void NavigateToLearningOutcomes()
    {
        DetachCurrentView();
        ActiveMenu = "LearningOutcomes";
        var vm = _services.GetRequiredService<LearningOutcomeManagementViewModel>();
        _ = vm.LoadAsync();
        CurrentView = vm;
    }

    [RelayCommand]
    private void NavigateToProgramOutcomes()
    {
        DetachCurrentView();
        ActiveMenu = "ProgramOutcomes";
        var vm = _services.GetRequiredService<ProgramOutcomeManagementViewModel>();
        _ = vm.LoadAsync();
        CurrentView = vm;
    }

    [RelayCommand]
    private void NavigateToProgramOutcomeReport()
    {
        DetachCurrentView();
        ActiveMenu = "ProgramOutcomeReport";
        var vm = _services.GetRequiredService<ProgramOutcomeReportViewModel>();
        _ = vm.LoadAsync();
        CurrentView = vm;
    }

    [RelayCommand]
    private void NavigateToProgramOutcomeMapping()
    {
        DetachCurrentView();
        ActiveMenu = "ProgramOutcomeMapping";
        var vm = _services.GetRequiredService<ProgramOutcomeMappingViewModel>();
        _ = vm.LoadAsync();
        CurrentView = vm;
    }

    [RelayCommand]
    private void NavigateToStudents()
    {
        DetachCurrentView();
        ActiveMenu = "Students";
        var vm = _services.GetRequiredService<StudentManagementViewModel>();
        _ = vm.LoadAsync();
        CurrentView = vm;
    }

    [RelayCommand]
    private void NavigateToAcademicStructure()
    {
        DetachCurrentView();
        ActiveMenu = "AcademicStructure";
        var vm = _services.GetRequiredService<AcademicStructureViewModel>();
        _ = vm.LoadAsync();
        CurrentView = vm;
    }

    private void OnOpenAnalysisRequested(int examId)
    {
        DetachCurrentView();
        ActiveMenu = "Analiz";
        var vm = _services.GetRequiredService<ExamAnalysisViewModel>();
        vm.OpenAnswerEntryRequested += OnOpenAnswerEntryRequested;
        _ = vm.LoadAsync(examId);
        CurrentView = vm;
    }

    private void OnOpenAnswerEntryRequested(int examId)
    {
        DetachCurrentView();
        ActiveMenu = "Cevap";
        var vm = _services.GetRequiredService<AnswerEntryViewModel>();
        vm.BackRequested += OnAnswerEntryBackRequested;
        _ = vm.LoadAsync(examId);
        CurrentView = vm;
    }

    private void OnAnswerEntryBackRequested(int examId)
    {
        OnOpenAnalysisRequested(examId);
    }

    private void OnExamSaved()
    {
        NavigateToDashboard();
    }

    private void OnSingleQuestionSaved()
    {
        NavigateToQuestionBank();
    }
}
