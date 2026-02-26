using NutrixSyncLabelToWms.App.Services;
using NutrixSyncLabelToWms.Core;

namespace NutrixSyncLabelToWms.App.ViewModels;

public sealed class SettingsViewModel : ObservableObject
{
    private readonly IUserSettingsStore _store;
    private readonly IEpicorBaqClient _epicor;
    private readonly IScanRepository _repo;
    private readonly AppState _appState;

    public UserSettings Settings { get; private set; } = new();

    private string _message = "";
    public string Message { get => _message; set => Set(ref _message, value); }

    public AsyncRelayCommand SaveCommand { get; }
    public AsyncRelayCommand ReloadCommand { get; }
    public AsyncRelayCommand TestEpicorCommand { get; }
    public AsyncRelayCommand TestSqlCommand { get; }

    public SettingsViewModel(IUserSettingsStore store, IEpicorBaqClient epicor, IScanRepository repo, AppState appState)
    {
        _store = store;
        _epicor = epicor;
        _repo = repo;
        _appState = appState;

        SaveCommand = new AsyncRelayCommand(SaveAsync);
        ReloadCommand = new AsyncRelayCommand(ReloadAsync);
        TestEpicorCommand = new AsyncRelayCommand(TestEpicorAsync);
        TestSqlCommand = new AsyncRelayCommand(TestSqlAsync);
    }

    public void SetSettings(UserSettings settings)
    {
        Settings = settings;
        _appState.Settings = settings;
        Raise(nameof(Settings));
    }

    private async Task SaveAsync()
    {
        await _store.SaveAsync(Settings);
        _appState.Settings = Settings;
        App.UpdateSettingsCache(Settings);
        Message = "บันทึก settings สำเร็จ";
    }

    private async Task ReloadAsync()
    {
        var loaded = await _store.LoadAsync();
        SetSettings(loaded);
        Message = "โหลด settings สำเร็จ";
    }

    private async Task TestEpicorAsync()
    {
        var r = await _epicor.TestConnectionAsync();
        Message = r.Message;
    }

    private async Task TestSqlAsync()
    {
        var r = await _repo.TestConnectionAsync();
        Message = r.Message;
    }
}
