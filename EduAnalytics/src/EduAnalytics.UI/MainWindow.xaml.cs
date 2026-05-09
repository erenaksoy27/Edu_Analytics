using System.Windows;
using EduAnalytics.UI.Services;
using EduAnalytics.UI.ViewModels;
using Material.Icons;

namespace EduAnalytics.UI;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void ToggleTheme_Click(object sender, RoutedEventArgs e)
    {
        var svc = App.Services.GetRequiredService<IThemeService>();
        svc.CycleTheme();
        UpdateThemeIcon(svc.CurrentTheme);
    }

    private void UpdateThemeIcon(ThemeMode mode)
    {
        ThemeIcon.Kind = mode switch
        {
            ThemeMode.Dark   => MaterialIconKind.WeatherNight,
            ThemeMode.System => MaterialIconKind.ThemeLightDark,
            _                => MaterialIconKind.WeatherSunny
        };
        ThemeToggleBtn.ToolTip = mode switch
        {
            ThemeMode.Dark   => "Tema: Koyu",
            ThemeMode.System => "Tema: Sistem (otomatik)",
            _                => "Tema: Açık"
        };
    }

    private void MinimizeWindow(object sender, RoutedEventArgs e)
        => WindowState = WindowState.Minimized;

    private void ToggleMaximizeWindow(object sender, RoutedEventArgs e)
        => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

    private void CloseWindow(object sender, RoutedEventArgs e)
        => Close();
}
