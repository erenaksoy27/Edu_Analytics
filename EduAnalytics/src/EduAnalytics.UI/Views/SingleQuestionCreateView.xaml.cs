using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using EduAnalytics.Business.Dtos;
using EduAnalytics.UI.ViewModels;

namespace EduAnalytics.UI.Views;

public partial class SingleQuestionCreateView : UserControl
{
    public SingleQuestionCreateView()
    {
        InitializeComponent();
    }

    private void AddSelectedLearningOutcomes_Click(object sender, RoutedEventArgs e)
    {
        if (LoListBox == null) return;
        var qvm = LoListBox.DataContext as QuestionEditViewModel;
        if (qvm == null) return;

        var items = LoListBox.SelectedItems.OfType<LearningOutcomeDto>().ToList();
        foreach (var lo in items)
        {
            if (!qvm.SelectedLearningOutcomes.Any(s => s.Id == lo.Id))
                qvm.SelectedLearningOutcomes.Add(lo);
        }
    }

    private void RemoveSelectedLearningOutcomes_Click(object sender, RoutedEventArgs e)
    {
        if (LoListBox == null) return;
        var qvm = LoListBox.DataContext as QuestionEditViewModel;
        if (qvm == null) return;

        var items = LoListBox.SelectedItems.OfType<LearningOutcomeDto>().ToList();
        foreach (var lo in items)
        {
            var existing = qvm.SelectedLearningOutcomes.FirstOrDefault(s => s.Id == lo.Id);
            if (existing != null)
                qvm.SelectedLearningOutcomes.Remove(existing);
        }
    }

    private void LoFilterTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (LoListBox == null) return;
        var qvm = LoListBox.DataContext as QuestionEditViewModel;
        if (qvm == null) return;

        var filter = LoFilterTextBox.Text?.Trim() ?? string.Empty;
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

    private void LoCheckBox_Checked(object sender, RoutedEventArgs e)
    {
        var cb = sender as CheckBox;
        var lo = cb?.DataContext as LearningOutcomeDto;
        if (lo == null) return;
        if (LoListBox == null) return;
        var qvm = LoListBox.DataContext as QuestionEditViewModel;
        if (qvm == null) return;

        if (!qvm.SelectedLearningOutcomes.Any(s => s.Id == lo.Id))
            qvm.SelectedLearningOutcomes.Add(lo);
    }

    private void LoCheckBox_Unchecked(object sender, RoutedEventArgs e)
    {
        var cb = sender as CheckBox;
        var lo = cb?.DataContext as LearningOutcomeDto;
        if (lo == null) return;
        if (LoListBox == null) return;
        var qvm = LoListBox.DataContext as QuestionEditViewModel;
        if (qvm == null) return;

        var existing = qvm.SelectedLearningOutcomes.FirstOrDefault(s => s.Id == lo.Id);
        if (existing != null)
            qvm.SelectedLearningOutcomes.Remove(existing);
    }
}