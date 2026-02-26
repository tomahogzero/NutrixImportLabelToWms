using System.Net;
using System.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NutrixSyncLabelToWms.App.Services;
using NutrixSyncLabelToWms.App.ViewModels;
using NutrixSyncLabelToWms.Core;
using NutrixSyncLabelToWms.Infrastructure;

namespace NutrixSyncLabelToWms.App;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = default!;
    private static UserSettings _cachedSettings = new();

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .Build();

        var sc = new ServiceCollection();
        sc.AddSingleton<IConfiguration>(configuration);
        sc.AddLogging(b => b.AddConsole());

        sc.AddSingleton<AppState>();
        sc.AddSingleton<IUserSettingsStore, JsonUserSettingsStore>();
        sc.AddSingleton<Func<UserSettings>>(_ => () => _cachedSettings);
        sc.AddSingleton<IQrParser, FlexibleQrParser>();
        sc.AddSingleton<IScanRepository, SqlScanRepository>();

        sc.AddHttpClient<IEpicorBaqClient, EpicorBaqClient>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(20);
            client.DefaultRequestVersion = HttpVersion.Version20;
            client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher;
        });

        sc.AddSingleton<IValidationService, ValidationService>();

        sc.AddSingleton<MainViewModel>();
        sc.AddSingleton<ScanViewModel>();
        sc.AddSingleton<HistoryViewModel>();
        sc.AddSingleton<SettingsViewModel>();
        sc.AddSingleton<MainWindow>();

        Services = sc.BuildServiceProvider();

        var settingsStore = Services.GetRequiredService<IUserSettingsStore>();
        _cachedSettings = await settingsStore.LoadAsync();
        Services.GetRequiredService<AppState>().Settings = _cachedSettings;

        var window = Services.GetRequiredService<MainWindow>();
        await window.InitializeAsync(_cachedSettings);
        window.Show();
    }

    public static void UpdateSettingsCache(UserSettings settings)
    {
        _cachedSettings = settings;
        Services.GetRequiredService<AppState>().Settings = settings;
    }
}
