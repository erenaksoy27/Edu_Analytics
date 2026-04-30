using System.Windows;
using EduAnalytics.Business.Services.Implementations;
using EduAnalytics.Business.Services.Interfaces;
using EduAnalytics.DataAccess.Context;
using EduAnalytics.DataAccess.Seed;
using EduAnalytics.UI.Services;
using EduAnalytics.UI.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EduAnalytics.UI;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var services = new ServiceCollection();
        ConfigureServices(services);
        Services = services.BuildServiceProvider();

        // İlk açılışta veritabanını oluştur ve seed uygula
        try
        {
            var ctx = Services.GetRequiredService<EduAnalyticsDbContext>();
            DbInitializer.Seed(ctx);
            ctx.Dispose();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Veritabanı başlatılamadı:\n\n{ex.GetType().Name}: {ex.Message}\n\n{ex.InnerException?.Message}",
                "Başlangıç Hatası",
                MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
            return;
        }

        var mainWindow = Services.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    private static void ConfigureServices(ServiceCollection services)
    {
        // DbContext - Transient: Her servis çağrısı kendi context'ini alır.
        // WPF masaüstü uygulamasında "Scope" kavramı yoktur, Transient en güvenli.
        services.AddDbContext<EduAnalyticsDbContext>(options =>
            options.UseSqlServer(
                "Server=(localdb)\\MSSQLLocalDB;" +
                "Database=EduAnalyticsDb;" +
                "Trusted_Connection=true;" +
                "TrustServerCertificate=true;"),
            ServiceLifetime.Transient);

        // Business Services - Transient
        // ── FAZ 1 servisleri ──
        services.AddTransient<IExamAnalysisService, ExamAnalysisService>();
        services.AddTransient<IDistractorAnalysisService, DistractorAnalysisService>();
        services.AddTransient<ITopicPerformanceService, TopicPerformanceService>();
        services.AddTransient<IStudentPerformanceService, StudentPerformanceService>();
        services.AddTransient<IExamCrudService, ExamCrudService>();
        services.AddTransient<IAnswerEntryService, AnswerEntryService>();
        services.AddTransient<ILearningOutcomePerformanceService, LearningOutcomePerformanceService>();

        // ── FAZ 2 servisleri (ASOS yenileme) ──
        services.AddTransient<IQuestionBankService, QuestionBankService>();
        services.AddTransient<ILearningOutcomeService, LearningOutcomeService>();
        services.AddTransient<IProgramOutcomeService, ProgramOutcomeService>();
        services.AddTransient<IExamBookletService, ExamBookletService>();
        services.AddTransient<IExamBalanceCheckService, ExamBalanceCheckService>();
        services.AddTransient<IExamCancellationService, ExamCancellationService>();

        // ViewModels
        services.AddTransient<MainViewModel>();
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<ExamAnalysisViewModel>();
        services.AddTransient<ExamCreateViewModel>();
        services.AddTransient<AnswerEntryViewModel>();

        // FAZ 3 — Yeni ASOS ekranları
        services.AddTransient<QuestionBankViewModel>();
        services.AddTransient<LearningOutcomeManagementViewModel>();
        services.AddTransient<ProgramOutcomeManagementViewModel>();
        services.AddTransient<ProgramOutcomeReportViewModel>();

        // FAZ 4 — İleri özellikler
        services.AddTransient<QuestionGroupEditorViewModel>();
        services.AddTransient<ExamFromBankViewModel>();
        services.AddTransient<SingleQuestionCreateViewModel>();

        // FAZ 5 — Rubric (klasik soru kriter-bazlı puanlama)
        services.AddTransient<IRubricService, RubricService>();
        services.AddTransient<RubricGradeDialogViewModel>();

        // UI altyapı: Toast bildirimleri (singleton — tüm uygulama aynı kuyruğu paylaşır)
        services.AddSingleton<ToastService>();

        // Windows
        services.AddTransient<MainWindow>();
    }
}
