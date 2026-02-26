using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using NutrixSyncLabelToWms.Core;

namespace NutrixSyncLabelToWms.Infrastructure;

public sealed partial class FlexibleQrParser : IQrParser
{
    private static readonly string[] PartKeys = ["part", "partnum", "part_no", "p"];
    private static readonly string[] LotKeys = ["lot", "lotnum", "lot_no", "l"];
    private static readonly string[] QtyKeys = ["qty", "quantity", "q"];
    private static readonly string[] UomKeys = ["uom", "unit"];
    private static readonly string[] ExpKeys = ["expiredate", "exp", "expiry"];
    private static readonly string[] RecKeys = ["receivedate", "receive", "rcvdate", "mfg"];

    public LabelPayload Parse(string rawText)
    {
        rawText ??= string.Empty;
        var payload = new LabelPayload { RawText = rawText.Trim() };

        if (TryParseJson(payload.RawText, payload)) return payload;
        if (TryParseKeyValue(payload.RawText, payload)) return payload;

        ParseRegexFallback(payload.RawText, payload);
        return payload;
    }

    private static bool TryParseJson(string raw, LabelPayload payload)
    {
        try
        {
            if (!raw.TrimStart().StartsWith('{')) return false;
            using var doc = JsonDocument.Parse(raw);
            var map = doc.RootElement.EnumerateObject().ToDictionary(x => x.Name.ToLowerInvariant(), x => x.Value.ToString());
            FillFromMap(map, payload);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool TryParseKeyValue(string raw, LabelPayload payload)
    {
        if (!raw.Contains('=') && !raw.Contains(':')) return false;

        var parts = raw.Split([';', '|', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return false;

        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var part in parts)
        {
            var idx = part.IndexOf('=');
            if (idx < 0) idx = part.IndexOf(':');
            if (idx <= 0) continue;
            var key = part[..idx].Trim();
            var value = part[(idx + 1)..].Trim();
            map[key] = value;
        }

        if (map.Count == 0) return false;
        FillFromMap(map.ToDictionary(x => x.Key.ToLowerInvariant(), x => x.Value), payload);
        return true;
    }

    private static void FillFromMap(IReadOnlyDictionary<string, string> map, LabelPayload payload)
    {
        payload.PartNum = FindValue(map, PartKeys);
        payload.LotNum = FindValue(map, LotKeys);
        payload.Uom = FindValue(map, UomKeys);

        var qtyRaw = FindValue(map, QtyKeys);
        if (decimal.TryParse(qtyRaw, NumberStyles.Any, CultureInfo.InvariantCulture, out var qty) ||
            decimal.TryParse(qtyRaw, NumberStyles.Any, CultureInfo.CurrentCulture, out qty))
        {
            payload.Qty = qty;
        }

        payload.ExpireDate = ParseDate(FindValue(map, ExpKeys));
        payload.ReceiveDate = ParseDate(FindValue(map, RecKeys));
    }

    private static string? FindValue(IReadOnlyDictionary<string, string> map, IEnumerable<string> keys)
        => keys.Select(k => map.TryGetValue(k, out var value) ? value : null).FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));

    private static DateTime? ParseDate(string? value)
        => DateTime.TryParse(value, out var date) ? date.Date : null;

    private static void ParseRegexFallback(string raw, LabelPayload payload)
    {
        payload.PartNum ??= PartRegex().Match(raw) is var p && p.Success ? p.Groups[1].Value : null;
        payload.LotNum ??= LotRegex().Match(raw) is var l && l.Success ? l.Groups[1].Value : null;
        if (payload.Qty == 0 && QtyRegex().Match(raw) is var q && q.Success && decimal.TryParse(q.Groups[1].Value, out var qty))
        {
            payload.Qty = qty;
        }
        payload.Uom ??= UomRegex().Match(raw) is var u && u.Success ? u.Groups[1].Value : null;

        payload.PartNum ??= GenericPartLikeRegex().Match(raw) is var gp && gp.Success ? gp.Groups[1].Value : null;
        payload.LotNum ??= GenericLotLikeRegex().Match(raw) is var gl && gl.Success ? gl.Groups[1].Value : null;
    }

    [GeneratedRegex(@"(?:part|partnum|pn)\s*[:=]\s*([A-Za-z0-9\-_/\.]+)", RegexOptions.IgnoreCase)]
    private static partial Regex PartRegex();

    [GeneratedRegex(@"(?:lot|lotnum|ln)\s*[:=]\s*([A-Za-z0-9\-_/\.]+)", RegexOptions.IgnoreCase)]
    private static partial Regex LotRegex();

    [GeneratedRegex(@"(?:qty|quantity)\s*[:=]\s*([0-9]+(?:\.[0-9]+)?)", RegexOptions.IgnoreCase)]
    private static partial Regex QtyRegex();

    [GeneratedRegex(@"(?:uom|unit)\s*[:=]\s*([A-Za-z]+)", RegexOptions.IgnoreCase)]
    private static partial Regex UomRegex();

    [GeneratedRegex(@"\b([A-Z]{2,}[A-Z0-9\-]{4,})\b")]
    private static partial Regex GenericPartLikeRegex();

    [GeneratedRegex(@"\b([A-Z]{2,}[0-9]{6,}|RMO[0-9]{8,})\b", RegexOptions.IgnoreCase)]
    private static partial Regex GenericLotLikeRegex();
}
