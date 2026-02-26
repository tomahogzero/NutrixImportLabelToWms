namespace NutrixSyncLabelToWms.Core;

public interface IQrParser
{
    LabelPayload Parse(string rawText);
}

public interface IEpicorBaqClient
{
    Task<EpicorQueryResult> QueryPartLotOnHandAsync(string partNum, string? lotNum, string warehouseCode, CancellationToken ct = default);
    Task<(bool Success, string Message)> TestConnectionAsync(CancellationToken ct = default);
}

public interface IScanRepository
{
    Task EnsureTablesAsync(CancellationToken ct = default);
    Task InsertScanAsync(ScanTransaction tx, CancellationToken ct = default);
    Task<IReadOnlyList<ScanTransaction>> GetHistoryAsync(HistoryFilter filter, CancellationToken ct = default);
    Task<(bool Success, string Message)> TestConnectionAsync(CancellationToken ct = default);
}

public interface IUserSettingsStore
{
    Task<UserSettings> LoadAsync(CancellationToken ct = default);
    Task SaveAsync(UserSettings settings, CancellationToken ct = default);
}

public interface IValidationService
{
    Task<ValidationResult> ValidateAsync(LabelPayload payload, UserSettings settings, CancellationToken ct = default);
}
