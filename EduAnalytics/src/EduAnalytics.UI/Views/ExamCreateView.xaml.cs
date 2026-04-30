using System.Windows.Controls;

namespace EduAnalytics.UI.Views;

public partial class ExamCreateView : UserControl
{
    public ExamCreateView()
    {
        InitializeComponent();
        this.Loaded += ExamCreateView_Loaded;
    }

    private async void ExamCreateView_Loaded(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is ViewModels.ExamCreateViewModel vm)
        {
            await vm.LoadAsync();
        }
    }
}
