using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Input;
using ZLFileRelay.ConfigTool.Services;

namespace ZLFileRelay.ConfigTool.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly ConfigurationService _configurationService;
    
    [ObservableProperty]
    private string _currentPage = "ServiceManagement";

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private bool _hasUnsavedChanges;

    public MainViewModel(ConfigurationService configurationService)
    {
        _configurationService = configurationService;
    }

    [RelayCommand]
    private async Task SaveConfigurationAsync()
    {
        try
        {
            var config = _configurationService.CurrentConfiguration;
            if (config == null)
            {
                StatusMessage = "No configuration to save";
                return;
            }

            var success = await _configurationService.SaveAsync(config);
            if (success)
            {
                StatusMessage = "Configuration saved successfully";
                HasUnsavedChanges = false;
            }
            else
            {
                StatusMessage = "Failed to save configuration";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error saving configuration: {ex.Message}";
        }
    }

    [RelayCommand]
    private void NavigateTo(string page)
    {
        CurrentPage = page;
    }
}

