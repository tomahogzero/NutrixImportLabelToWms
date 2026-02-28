using NutrixSyncLabelToWms.Core;

namespace NutrixSyncLabelToWms.App.Services;

public sealed class AppState
{
    public UserSettings Settings { get; set; } = new();
}
