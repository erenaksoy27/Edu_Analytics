using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EduAnalytics.Business.Dtos;
using EduAnalytics.Business.Services.Interfaces;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using Microsoft.Win32;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.IO;

namespace EduAnalytics.UI.ViewModels;

public partial class ExamAnalysisViewModel : ObservableObject
{
    private readonly IExamAnalysisService _examService;
    private readonly IDistractorAnalysisService _distractorService;
    private readonly ITopicPerformanceService _topicService;
    private readonly IStudentPerformanceService _studentService;
    private readonly ILearningOutcomePerformanceService _learningOutcomeService;
    private readonly IExamCancellationService _cancellationService;
    private readonly IExamBalanceCheckService _balanceService;

    /// <summary>Parent (MainViewModel) bu event'i dinleyerek Cevap Girişi ekranına geçer.</summary>
    public event Action<int>? OpenAnswerEntryRequested;

    private int _currentExamId;

    [ObservableProperty] private ExamSummaryDto? _summary;
    [ObservableProperty] private ObservableCollection<TopicPerformanceDto> _topics = new();
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

    // Grafikler
    [ObservableProperty] private ISeries[] _topicChartSeries = Array.Empty<ISeries>();
    [ObservableProperty] private Axis[] _topicChartXAxes = Array.Empty<Axis>();
    [ObservableProperty] private Axis[] _topicChartYAxes = Array.Empty<Axis>();

    [ObservableProperty] private ISeries[] _questionSuccessSeries = Array.Empty<ISeries>();
    [ObservableProperty] private Axis[] _questionXAxes = Array.Empty<Axis>();
    [ObservableProperty] private Axis[] _questionYAxes = Array.Empty<Axis>();

    // Yeni Grafikler
    [ObservableProperty] private ISeries[] _scoreDistributionSeries = Array.Empty<ISeries>();
    [ObservableProperty] private Axis[] _scoreDistributionXAxes = Array.Empty<Axis>();

    [ObservableProperty] private ISeries[] _successRateDoughnutSeries = Array.Empty<ISeries>();
    
    [ObservableProperty] private ISeries[] _radarSeries = Array.Empty<ISeries>();
    [ObservableProperty] private PolarAxis[] _radarAxes = Array.Empty<PolarAxis>();

    [ObservableProperty] private ExamBalanceReportDto? _balanceReport;

    public ExamAnalysisViewModel(
        IExamAnalysisService examService,
        IDistractorAnalysisService distractorService,
        ITopicPerformanceService topicService,
        IStudentPerformanceService studentService,
        ILearningOutcomePerformanceService learningOutcomeService,
        IExamCancellationService cancellationService,
        IExamBalanceCheckService balanceService)
    {
        _examService = examService;
        _distractorService = distractorService;
        _topicService = topicService;
        _studentService = studentService;
        _learningOutcomeService = learningOutcomeService;
        _cancellationService = cancellationService;
        _balanceService = balanceService;
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

    [RelayCommand]
    private async Task ExportReportAsync()
    {
        try
        {
            var sfd = new SaveFileDialog
            {
                Filter = "PDF Dosyası (*.pdf)|*.pdf",
                Title = "Analiz Raporunu Dışa Aktar",
                FileName = $"{Summary?.ExamTitle ?? "Analiz"}_Raporu.pdf"
            };

            if (sfd.ShowDialog() == true)
            {
                IsLoading = true;
                
                // QuestPDF lisans yapılandırması (Community Edition)
                QuestPDF.Settings.License = LicenseType.Community;

                await Task.Run(() =>
                {
                    Document.Create(container =>
                    {
                        container.Page(page =>
                        {
                            page.Size(PageSizes.A4);
                            page.Margin(2, Unit.Centimetre);
                            page.PageColor(Colors.White);
                            page.DefaultTextStyle(x => x.FontSize(11).FontFamily(Fonts.Arial)); // You can use standard fonts if Arial fails, e.g. .FontFamily(Fonts.Arial) or just standard

                            page.Header().Element(ComposeHeader);
                            page.Content().Element(ComposeContent);
                            page.Footer().Element(ComposeFooter);
                        });
                    })
                    .GeneratePdf(sfd.FileName);
                });

                // Başarılı olursa hata mesajını temizle, dilerseniz bir SuccessMessage da eklenebilir.
                ErrorMessage = null;
                // Örneğin: SuccessMessage = "PDF başarıyla dışa aktarıldı."; (Tabi ViewModel'da böyle bir property varsa)
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"PDF Dışa Aktarım Hatası: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ComposeHeader(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(column =>
            {
                column.Item().Text(Summary?.ExamTitle ?? "Sınav Analiz Raporu").FontSize(20).SemiBold().FontColor(Colors.Blue.Darken2);
                column.Item().Text(Summary?.CourseName ?? "Ders Adı Yok").FontSize(14).FontColor(Colors.Grey.Medium);
            });
            row.ConstantItem(100).AlignRight().Text($"{DateTime.Now:dd.MM.yyyy}").FontSize(10);
        });
    }

    private void ComposeContent(IContainer container)
    {
        container.PaddingVertical(1, Unit.Centimetre).Column(column =>
        {
            column.Spacing(20);

            // Özet Metrikleri
            column.Item().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingBottom(5).Text("Genel Sınav Özeti").FontSize(14).SemiBold();
            
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                });

                table.Cell().Text("Öğrenci Sayısı").SemiBold();
                table.Cell().Text("Soru Sayısı").SemiBold();
                table.Cell().Text("Genel Başarı").SemiBold();
                table.Cell().Text("Ortalama Puan").SemiBold();

                table.Cell().Text(Summary?.TotalStudents.ToString() ?? "-");
                table.Cell().Text(Summary?.TotalQuestions.ToString() ?? "-");
                table.Cell().Text($"%{Summary?.AverageSuccessRate:N1}");
                table.Cell().Text($"{Summary?.AverageScore:N2} / {Summary?.MaxPossibleScore:N0}");
            });

            // Güçlü Çeldiriciler
            if (StrongDistractors != null && StrongDistractors.Count > 0)
            {
                column.Item().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingBottom(5).Text("En Güçlü Çeldiriciler").FontSize(14).SemiBold().FontColor(Colors.Red.Darken2);

                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                    });

                    table.Header(header =>
                    {
                        header.Cell().Text("Soru").SemiBold();
                        header.Cell().Text("Doğru").SemiBold();
                        header.Cell().Text("Çeldirici").SemiBold();
                        header.Cell().Text("Yanılan %").SemiBold();
                    });

                    foreach (var dist in StrongDistractors)
                    {
                        table.Cell().Text($"Soru {dist.QuestionNumber}");
                        table.Cell().Text(dist.CorrectOption);
                        table.Cell().Text(dist.StrongestDistractorOption).FontColor(Colors.Red.Medium);
                        table.Cell().Text($"%{dist.StrongestDistractorRate:N1}");
                    }
                });
            }

            // Konu Analizi
            if (Topics != null && Topics.Count > 0)
            {
                column.Item().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingBottom(5).Text("Konu / Kazanım Başarı Dağılımı").FontSize(14).SemiBold();

                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(50); // Hafta
                        columns.RelativeColumn(); // Konu Adı
                        columns.RelativeColumn(); // Seviye
                    });

                    table.Header(header =>
                    {
                        header.Cell().Text("Hafta").SemiBold();
                        header.Cell().Text("Konu Adı").SemiBold();
                        header.Cell().Text("Başarı Oranı").SemiBold();
                    });

                    foreach (var topic in Topics)
                    {
                        table.Cell().Text($"Hafta {topic.WeekNumber}");
                        table.Cell().Text(topic.TopicTitle);
                        table.Cell().Text($"%{topic.SuccessRate:N1} - {topic.PerformanceLevel}");
                    }
                });
            }

            // Öğrenim Çıktısı Analizi
            if (LearningOutcomes != null && LearningOutcomes.Count > 0)
            {
                column.Item().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingBottom(5).Text("Öğrenim Çıktısı Başarı Dağılımı").FontSize(14).SemiBold();

                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(2); // Çıktı Adı
                        columns.RelativeColumn(3); // Açıklama
                        columns.RelativeColumn(1); // Seviye
                    });

                    table.Header(header =>
                    {
                        header.Cell().Text("Öğrenim Çıktısı").SemiBold();
                        header.Cell().Text("Açıklama").SemiBold();
                        header.Cell().Text("Başarı Oranı").SemiBold();
                    });

                    foreach (var outcome in LearningOutcomes)
                    {
                        table.Cell().Text(outcome.OutcomeName);
                        table.Cell().Text(outcome.Description ?? "-");
                        table.Cell().Text($"%{outcome.SuccessRate:N1} - {outcome.PerformanceLevel}");
                    }
                });
            }
        });
    }

    private void ComposeFooter(IContainer container)
    {
        container.AlignCenter().Text(x =>
        {
            x.Span("Sayfa ");
            x.CurrentPageNumber();
            x.Span(" / ");
            x.TotalPages();
        });
    }

    public async Task LoadAsync(int examId)
    {
        _currentExamId = examId;
        IsLoading = true;
        ErrorMessage = null;

        // Eski yüklemeden kalan veriler dursun istemiyoruz — temizle
        Summary = null;
        BalanceReport = null;

        try
        {
            Summary = await _examService.GetSummaryAsync(examId);

            var topicList = await _topicService.AnalyzeExamAsync(examId);
            Topics = new ObservableCollection<TopicPerformanceDto>(topicList);

            var learningOutcomeList = await _learningOutcomeService.AnalyzeExamAsync(examId);
            LearningOutcomes = new ObservableCollection<LearningOutcomePerformanceDto>(learningOutcomeList);

            var questionList = await _distractorService.AnalyzeExamAsync(examId);
            Questions = new ObservableCollection<QuestionAnalysisDto>(questionList);

            var strongList = await _distractorService.GetStrongDistractorsAsync(examId, 50);
            StrongDistractors = new ObservableCollection<QuestionAnalysisDto>(strongList);

            var studentList = await _studentService.GetExamRankingAsync(examId);
            Students = new ObservableCollection<StudentPerformanceDto>(studentList);

            // FAZ 4: Denge raporu
            BalanceReport = await _balanceService.AnalyzeAsync(examId);

            // Sınıf Filtresi (Combobox) dolduruluyor
            var classes = Students.Select(s => s.ClassName).Where(c => !string.IsNullOrEmpty(c)).Distinct().OrderBy(c => c).ToList();
            var classList = new List<string> { "Tümü" };
            classList.AddRange(classes);
            AvailableClasses = new ObservableCollection<string>(classList);
            
            // Seçimi tümü yapıp filtrelemeyi tetikle
            SelectedClass = "Tümü"; 
            
            BuildTopicChart(topicList);
            BuildQuestionChart(questionList);

            // Yeni Grafikleri Oluştur (Summary null olabilir → güvenli kullan)
            BuildScoreDistributionHistogram(studentList, Summary?.MaxPossibleScore ?? 0);
            BuildSuccessRateDoughnut(Summary?.AverageSuccessRate ?? 0);
            BuildRadarChart(topicList);
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

    private void BuildTopicChart(List<TopicPerformanceDto> topics)
    {
        TopicChartSeries = new ISeries[]
        {
            new ColumnSeries<double>
            {
                Name = "Başarı %",
                Values = topics.Select(t => t.SuccessRate).ToArray(),
                Fill = new SolidColorPaint(SKColor.Parse("#2563EB"))
            }
        };

        TopicChartXAxes = new[]
        {
            new Axis
            {
                Labels = topics.Select(t => $"H{t.WeekNumber}").ToArray(),
                LabelsRotation = 0
            }
        };

        TopicChartYAxes = new[]
        {
            new Axis
            {
                Name = "Başarı %",
                MinLimit = 0,
                MaxLimit = 100
            }
        };
    }

    private void BuildQuestionChart(List<QuestionAnalysisDto> questions)
    {
        QuestionSuccessSeries = new ISeries[]
        {
            new ColumnSeries<double>
            {
                Name = "Başarı %",
                Values = questions.Select(q => q.SuccessRate).ToArray(),
                Fill = new SolidColorPaint(SKColor.Parse("#10B981"))
            }
        };

        QuestionXAxes = new[]
        {
            new Axis
            {
                Labels = questions.Select(q => $"S{q.QuestionNumber}").ToArray()
            }
        };

        QuestionYAxes = new[]
        {
            new Axis
            {
                Name = "Başarı %",
                MinLimit = 0,
                MaxLimit = 100
            }
        };
    }

    private void BuildScoreDistributionHistogram(List<StudentPerformanceDto> students, decimal maxScore)
    {
        if (students.Count == 0 || maxScore <= 0) return;

        // Basit bir 5-bin histogramı oluştur: 0-20%, 20-40%, 40-60%, 60-80%, 80-100%
        int[] bins = new int[5];
        foreach (var s in students)
        {
            double pct = (double)(s.TotalScore / maxScore) * 100;
            if (pct < 20) bins[0]++;
            else if (pct < 40) bins[1]++;
            else if (pct < 60) bins[2]++;
            else if (pct < 80) bins[3]++;
            else bins[4]++;
        }

        ScoreDistributionSeries = new ISeries[]
        {
            new ColumnSeries<int>
            {
                Name = "Öğrenci Sayısı",
                Values = bins,
                Fill = new SolidColorPaint(SKColor.Parse("#8B5CF6")) // Mor renk
            }
        };

        ScoreDistributionXAxes = new[]
        {
            new Axis
            {
                Labels = new[] { "0-20%", "20-40%", "40-60%", "60-80%", "80-100%" }
            }
        };
    }

    private void BuildSuccessRateDoughnut(double avgSuccess)
    {
        SuccessRateDoughnutSeries = new ISeries[]
        {
            new PieSeries<double> { Values = new double[] { avgSuccess }, Name = "Başarı", Fill = new SolidColorPaint(SKColor.Parse("#10B981")) },
            new PieSeries<double> { Values = new double[] { 100 - avgSuccess }, Name = "Kayıp", Fill = new SolidColorPaint(SKColor.Parse("#EF4444")) }
        };
    }

    private void BuildRadarChart(List<TopicPerformanceDto> topics)
    {
        if (topics.Count == 0) return;

        RadarSeries = new ISeries[]
        {
            new PolarLineSeries<double>
            {
                Values = topics.Select(t => t.SuccessRate).ToArray(),
                Name = "Sınıf Ortalaması",
                LineSmoothness = 0,
                GeometrySize = 10,
                Fill = new SolidColorPaint(SKColor.Parse("#3B82F6").WithAlpha(90)),
                Stroke = new SolidColorPaint(SKColor.Parse("#3B82F6")) { StrokeThickness = 2 }
            }
        };

        RadarAxes = new PolarAxis[]
        {
            new PolarAxis
            {
                Labels = topics.Select(t => $"Hafta {t.WeekNumber}").ToArray()
            }
        };
    }

    partial void OnSelectedClassChanged(string? value)
    {
        FilterStudents();
    }

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
