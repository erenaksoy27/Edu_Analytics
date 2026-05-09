using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EduAnalytics.Business.Dtos;
using EduAnalytics.Business.Services.Interfaces;
using EduAnalytics.Core.Entities;

namespace EduAnalytics.UI.ViewModels;

public partial class ProgramOutcomeManagementViewModel : ObservableObject
{
    private readonly IProgramOutcomeService _poService;
    private readonly SemaphoreSlim _opLock = new(1, 1);

    [ObservableProperty] private ObservableCollection<Program> _programs = new();
    [ObservableProperty] private Program? _selectedProgram;
    [ObservableProperty] private ObservableCollection<ProgramOutcomeDto> _programOutcomes = new();

    [ObservableProperty] private string _formCode = string.Empty;
    [ObservableProperty] private string _formDescription = string.Empty;
    [ObservableProperty] private ProgramOutcomeDto? _editingOutcome;

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private string? _successMessage;

    public ProgramOutcomeManagementViewModel(IProgramOutcomeService poService)
    {
        _poService = poService;
    }

    public async Task LoadAsync()
    {
        IsLoading = true;
        ErrorMessage = null;
        SuccessMessage = null;

        try
        {
            var programs = await _poService.GetProgramsAsync();
            Programs = new ObservableCollection<Program>(programs);

            if (SelectedProgram == null && programs.Count > 0)
                SelectedProgram = programs[0];
            else
                await ReloadOutcomesAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Yükleme hatası: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    partial void OnSelectedProgramChanged(Program? value)
    {
        _ = ReloadOutcomesAsync();
    }

    private async Task ReloadOutcomesAsync()
    {
        if (SelectedProgram == null) return;
        await _opLock.WaitAsync();
        try
        {
            var list = await _poService.GetByProgramAsync(SelectedProgram.Id);
            ProgramOutcomes = new ObservableCollection<ProgramOutcomeDto>(list);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"PÇ listesi yüklenemedi: {ex.Message}";
        }
        finally
        {
            _opLock.Release();
        }
    }

    [RelayCommand]
    private void StartEdit(ProgramOutcomeDto? po)
    {
        if (po == null) return;
        EditingOutcome = po;
        FormCode = po.Code;
        FormDescription = po.Description;
    }

    [RelayCommand]
    private void StartNew()
    {
        EditingOutcome = null;
        FormCode = string.Empty;
        FormDescription = string.Empty;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (SelectedProgram == null)
        {
            ErrorMessage = "Önce program seçin.";
            return;
        }
        if (string.IsNullOrWhiteSpace(FormCode) || string.IsNullOrWhiteSpace(FormDescription))
        {
            ErrorMessage = "Kod ve açıklama zorunludur.";
            return;
        }

        try
        {
            var model = new ProgramOutcomeCreateModel
            {
                ProgramId = SelectedProgram.Id,
                Code = FormCode.Trim(),
                Description = FormDescription.Trim()
            };

            if (EditingOutcome == null)
            {
                await _poService.CreateAsync(model);
                SuccessMessage = "Yeni PÇ eklendi.";
            }
            else
            {
                await _poService.UpdateAsync(EditingOutcome.Id, model);
                SuccessMessage = "PÇ güncellendi.";
            }

            StartNew();
            await ReloadOutcomesAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    [RelayCommand]
    private async Task DeleteAsync(ProgramOutcomeDto? po)
    {
        if (po == null) return;
        var confirm = System.Windows.MessageBox.Show(
            $"{po.Code} silinsin mi? Bağlı ÖÇ eşleşmeleri de kaldırılacak.",
            "PÇ Sil",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question);

        if (confirm != System.Windows.MessageBoxResult.Yes) return;

        try
        {
            await _poService.DeleteAsync(po.Id);
            ProgramOutcomes.Remove(po);
            SuccessMessage = "PÇ silindi.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }
}
