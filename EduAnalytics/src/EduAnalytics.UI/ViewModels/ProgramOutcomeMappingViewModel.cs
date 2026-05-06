using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EduAnalytics.Business.Dtos;
using EduAnalytics.Business.Services.Interfaces;
using EduAnalytics.Core.Entities;

namespace EduAnalytics.UI.ViewModels;

public class MatrixRow
{
    public string RowLabel { get; set; } = string.Empty;
    public int? P1 { get; set; }
    public int? P2 { get; set; }
    public int? P3 { get; set; }
    public int? P4 { get; set; }
    public int? P5 { get; set; }
    public int? P6 { get; set; }
    public int? P7 { get; set; }
    public int? P8 { get; set; }
    public int? P9 { get; set; }
    public int? P10 { get; set; }
    public int? P11 { get; set; }
    public int? P12 { get; set; }
    public int? P13 { get; set; }
    public int? P14 { get; set; }
    public int? P15 { get; set; }
}

/// <summary>
/// Program Çıktısı (PÇ) ↔ Ders Çıktısı (ÖÇ) eşleştirme ekranı.
/// Soldan PÇ seç, sağda eşleşmiş ÖÇ'leri gör + bağlanabilecek ÖÇ'ler havuzından ekle / kaldır.
/// </summary>
public partial class ProgramOutcomeMappingViewModel : ObservableObject
{
    private readonly IProgramOutcomeService _service;

    [ObservableProperty] private ObservableCollection<Program> _programs = new();
    [ObservableProperty] private Program? _selectedProgram;

    [ObservableProperty] private ObservableCollection<ProgramOutcomeDto> _programOutcomes = new();
    [ObservableProperty] private ProgramOutcomeDto? _selectedProgramOutcome;

    [ObservableProperty] private ObservableCollection<MappedLearningOutcomeDto> _mappedOutcomes = new();
    [ObservableProperty] private ObservableCollection<LearningOutcomeDto> _availableOutcomes = new();
    [ObservableProperty] private LearningOutcomeDto? _outcomeToAdd;
    [ObservableProperty] private int _newContributionLevel = 3;

    [ObservableProperty] private ObservableCollection<MatrixRow> _matrixRows = new();

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private string? _successMessage;

    public ProgramOutcomeMappingViewModel(IProgramOutcomeService service)
    {
        _service = service;
    }

    public async Task LoadAsync()
    {
        IsLoading = true;
        ErrorMessage = null;
        SuccessMessage = null;
        try
        {
            var programs = await _service.GetProgramsAsync();
            Programs = new ObservableCollection<Program>(programs);
            if (SelectedProgram == null && Programs.Count > 0)
                SelectedProgram = Programs[0];
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Programlar yüklenemedi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    partial void OnSelectedProgramChanged(Program? value)
    {
        _ = ReloadProgramOutcomesAsync();
    }

    partial void OnSelectedProgramOutcomeChanged(ProgramOutcomeDto? value)
    {
        _ = ReloadMappingsAsync();
    }

    private async Task ReloadProgramOutcomesAsync()
    {
        if (SelectedProgram == null) return;
        try
        {
            var po = await _service.GetByProgramAsync(SelectedProgram.Id);
            ProgramOutcomes = new ObservableCollection<ProgramOutcomeDto>(po);

            var available = await _service.GetAllLearningOutcomesInProgramAsync(SelectedProgram.Id);
            AvailableOutcomes = new ObservableCollection<LearningOutcomeDto>(available);

            SelectedProgramOutcome = ProgramOutcomes.FirstOrDefault();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"PÇ listesi yüklenemedi: {ex.Message}";
        }
    }

    private async Task ReloadMappingsAsync()
    {
        if (SelectedProgramOutcome == null)
        {
            MappedOutcomes = new ObservableCollection<MappedLearningOutcomeDto>();
            return;
        }
        try
        {
            var mappings = await _service.GetMappedLearningOutcomesAsync(SelectedProgramOutcome.Id);
            MappedOutcomes = new ObservableCollection<MappedLearningOutcomeDto>(mappings);

            // Tüm program çıktılarına göre matrix değerlerini topla ve yansıt
            if (SelectedProgram != null)
            {
               await RegenerateMatrixAsync(SelectedProgram.Id);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Eşleştirmeler yüklenemedi: {ex.Message}";
        }
    }

    private async Task RegenerateMatrixAsync(int programId)
    {
        var allOutcomes = await _service.GetByProgramAsync(programId);
        
        var matrixList = new List<MatrixRow>();
        
        // ÖÇ koduna (Ö1, Ö2...) göre gruplandır
        var ocMappings = new Dictionary<string, MatrixRow>();
        
        foreach (var po in allOutcomes)
        {
            var mappingsForPo = await _service.GetMappedLearningOutcomesAsync(po.Id);
            foreach(var mapping in mappingsForPo)
            {
                if(!ocMappings.ContainsKey(mapping.Code))
                {
                   ocMappings[mapping.Code] = new MatrixRow { RowLabel = mapping.Code };
                }

                var row = ocMappings[mapping.Code];
                if (po.Code == "P1") row.P1 = mapping.ContributionLevel;
                else if (po.Code == "P2") row.P2 = mapping.ContributionLevel;
                else if (po.Code == "P3") row.P3 = mapping.ContributionLevel;
                else if (po.Code == "P4") row.P4 = mapping.ContributionLevel;
                else if (po.Code == "P5") row.P5 = mapping.ContributionLevel;
                else if (po.Code == "P6") row.P6 = mapping.ContributionLevel;
                else if (po.Code == "P7") row.P7 = mapping.ContributionLevel;
                else if (po.Code == "P8") row.P8 = mapping.ContributionLevel;
                else if (po.Code == "P9") row.P9 = mapping.ContributionLevel;
                else if (po.Code == "P10") row.P10 = mapping.ContributionLevel;
                else if (po.Code == "P11") row.P11 = mapping.ContributionLevel;
                else if (po.Code == "P12") row.P12 = mapping.ContributionLevel;
                else if (po.Code == "P13") row.P13 = mapping.ContributionLevel;
                else if (po.Code == "P14") row.P14 = mapping.ContributionLevel;
                else if (po.Code == "P15") row.P15 = mapping.ContributionLevel;
            }
        }

        foreach(var item in ocMappings.Values.OrderBy(v => v.RowLabel))
        {
            matrixList.Add(item);
        }
        
        MatrixRows = new ObservableCollection<MatrixRow>(matrixList);
    }

    [RelayCommand]
    private async Task LinkOutcomeAsync()
    {
        if (SelectedProgramOutcome == null || OutcomeToAdd == null)
        {
            ErrorMessage = "PÇ ve ÖÇ seçilmeli.";
            return;
        }

        try
        {
            await _service.LinkToLearningOutcomeAsync(
                SelectedProgramOutcome.Id,
                OutcomeToAdd.Id,
                Math.Clamp(NewContributionLevel, 1, 5));

            SuccessMessage = $"✓ ÖÇ '{OutcomeToAdd.Code}' bağlandı (katkı: {NewContributionLevel}).";
            OutcomeToAdd = null;
            await ReloadMappingsAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Bağlama hatası: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task UnlinkOutcomeAsync(MappedLearningOutcomeDto? mapping)
    {
        if (mapping == null || SelectedProgramOutcome == null) return;

        var confirm = System.Windows.MessageBox.Show(
            $"'{mapping.Code} — {mapping.Name}' eşleştirmesini kaldırmak istediğinize emin misiniz?",
            "Eşleştirme Kaldır",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question);
        if (confirm != System.Windows.MessageBoxResult.Yes) return;

        try
        {
            await _service.UnlinkFromLearningOutcomeAsync(SelectedProgramOutcome.Id, mapping.LearningOutcomeId);
            SuccessMessage = "Eşleştirme kaldırıldı.";
            await ReloadMappingsAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Kaldırma hatası: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task UpdateContributionAsync(MappedLearningOutcomeDto? mapping)
    {
        if (mapping == null || SelectedProgramOutcome == null) return;
        try
        {
            await _service.LinkToLearningOutcomeAsync(
                SelectedProgramOutcome.Id,
                mapping.LearningOutcomeId,
                Math.Clamp(mapping.ContributionLevel, 1, 5));

            SuccessMessage = $"✓ Katkı seviyesi güncellendi.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Güncelleme hatası: {ex.Message}";
        }
    }
}
