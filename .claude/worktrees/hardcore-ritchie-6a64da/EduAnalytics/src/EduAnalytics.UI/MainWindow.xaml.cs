using System.Windows;
using EduAnalytics.UI.ViewModels;

namespace EduAnalytics.UI;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
