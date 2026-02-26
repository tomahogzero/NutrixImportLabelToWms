using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using NutrixSyncLabelToWms.Core;

namespace NutrixSyncLabelToWms.Infrastructure;

public sealed class EpicorBaqClient : IEpicorBaqClient
{
    private readonly HttpClient _httpClient;
    private readonly Func<UserSettings> _settingsAccessor;
    private readonly ILogger<EpicorBaqClient> _logger;

    public EpicorBaqClient(HttpClient httpClient, Func<UserSettings> settingsAccessor, ILogger<EpicorBaqClient> logger)
    {
        _httpClient = httpClient;
        _settingsAccessor = settingsAccessor;
        _logger = logger;
    }

    public async Task<EpicorQueryResult> QueryPartLotOnHandAsync(string partNum, string? lotNum, string warehouseCode, CancellationToken ct = default)
    {
        var settings = _settingsAccessor();
        var ep = settings.Epicor;
        if (string.IsNullOrWhiteSpace(ep.BaseUrl)) throw new InvalidOperationException("ยังไม่ได้ตั้งค่า Epicor BaseUrl");

        var baseUrl = ep.BaseUrl.TrimEnd('/');
        var endpoint = $"{baseUrl}/api/v2/odata/{ep.Company}/BaqSvc/NTXzPartLotOnHand/Data";

        var filters = new List<string> { $"PartBin_PartNum eq '{Escape(partNum)}'" };
        if (!string.IsNullOrWhiteSpace(warehouseCode)) filters.Add($"PartBin_WarehouseCode eq '{Escape(warehouseCode)}'");
        if (!string.IsNullOrWhiteSpace(lotNum)) filters.Add($"PartBin_LotNum eq '{Escape(lotNum!)}'");
        var url = $"{endpoint}?$top=200&$count=true&$filter={Uri.EscapeDataString(string.Join(" and ", filters))}";

        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        AddHeaders(req, ep);

        using var res = await _httpClient.SendAsync(req, ct);
        var rawJson = await res.Content.ReadAsStringAsync(ct);
        if (!res.IsSuccessStatusCode)
        {
            var msg = BuildHttpErrorMessage(res.StatusCode, rawJson);
            throw new HttpRequestException(msg, null, res.StatusCode);
        }

        using var doc = JsonDocument.Parse(rawJson);
        var root = doc.RootElement;
        var count = root.TryGetProperty("@odata.count", out var c) ? c.GetInt32() : 0;
        var rows = new List<EpicorBaqRow>();

        if (root.TryGetProperty("value", out var value) && value.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in value.EnumerateArray())
            {
                rows.Add(new EpicorBaqRow
                {
                    PartNum = item.TryGetProperty("PartBin_PartNum", out var p) ? p.GetString() : null,
                    LotNum = item.TryGetProperty("PartBin_LotNum", out var l) ? l.GetString() : null,
                    WarehouseCode = item.TryGetProperty("PartBin_WarehouseCode", out var w) ? w.GetString() : null,
                    DimCode = item.TryGetProperty("PartBin_DimCode", out var d) ? d.GetString() : null,
                    OnHandQty = item.TryGetProperty("PartBin_OnhandQty", out var q) ? q.GetDecimal() : 0m,
                    ExpirationDate = item.TryGetProperty("PartLot_ExpirationDate", out var e) && e.ValueKind != JsonValueKind.Null ? e.GetDateTime() : null,
                });
            }
        }

        return new EpicorQueryResult { Count = count, Rows = rows, RawJson = rawJson };
    }

    public async Task<(bool Success, string Message)> TestConnectionAsync(CancellationToken ct = default)
    {
        try
        {
            var settings = _settingsAccessor();
            var samplePart = "RM11-O-PTM-001";
            await QueryPartLotOnHandAsync(samplePart, null, settings.Epicor.WarehouseCode, ct);
            return (true, "เชื่อมต่อ Epicor สำเร็จ");
        }
        catch (TaskCanceledException)
        {
            return (false, "Epicor timeout กรุณาตรวจสอบ URL หรือเครือข่าย");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Epicor test connection failed");
            return (false, ex.Message);
        }
    }

    private static string Escape(string input) => input.Replace("'", "''", StringComparison.Ordinal);

    private static void AddHeaders(HttpRequestMessage req, EpicorSettings ep)
    {
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        req.Headers.Add("X-API-Key", ep.XApiKey);
        req.Headers.Add("CallSettings", JsonSerializer.Serialize(new { Company = ep.Company, Plant = ep.Plant }));
        var basic = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{ep.Username}:{ep.Password}"));
        req.Headers.Authorization = new AuthenticationHeaderValue("Basic", basic);
    }

    private static string BuildHttpErrorMessage(HttpStatusCode statusCode, string rawJson)
        => statusCode switch
        {
            HttpStatusCode.Unauthorized => "Epicor Authentication ไม่ถูกต้อง (401)",
            HttpStatusCode.Forbidden => "Epicor API ถูกปฏิเสธสิทธิ์ (403)",
            HttpStatusCode.InternalServerError => $"Epicor เกิดข้อผิดพลาดภายใน (500): {rawJson}",
            _ => $"Epicor error {(int)statusCode}: {rawJson}"
        };
}

public sealed class ValidationService : IValidationService
{
    private readonly IEpicorBaqClient _epicor;

    public ValidationService(IEpicorBaqClient epicor) => _epicor = epicor;

    public async Task<ValidationResult> ValidateAsync(LabelPayload payload, UserSettings settings, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(payload.PartNum) || string.IsNullOrWhiteSpace(payload.LotNum))
        {
            return new ValidationResult { IsPass = false, Message = "กรุณาระบุ PartNum และ LotNum ก่อน Validate", LabelQty = payload.Qty };
        }

        var result = await _epicor.QueryPartLotOnHandAsync(payload.PartNum, payload.LotNum, settings.Epicor.WarehouseCode, ct);
        var matched = result.Rows
            .Where(x => string.Equals(x.PartNum, payload.PartNum, StringComparison.OrdinalIgnoreCase)
                     && string.Equals(x.LotNum, payload.LotNum, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (matched.Count == 0)
        {
            return new ValidationResult
            {
                IsPass = false,
                Message = "NOT_FOUND",
                LabelQty = payload.Qty,
                QtyOnHand = 0,
                MatchedRowsCount = 0,
                EpicorResponseJson = result.RawJson
            };
        }

        var onHand = matched.Sum(x => x.OnHandQty);
        var pass = onHand >= payload.Qty;

        return new ValidationResult
        {
            IsPass = pass,
            Message = pass ? "PASS" : "INSUFFICIENT_QTY",
            LabelQty = payload.Qty,
            QtyOnHand = onHand,
            MatchedRowsCount = matched.Count,
            EpicorResponseJson = result.RawJson
        };
    }
}
