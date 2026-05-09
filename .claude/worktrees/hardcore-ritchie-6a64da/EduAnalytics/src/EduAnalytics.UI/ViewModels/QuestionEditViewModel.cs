using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EduAnalytics.Core.Entities;
using EduAnalytics.Core.Enums;

namespace EduAnalytics.UI.ViewModels;

/// <summary>
/// Sınav oluşturma formunda tek bir sorunun düzenlendiği alt ViewModel.
/// </summary>
public partial class QuestionEditViewModel : ObservableObject
{
    [ObservableProperty]
    private int _questionNumber;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsMultipleChoice))]
    [NotifyPropertyChangedFor(nameof(IsOpenEnded))]
    private QuestionType _type = QuestionType.MultipleChoice;

    [ObservableProperty] private decimal _maxPoints = 1.0m;
    [ObservableProperty] private string _questionText = string.Empty;
    [ObservableProperty] private string _optionA = string.Empty;
    [ObservableProperty] private string _optionB = string.Empty;
    [ObservableProperty] private string _optionC = string.Empty;
    [ObservableProperty] private string _optionD = string.Empty;
    [ObservableProperty] private string _optionE = string.Empty;
    [ObservableProperty] private OptionLetter _correctOption = OptionLetter.A;
    [ObservableProperty] private string _answerKey = string.Empty;

    /// <summary>UI'da gösterilecek şık harfleri (Test sorusunda radio button'lar için).</summary>
    public OptionLetter[] AvailableOptions { get; } =
        { OptionLetter.A, OptionLetter.B, OptionLetter.C, OptionLetter.D, OptionLetter.E };

    /// <summary>Mevcut ders için tüm konular (ortak referans).</summary>
    public ObservableCollection<Topic> AvailableTopics { get; } = new();

    /// <summary>Bu soruya bağlanmış konular.</summary>
    public ObservableCollection<Topic> SelectedTopics { get; } = new();

    [ObservableProperty]
    private Topic? _topicToAdd;

    public bool IsMultipleChoice => Type == QuestionType.MultipleChoice;
    public bool IsOpenEnded => Type == QuestionType.OpenEnded;

    [RelayCommand]
    private void AddTopic()
    {
        if (TopicToAdd != null && !SelectedTopics.Any(t => t.Id == TopicToAdd.Id))
        {
            SelectedTopics.Add(TopicToAdd);
        }
        TopicToAdd = null;
    }

    [RelayCommand]
    private void RemoveTopic(Topic? topic)
    {
        if (topic != null)
            SelectedTopics.Remove(topic);
    }

    /// <summary>
    /// Tür değiştiğinde varsayılan puanı akıllıca ayarla.
    /// </summary>
    partial void OnTypeChanged(QuestionType value)
    {
        if (value == QuestionType.MultipleChoice && MaxPoints > 5)
            MaxPoints = 1.0m;
        else if (value == QuestionType.OpenEnded && MaxPoints <= 1)
            MaxPoints = 5.0m;
    }

    /// <summary>
    /// UI modelini servisin beklediği DTO'ya dönüştür.
    /// </summary>
    public Business.Dtos.QuestionCreateModel ToCreateModel()
    {
        return new Business.Dtos.QuestionCreateModel
        {
            QuestionNumber = QuestionNumber,
            Type = Type,
            MaxPoints = MaxPoints,
            QuestionText = QuestionText,
            OptionA = IsMultipleChoice ? OptionA : string.Empty,
            OptionB = IsMultipleChoice ? OptionB : string.Empty,
            OptionC = IsMultipleChoice ? OptionC : string.Empty,
            OptionD = IsMultipleChoice ? OptionD : string.Empty,
            OptionE = IsMultipleChoice ? OptionE : string.Empty,
            CorrectOption = IsMultipleChoice ? CorrectOption : OptionLetter.Empty,
            AnswerKey = IsOpenEnded ? AnswerKey : null,
            IsActive = true,
            IsFavorite = false,
            TopicIds = SelectedTopics.Select(t => t.Id).ToList(),
            LearningOutcomeIds = new List<int>(),
            QuestionGroupId = null
        };
    }

    /// <summary>
    /// Form doğrulaması. Sorun varsa hata mesajı döner, yoksa null.
    /// </summary>
    public string? Validate()
    {
        if (string.IsNullOrWhiteSpace(QuestionText))
            return $"Soru {QuestionNumber}: Soru metni boş olamaz.";
        if (MaxPoints <= 0)
            return $"Soru {QuestionNumber}: Puan 0'dan büyük olmalı.";
        if (SelectedTopics.Count == 0)
            return $"Soru {QuestionNumber}: En az bir konu seçilmeli.";

        if (IsMultipleChoice)
        {
            if (string.IsNullOrWhiteSpace(OptionA) || string.IsNullOrWhiteSpace(OptionB) ||
                string.IsNullOrWhiteSpace(OptionC) || string.IsNullOrWhiteSpace(OptionD) 
                || string.IsNullOrWhiteSpace(OptionE))
                return $"Soru {QuestionNumber}: Tüm şıklar (A/B/C/D/E) doldurulmalı.";
            if (CorrectOption == OptionLetter.Empty)
                return $"Soru {QuestionNumber}: Doğru şık seçilmeli.";
        }

        return null;
    }
}
