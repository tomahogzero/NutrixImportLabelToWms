namespace NutrixSyncLabelToWms.Core;

public sealed class EpicorSettings
{
    public string BaseUrl { get; set; } = string.Empty;
    public string Company { get; set; } = "NUTRIX";
    public string Plant { get; set; } = "00001";
    public string WarehouseCode { get; set; } = "PDO11";
    public string XApiKey { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public sealed class SqlSettings
{
    public string ConnectionString { get; set; } = string.Empty;
}

public sealed class UserSettings
{
    public EpicorSettings Epicor { get; set; } = new();
    public SqlSettings Sql { get; set; } = new();
}
