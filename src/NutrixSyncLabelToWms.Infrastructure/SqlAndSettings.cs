using System.Data;
using System.Text.Json;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using NutrixSyncLabelToWms.Core;

namespace NutrixSyncLabelToWms.Infrastructure;

public sealed class SqlScanRepository : IScanRepository
{
    private readonly Func<UserSettings> _settingsAccessor;

    public SqlScanRepository(Func<UserSettings> settingsAccessor)
    {
        _settingsAccessor = settingsAccessor;
    }

    private IDbConnection CreateConnection()
    {
        var cs = _settingsAccessor().Sql.ConnectionString;
        if (string.IsNullOrWhiteSpace(cs)) throw new InvalidOperationException("ยังไม่ได้ตั้งค่า SQL ConnectionString");
        return new SqlConnection(cs);
    }

    public async Task EnsureTablesAsync(CancellationToken ct = default)
    {
        const string sql = @"
IF OBJECT_ID('dbo.ScanTransactions', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ScanTransactions (
      Id uniqueidentifier NOT NULL PRIMARY KEY,
      ScanAt datetime2 NOT NULL,
      Plant nvarchar(10) NOT NULL,
      QrRawText nvarchar(max) NOT NULL,
      PartNum nvarchar(50) NULL,
      LotNum nvarchar(100) NULL,
      ExpireDate date NULL,
      ReceiveDate date NULL,
      QtyLabel decimal(18,3) NOT NULL,
      UomLabel nvarchar(20) NULL,
      QtyOnHand decimal(18,3) NULL,
      ValidationStatus nvarchar(10) NOT NULL,
      ValidationMessage nvarchar(255) NOT NULL,
      EpicorResponseJson nvarchar(max) NULL
    );
    CREATE INDEX IX_ScanTransactions_ScanAt ON dbo.ScanTransactions(ScanAt DESC);
END";

        using var conn = CreateConnection();
        await conn.ExecuteAsync(new CommandDefinition(sql, cancellationToken: ct));
    }

    public async Task InsertScanAsync(ScanTransaction tx, CancellationToken ct = default)
    {
        const string sql = @"
INSERT INTO dbo.ScanTransactions
(Id,ScanAt,Plant,QrRawText,PartNum,LotNum,ExpireDate,ReceiveDate,QtyLabel,UomLabel,QtyOnHand,ValidationStatus,ValidationMessage,EpicorResponseJson)
VALUES
(@Id,@ScanAt,@Plant,@QrRawText,@PartNum,@LotNum,@ExpireDate,@ReceiveDate,@QtyLabel,@UomLabel,@QtyOnHand,@ValidationStatus,@ValidationMessage,@EpicorResponseJson);";

        using var conn = CreateConnection();
        await conn.ExecuteAsync(new CommandDefinition(sql, tx, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<ScanTransaction>> GetHistoryAsync(HistoryFilter filter, CancellationToken ct = default)
    {
        var sql = @"
SELECT Id,ScanAt,Plant,QrRawText,PartNum,LotNum,ExpireDate,ReceiveDate,QtyLabel,UomLabel,QtyOnHand,ValidationStatus,ValidationMessage,EpicorResponseJson
FROM dbo.ScanTransactions
WHERE (@FromDate IS NULL OR ScanAt >= @FromDate)
  AND (@ToDate IS NULL OR ScanAt < DATEADD(day,1,@ToDate))
  AND (@PartNum IS NULL OR PartNum = @PartNum)
  AND (@LotNum IS NULL OR LotNum = @LotNum)
  AND (@Status IS NULL OR ValidationStatus = @Status)
ORDER BY ScanAt DESC";

        using var conn = CreateConnection();
        var rows = await conn.QueryAsync<ScanTransaction>(new CommandDefinition(sql, filter, cancellationToken: ct));
        return rows.ToList();
    }

    public async Task<(bool Success, string Message)> TestConnectionAsync(CancellationToken ct = default)
    {
        try
        {
            await using var conn = new SqlConnection(_settingsAccessor().Sql.ConnectionString);
            if (string.IsNullOrWhiteSpace(conn.ConnectionString))
            {
                throw new InvalidOperationException("ยังไม่ได้ตั้งค่า SQL ConnectionString");
            }

            await conn.OpenAsync(ct);
            await conn.ExecuteScalarAsync<int>(new CommandDefinition("SELECT 1", cancellationToken: ct));
            return (true, "เชื่อมต่อ SQL Server สำเร็จ");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }
}

public sealed class JsonUserSettingsStore : IUserSettingsStore
{
    private readonly IConfiguration _configuration;
    private readonly string _filePath;

    public JsonUserSettingsStore(IConfiguration configuration)
    {
        _configuration = configuration;
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var folder = Path.Combine(appData, "NutrixSyncLabelToWms");
        Directory.CreateDirectory(folder);
        _filePath = Path.Combine(folder, "user-settings.json");
    }

    public async Task<UserSettings> LoadAsync(CancellationToken ct = default)
    {
        var defaults = _configuration.GetSection("Defaults").Get<UserSettings>() ?? new UserSettings();
        if (!File.Exists(_filePath)) return defaults;

        await using var fs = File.OpenRead(_filePath);
        var user = await JsonSerializer.DeserializeAsync<UserSettings>(fs, cancellationToken: ct) ?? new UserSettings();

        user.Epicor.BaseUrl = string.IsNullOrWhiteSpace(user.Epicor.BaseUrl) ? defaults.Epicor.BaseUrl : user.Epicor.BaseUrl;
        user.Epicor.Company = string.IsNullOrWhiteSpace(user.Epicor.Company) ? defaults.Epicor.Company : user.Epicor.Company;
        user.Epicor.Plant = string.IsNullOrWhiteSpace(user.Epicor.Plant) ? defaults.Epicor.Plant : user.Epicor.Plant;
        user.Epicor.WarehouseCode = string.IsNullOrWhiteSpace(user.Epicor.WarehouseCode) ? defaults.Epicor.WarehouseCode : user.Epicor.WarehouseCode;
        return user;
    }

    public async Task SaveAsync(UserSettings settings, CancellationToken ct = default)
    {
        await using var fs = File.Create(_filePath);
        await JsonSerializer.SerializeAsync(fs, settings, new JsonSerializerOptions { WriteIndented = true }, ct);
    }
}
