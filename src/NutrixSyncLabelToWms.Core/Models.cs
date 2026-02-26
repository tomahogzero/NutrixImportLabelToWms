namespace NutrixSyncLabelToWms.Core;

public sealed class LabelPayload
{
    public string RawText { get; set; } = string.Empty;
    public string? PartNum { get; set; }
    public string? LotNum { get; set; }
    public decimal Qty { get; set; }
    public string? Uom { get; set; }
    public DateTime? ExpireDate { get; set; }
    public DateTime? ReceiveDate { get; set; }
}

public sealed class ValidationResult
{
    public bool IsPass { get; set; }
    public string Status => IsPass ? "PASS" : "FAIL";
    public string Message { get; set; } = string.Empty;
    public decimal LabelQty { get; set; }
    public decimal QtyOnHand { get; set; }
    public int MatchedRowsCount { get; set; }
    public string EpicorResponseJson { get; set; } = string.Empty;
}

public sealed class EpicorBaqRow
{
    public string? PartNum { get; set; }
    public string? LotNum { get; set; }
    public string? WarehouseCode { get; set; }
    public string? DimCode { get; set; }
    public decimal OnHandQty { get; set; }
    public DateTime? ExpirationDate { get; set; }
}

public sealed class EpicorQueryResult
{
    public int Count { get; set; }
    public IReadOnlyList<EpicorBaqRow> Rows { get; set; } = Array.Empty<EpicorBaqRow>();
    public string RawJson { get; set; } = string.Empty;
}

public sealed class ScanTransaction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime ScanAt { get; set; } = DateTime.Now;
    public string Plant { get; set; } = string.Empty;
    public string QrRawText { get; set; } = string.Empty;
    public string? PartNum { get; set; }
    public string? LotNum { get; set; }
    public DateTime? ExpireDate { get; set; }
    public DateTime? ReceiveDate { get; set; }
    public decimal QtyLabel { get; set; }
    public string? UomLabel { get; set; }
    public decimal? QtyOnHand { get; set; }
    public string ValidationStatus { get; set; } = "FAIL";
    public string ValidationMessage { get; set; } = string.Empty;
    public string EpicorResponseJson { get; set; } = string.Empty;
}

public sealed class HistoryFilter
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? PartNum { get; set; }
    public string? LotNum { get; set; }
    public string? Status { get; set; }
}
