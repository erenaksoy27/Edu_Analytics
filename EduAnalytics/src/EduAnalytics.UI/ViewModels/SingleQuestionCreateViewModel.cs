using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EduAnalytics.Business.Services.Interfaces;
using EduAnalytics.Core.Entities;
using EduAnalytics.Business.Dtos;

namespace EduAnalytics.UI.ViewModels;

public partial class SingleQuestionCreateViewModel : ObservableObject
{
    private readonly IExamCrudService _service;

    public event Action? QuestionSaved;

    [ObservableProperty] private ObservableCollection<Course> _courses = new();
    [ObservableProperty] private Course? _selectedCourse;

    [ObservableProperty] private QuestionEditViewModel _question = new();
    
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private string? _successMessage;
    [ObservableProperty] private bool _isSaving;

    public SingleQuestionCreateViewModel(IExamCrudService service)
    {
        _service = service;
        Question.QuestionNumber = 1;
    }

    public async Task LoadAsync()
    {
        ErrorMessage = null;
        SuccessMessage = null;
        try
        {
            var courses = await _service.GetCoursesAsync();
            Courses = new ObservableCollection<Course>(courses);
            if (SelectedCourse == null && Courses.Count > 0)
            {
                SelectedCourse = Courses[0];
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = "Veriler yüklenemedi: " + ex.Message;
        }
    }

    partial void OnSelectedCourseChanged(Course? value)
    {
        _ = ReloadTopicsAsync();
        Question.AvailableTopics.Clear();
    }

    private async Task ReloadTopicsAsync()
    {
        if (SelectedCourse == null) return;
        try
        {
            var topics = await _service.GetTopicsForCourseAsync(SelectedCourse.Id);
            Question.AvailableTopics.Clear();
            foreach (var t in topics) Question.AvailableTopics.Add(t);
        }
        catch { }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        ErrorMessage = null;
        SuccessMessage = null;

        if (SelectedCourse == null)
        {
            ErrorMessage = "Lütfen bir ders seçin.";
            return;
        }

        var err = Question.Validate();
        if (err != null)
        {
            ErrorMessage = err;
            return;
        }

        IsSaving = true;
        try
        {
            var userId = await _service.GetDefaultUserIdAsync();

            var model = new ExamCreateModel
            {
                CourseId = SelectedCourse.Id,
                Title = "Single Question Draft",
                ExamDate = DateTime.Today,
                CreatedByUserId = userId,
                Questions = new System.Collections.Generic.List<QuestionCreateModel> { Question.ToCreateModel() }
            };

            await _service.CreateExamAsync(model);

            SuccessMessage = "Soru baþarýyla oluþturuldu.";
            Question = new QuestionEditViewModel { QuestionNumber = 1 };
            QuestionSaved?.Invoke();
        }
        catch (Exception ex)
        {
            ErrorMessage = "Kayýt hatasý: " + ex.Message;
        }
        finally
        {
            IsSaving = false;
        }
    }
}