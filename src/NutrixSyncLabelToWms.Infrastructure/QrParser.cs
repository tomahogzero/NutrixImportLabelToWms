using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using NutrixSyncLabelToWms.Core;

namespace NutrixSyncLabelToWms.Infrastructure;

public sealed class FlexibleQrParser : IQrParser
{
    private static readonly string[] PartKeys = new[] { "part", "partnum", "part_no", "p" };
    private static readonly string[] LotKeys = new[] { "lot", "lotnum", "lot_no", "l" };
    private static readonly string[] QtyKeys = new[] { "qty", "quantity", "q" };
    private static readonly string[] UomKeys = new[] { "uom", "unit" };
    private static readonly string[] ExpKeys = new[] { "expiredate", "exp", "expiry" };
    private static readonly string[] RecKeys = new[] { "receivedate", "receive", "rcvdate", "mfg" };

    private static readonly Regex PartRegex = new Regex(@"(?:part|partnum|pn)\s*[:=]\s*([A-Za-z0-9\-_/\.]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex LotRegex = new Regex(@"(?:lot|lotnum|ln)\s*[:=]\s*([A-Za-z0-9\-_/\.]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex QtyRegex = new Regex(@"(?:qty|quantity)\s*[:=]\s*([0-9]+(?:\.[0-9]+)?)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex UomRegex = new Regex(@"(?:uom|unit)\s*[:=]\s*([A-Za-z]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex GenericPartLikeRegex = new Regex(@"\b([A-Z]{2,}[A-Z0-9\-]{4,})\b", RegexOptions.Compiled);
    private static readonly Regex GenericLotLikeRegex = new Regex(@"\b([A-Z]{2,}[0-9]{6,}|RMO[0-9]{8,})\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public LabelPayload Parse(string rawText)
    {
        rawText = rawText ?? string.Empty;
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
            if (!raw.TrimStart().StartsWith("{", StringComparison.Ordinal)) return false;
            using (var doc = JsonDocument.Parse(raw))
            {
                var map = doc.RootElement.EnumerateObject().ToDictionary(x => x.Name.ToLowerInvariant(), x => x.Value.ToString());
                FillFromMap(map, payload);
                return true;
            }
        }
        catch
        {
            return false;
        }
    }

    private static bool TryParseKeyValue(string raw, LabelPayload payload)
    {
        if (!raw.Contains("=") && !raw.Contains(":")) return false;

        var parts = raw.Split(new[] { ';', '|', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return false;

        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var part in parts)
        {
            var idx = part.IndexOf('=');
            if (idx < 0) idx = part.IndexOf(':');
            if (idx <= 0) continue;
            var key = part.Substring(0, idx).Trim();
            var value = part.Substring(idx + 1).Trim();
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
        decimal qty;
        if (decimal.TryParse(qtyRaw, NumberStyles.Any, CultureInfo.InvariantCulture, out qty) ||
            decimal.TryParse(qtyRaw, NumberStyles.Any, CultureInfo.CurrentCulture, out qty))
        {
            payload.Qty = qty;
        }

        payload.ExpireDate = ParseDate(FindValue(map, ExpKeys));
        payload.ReceiveDate = ParseDate(FindValue(map, RecKeys));
    }

    private static string FindValue(IReadOnlyDictionary<string, string> map, IEnumerable<string> keys)
    {
        foreach (var key in keys)
        {
            string value;
            if (map.TryGetValue(key, out value) && !string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }
        return null;
    }

    private static DateTime? ParseDate(string value)
    {
        DateTime date;
        return DateTime.TryParse(value, out date) ? date.Date : (DateTime?)null;
    }

    private static void ParseRegexFallback(string raw, LabelPayload payload)
    {
        if (string.IsNullOrWhiteSpace(payload.PartNum))
        {
            var partMatch = PartRegex.Match(raw);
            if (partMatch.Success) payload.PartNum = partMatch.Groups[1].Value;
        }

        if (string.IsNullOrWhiteSpace(payload.LotNum))
        {
            var lotMatch = LotRegex.Match(raw);
            if (lotMatch.Success) payload.LotNum = lotMatch.Groups[1].Value;
        }

        if (payload.Qty == 0)
        {
            var qtyMatch = QtyRegex.Match(raw);
            decimal qty;
            if (qtyMatch.Success && decimal.TryParse(qtyMatch.Groups[1].Value, out qty))
            {
                payload.Qty = qty;
            }
        }

        if (string.IsNullOrWhiteSpace(payload.Uom))
        {
            var uomMatch = UomRegex.Match(raw);
            if (uomMatch.Success) payload.Uom = uomMatch.Groups[1].Value;
        }

        if (string.IsNullOrWhiteSpace(payload.PartNum))
        {
            var gp = GenericPartLikeRegex.Match(raw);
            if (gp.Success) payload.PartNum = gp.Groups[1].Value;
        }

        if (string.IsNullOrWhiteSpace(payload.LotNum))
        {
            var gl = GenericLotLikeRegex.Match(raw);
            if (gl.Success) payload.LotNum = gl.Groups[1].Value;
        }
    }
}
