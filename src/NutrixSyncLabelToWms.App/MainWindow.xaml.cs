using System.Windows;
using NutrixSyncLabelToWms.App.ViewModels;
using NutrixSyncLabelToWms.Core;

namespace NutrixSyncLabelToWms.App;

public partial class MainWindow : Window
{
    private readonly MainViewModel _mainVm;
    private readonly SettingsViewModel _settingsVm;

    public MainWindow(MainViewModel mainVm, SettingsViewModel settingsVm)
    {
        InitializeComponent();
        _mainVm = mainVm;
        _settingsVm = settingsVm;
        DataContext = _mainVm;
    }

    public Task InitializeAsync(UserSettings settings)
    {
        _settingsVm.SetSettings(settings);
        return Task.CompletedTask;
    }
}
