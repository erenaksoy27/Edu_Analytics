using System.Windows;
using System.Windows.Media.Animation;

namespace EduAnalytics.UI.Views;

public partial class SplashWindow : Window
{
    public SplashWindow()
    {
        InitializeComponent();
    }

    public Task FadeOutAndCloseAsync()
    {
        var tcs = new TaskCompletionSource();
        var animation = new DoubleAnimation(0, TimeSpan.FromMilliseconds(200))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
        };
        animation.Completed += (_, _) => { Close(); tcs.SetResult(); };
        BeginAnimation(OpacityProperty, animation);
        return tcs.Task;
    }
}
