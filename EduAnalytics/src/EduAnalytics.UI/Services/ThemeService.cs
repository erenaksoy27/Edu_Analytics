using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;

namespace EduAnalytics.UI.Services;

public enum ThemeMode { Light, Dark, System }

public interface IThemeService
{
    ThemeMode CurrentTheme { get; }
    event Action ThemeChanged;
    void Apply(ThemeMode mode);
    void CycleTheme();
    void LoadAndApply();
    SKColorPalette GetChartPalette();
}

/// <summary>SkiaSharp chart renk paleti — tema bilincinde.</summary>
public record SKColorPalette(
    string Primary,
    string Success,
    string Warning,
    string Danger,
    string Info,
    string Purple,
    string Cyan,
    string Muted
);

public class ThemeService : IThemeService
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "EduAnalytics", "settings.json");

    public ThemeMode CurrentTheme { get; private set; } = ThemeMode.System;
    public event Action? ThemeChanged;

    public ThemeService()
    {
        SystemEvents.UserPreferenceChanged += OnSystemPreferenceChanged;
    }

    // ─── Public API ──────────────────────────────────────────────────────

    public void LoadAndApply()
    {
        var saved = LoadSaved();
        Apply(saved);
    }

    public void Apply(ThemeMode mode)
    {
        CurrentTheme = mode;
        var effective = mode == ThemeMode.System ? DetectSystemTheme() : mode;
        SwapBrushColors(effective);
        SaveSettings(mode);
        ThemeChanged?.Invoke();
    }

    public void CycleTheme()
    {
        var next = CurrentTheme switch
        {
            ThemeMode.Light  => ThemeMode.Dark,
            ThemeMode.Dark   => ThemeMode.System,
            _                => ThemeMode.Light
        };
        Apply(next);
    }

    public SKColorPalette GetChartPalette()
    {
        var effective = CurrentTheme == ThemeMode.System ? DetectSystemTheme() : CurrentTheme;
        return effective == ThemeMode.Dark
            ? new SKColorPalette("#818CF8", "#34D399", "#FBBF24", "#F87171",
                                 "#60A5FA", "#A78BFA", "#67E8F9", "#64748B")
            : new SKColorPalette("#4F46E5", "#059669", "#D97706", "#E11D48",
                                 "#2563EB", "#8B5CF6", "#06B6D4", "#9CA3AF");
    }

    // ─── Internal ────────────────────────────────────────────────────────

    private static ThemeMode DetectSystemTheme()
    {
        try
        {
            var val = Registry.GetValue(
                @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize",
                "AppsUseLightTheme", 1);
            return val is 0 ? ThemeMode.Dark : ThemeMode.Light;
        }
        catch { return ThemeMode.Light; }
    }

    private static void SwapBrushColors(ThemeMode effective)
    {
        var source = effective == ThemeMode.Dark ? "Themes/Dark.xaml" : "Themes/Light.xaml";
        var dict = new ResourceDictionary
        {
            Source = new Uri($"pack://application:,,,/EduAnalytics.UI;component/{source}")
        };

        foreach (var key in dict.Keys)
        {
            if (dict[key] is SolidColorBrush src &&
                Application.Current.Resources[key] is SolidColorBrush target)
            {
                target.Color = src.Color;
            }
        }
    }

    private void OnSystemPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        if (CurrentTheme == ThemeMode.System &&
            e.Category == UserPreferenceCategory.General)
        {
            Application.Current.Dispatcher.Invoke(() => Apply(ThemeMode.System));
        }
    }

    // ─── Persist ─────────────────────────────────────────────────────────

    private static ThemeMode LoadSaved()
    {
        try
        {
            if (!File.Exists(SettingsPath)) return ThemeMode.System;
            var json = File.ReadAllText(SettingsPath);
            var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("theme", out var val) &&
                Enum.TryParse<ThemeMode>(val.GetString(), out var mode))
                return mode;
        }
        catch { }
        return ThemeMode.System;
    }

    private static void SaveSettings(ThemeMode mode)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
            var json = JsonSerializer.Serialize(new { theme = mode.ToString() });
            File.WriteAllText(SettingsPath, json);
        }
        catch { }
    }
}
