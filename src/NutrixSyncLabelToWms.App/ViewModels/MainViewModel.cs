namespace NutrixSyncLabelToWms.App.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private object? _currentView;

    public object? CurrentView
    {
        get => _currentView;
        set => Set(ref _currentView, value);
    }

    public RelayCommand ShowScanCommand { get; }
    public RelayCommand ShowHistoryCommand { get; }
    public RelayCommand ShowSettingsCommand { get; }

    public MainViewModel(ScanViewModel scanVm, HistoryViewModel historyVm, SettingsViewModel settingsVm)
    {
        ShowScanCommand = new RelayCommand(() => CurrentView = scanVm);
        ShowHistoryCommand = new RelayCommand(() => CurrentView = historyVm);
        ShowSettingsCommand = new RelayCommand(() => CurrentView = settingsVm);
        CurrentView = scanVm;
    }
}
