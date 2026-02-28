using NutrixSyncLabelToWms.App.Services;
using NutrixSyncLabelToWms.Core;

namespace NutrixSyncLabelToWms.App.ViewModels;

public sealed class ScanViewModel : ObservableObject
{
    private readonly IQrParser _parser;
    private readonly IValidationService _validation;
    private readonly IScanRepository _repo;
    private readonly AppState _appState;

    private string _qrInput = string.Empty;
    private LabelPayload _payload = new();
    private ValidationResult? _validationResult;
    private string _statusMessage = "พร้อมสแกน";

    public string QrInput { get => _qrInput; set => Set(ref _qrInput, value); }
    public LabelPayload Payload { get => _payload; set => Set(ref _payload, value); }
    public ValidationResult? ValidationResult { get => _validationResult; set => Set(ref _validationResult, value); }
    public string StatusMessage { get => _statusMessage; set => Set(ref _statusMessage, value); }

    public AsyncRelayCommand ParseCommand { get; }
    public AsyncRelayCommand ValidateCommand { get; }
    public AsyncRelayCommand SaveCommand { get; }

    public ScanViewModel(IQrParser parser, IValidationService validation, IScanRepository repo, AppState appState)
    {
        _parser = parser;
        _validation = validation;
        _repo = repo;
        _appState = appState;

        ParseCommand = new AsyncRelayCommand(ParseAsync);
        ValidateCommand = new AsyncRelayCommand(ValidateAsync);
        SaveCommand = new AsyncRelayCommand(SaveAsync);
    }

    private Task ParseAsync()
    {
        Payload = _parser.Parse(QrInput);
        ValidationResult = null;
        StatusMessage = "Parse สำเร็จ";
        return Task.CompletedTask;
    }

    private async Task ValidateAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(Payload.RawText)) Payload = _parser.Parse(QrInput);
            ValidationResult = await _validation.ValidateAsync(Payload, _appState.Settings);
            StatusMessage = $"Validate: {ValidationResult.Status} ({ValidationResult.Message})";
        }
        catch (Exception ex)
        {
            ValidationResult = new ValidationResult { IsPass = false, Message = ex.Message };
            StatusMessage = $"Validate error: {ex.Message}";
        }
    }

    private async Task SaveAsync()
    {
        try
        {
            await _repo.EnsureTablesAsync();
            var result = ValidationResult ?? new ValidationResult { IsPass = false, Message = "NOT_VALIDATED", LabelQty = Payload.Qty };
            var tx = new ScanTransaction
            {
                Id = Guid.NewGuid(),
                ScanAt = DateTime.Now,
                Plant = _appState.Settings.Epicor.Plant,
                QrRawText = Payload.RawText,
                PartNum = Payload.PartNum,
                LotNum = Payload.LotNum,
                ExpireDate = Payload.ExpireDate,
                ReceiveDate = Payload.ReceiveDate,
                QtyLabel = Payload.Qty,
                UomLabel = Payload.Uom,
                QtyOnHand = result.QtyOnHand,
                ValidationStatus = result.Status,
                ValidationMessage = result.Message,
                EpicorResponseJson = result.EpicorResponseJson
            };

            await _repo.InsertScanAsync(tx);
            StatusMessage = "บันทึกลง SQL สำเร็จ";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Save error: {ex.Message}";
        }
    }
}
