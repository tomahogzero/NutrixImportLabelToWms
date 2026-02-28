using System.Collections.ObjectModel;
using NutrixSyncLabelToWms.Core;

namespace NutrixSyncLabelToWms.App.ViewModels;

public sealed class HistoryViewModel : ObservableObject
{
    private readonly IScanRepository _repo;

    public ObservableCollection<ScanTransaction> Items { get; } = new ObservableCollection<ScanTransaction>();

    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public string? PartNum { get; set; }
    public string? LotNum { get; set; }
    public string? Status { get; set; }

    private string _message = "";
    public string Message { get => _message; set => Set(ref _message, value); }

    public AsyncRelayCommand SearchCommand { get; }

    public HistoryViewModel(IScanRepository repo)
    {
        _repo = repo;
        SearchCommand = new AsyncRelayCommand(SearchAsync);
    }

    private async Task SearchAsync()
    {
        try
        {
            var rows = await _repo.GetHistoryAsync(new HistoryFilter
            {
                FromDate = DateFrom,
                ToDate = DateTo,
                PartNum = string.IsNullOrWhiteSpace(PartNum) ? null : PartNum,
                LotNum = string.IsNullOrWhiteSpace(LotNum) ? null : LotNum,
                Status = string.IsNullOrWhiteSpace(Status) ? null : Status
            });

            Items.Clear();
            foreach (var row in rows) Items.Add(row);
            Message = $"พบข้อมูล {Items.Count} รายการ";
        }
        catch (Exception ex)
        {
            Message = ex.Message;
        }
    }
}
