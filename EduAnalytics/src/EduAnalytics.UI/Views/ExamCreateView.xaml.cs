using System.Windows.Controls;
using System.Windows;
using EduAnalytics.Business.Dtos;
using System.Linq;
using EduAnalytics.UI.ViewModels;
using System.Windows.Data;
using System;

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

    private void AddSelectedLearningOutcomes_Click(object sender, RoutedEventArgs e)
    {
        var button = sender as Button;
        if (button?.Tag is ListBox lb && lb.DataContext is QuestionEditViewModel qvm)
        {
            var items = lb.SelectedItems.OfType<LearningOutcomeDto>().ToList();
            foreach (var lo in items)
            {
                if (!qvm.SelectedLearningOutcomes.Any(s => s.Id == lo.Id))
                    qvm.SelectedLearningOutcomes.Add(lo);
            }
        }
    }

    private void RemoveSelectedLearningOutcomes_Click(object sender, RoutedEventArgs e)
    {
        var button = sender as Button;
        if (button?.Tag is ListBox lb && lb.DataContext is QuestionEditViewModel qvm)
        {
            var items = lb.SelectedItems.OfType<LearningOutcomeDto>().ToList();
            foreach (var lo in items)
            {
                var existing = qvm.SelectedLearningOutcomes.FirstOrDefault(s => s.Id == lo.Id);
                if (existing != null)
                    qvm.SelectedLearningOutcomes.Remove(existing);
            }
        }
    }

    private void LoFilterTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        var tb = sender as TextBox;
        if (tb?.Tag is ListBox lb && lb.DataContext is QuestionEditViewModel qvm)
        {
            var filter = tb.Text?.Trim() ?? string.Empty;
            var view = CollectionViewSource.GetDefaultView(qvm.AvailableLearningOutcomes);
            if (view == null) return;

            if (string.IsNullOrEmpty(filter))
            {
                view.Filter = null;
            }
            else
            {
                view.Filter = obj =>
                {
                    if (obj is LearningOutcomeDto lo)
                    {
                        return (lo.Name?.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
                               || (lo.Code?.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0);
                    }
                    return false;
                };
            }
            view.Refresh();
        }
    }

    private void LoCheckBox_Checked(object sender, RoutedEventArgs e)
    {
        var cb = sender as CheckBox;
        var lo = cb?.DataContext as LearningOutcomeDto;
        if (lo == null) return;

        // Find the templated parent ListBox items
        DependencyObject parent = cb;
        while (parent != null && !(parent is ListBoxItem))
            parent = System.Windows.Media.VisualTreeHelper.GetParent(parent);

        if (parent is ListBoxItem lbi && ItemsControl.ItemsControlFromItemContainer(lbi) is ListBox lb && lb.DataContext is QuestionEditViewModel qvm)
        {
            if (!qvm.SelectedLearningOutcomes.Any(s => s.Id == lo.Id))
                qvm.SelectedLearningOutcomes.Add(lo);
        }
    }

    private void LoCheckBox_Unchecked(object sender, RoutedEventArgs e)
    {
        var cb = sender as CheckBox;
        var lo = cb?.DataContext as LearningOutcomeDto;
        if (lo == null) return;

        DependencyObject parent = cb;
        while (parent != null && !(parent is ListBoxItem))
            parent = System.Windows.Media.VisualTreeHelper.GetParent(parent);

        if (parent is ListBoxItem lbi && ItemsControl.ItemsControlFromItemContainer(lbi) is ListBox lb && lb.DataContext is QuestionEditViewModel qvm)
        {
            var existing = qvm.SelectedLearningOutcomes.FirstOrDefault(s => s.Id == lo.Id);
            if (existing != null)
                qvm.SelectedLearningOutcomes.Remove(existing);
        }
    }

}
