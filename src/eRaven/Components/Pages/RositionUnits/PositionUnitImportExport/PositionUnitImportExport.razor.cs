//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PositionUnitImportExport
//-----------------------------------------------------------------------------

using eRaven.Domain.Entities;
using eRaven.Infrastructure.Excel;
using eRaven.Infrastructure.Repositories.PositionUnitRepository;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;

namespace eRaven.Components.Pages.RositionUnits.PositionUnitImportExport;

public partial class PositionUnitImportExport : ComponentBase
{
    [Parameter] public EventCallback OnChanged { get; set; }

    [Inject] public IPositionUnitExcelService Excel { get; set; } = default!;
    [Inject] public IPositionUnitRepository Repo { get; set; } = default!;
    [Inject] public IJSRuntime JS { get; set; } = default!;

    private bool _importOpen;
    private IBrowserFile? _file;
    private PositionUnitImportResult? _importResult;

    private bool _isParsing;
    private bool _isImporting;
    private bool _importDone;

    private string? _statusMessage;

    // ------------------------------
    // UI actions
    // ------------------------------

    private Task OpenImport()
    {
        _file = null;
        _importResult = null;

        _isParsing = false;
        _isImporting = false;
        _importDone = false;
        _statusMessage = null;

        _importOpen = true;
        return Task.CompletedTask;
    }

    private async Task OnFileSelected(InputFileChangeEventArgs e)
    {
        _file = e.File;
        if (_file is null) return;

        _importDone = false;
        _statusMessage = null;

        try
        {
            _isParsing = true;
            await InvokeAsync(StateHasChanged);

            await using var stream =
                _file.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024);

            _importResult = await Excel.ParseAsync(stream, CancellationToken.None);
        }
        finally
        {
            _isParsing = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task<bool> CommitImportAsync()
    {
        if (_importResult is null)
        {
            _statusMessage = "Файл не обрано.";
            return false;
        }

        if (_importResult.Errors.Count > 0)
        {
            _statusMessage = "Імпорт неможливий через помилки у файлі.";
            return false;
        }

        try
        {
            _isImporting = true;
            _importDone = false;
            _statusMessage = null;

            await InvokeAsync(StateHasChanged);

            // Перевірки на дублікати в БД
            foreach (var x in _importResult.ValidItems)
            {
                if (await Repo.CodeExistsAsync(x.Code, CancellationToken.None))
                {
                    _importResult.Errors.Add(
                        new PositionUnitImportError(
                            0,
                            nameof(PositionUnit.Code),
                            $"Код '{x.Code}' вже існує в базі."));
                }

                if (await Repo.ActiveNumberExistsAsync(x.Number, CancellationToken.None))
                {
                    _importResult.Errors.Add(
                        new PositionUnitImportError(
                            0,
                            nameof(PositionUnit.Number),
                            $"Номер '{x.Number}' вже активний у базі."));
                }
            }

            if (_importResult.Errors.Count > 0)
            {
                _statusMessage = "Імпорт зупинено через конфлікти з даними у базі.";
                return false;
            }

            foreach (var x in _importResult.ValidItems)
                await Repo.AddPositionUnitAsync(x, CancellationToken.None);

            if (OnChanged.HasDelegate)
                await OnChanged.InvokeAsync();

            _importDone = true;
            _statusMessage =
                $"Імпорт завершено. Додано записів: {_importResult.ValidItems.Count}.";

            return true; // Modal закриється
        }
        finally
        {
            _isImporting = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task ExportAsync()
    {
        var items = await Repo.GetAllPositionUnitsAsync(CancellationToken.None);

        var bytes = Excel.Export(items);
        var base64 = Convert.ToBase64String(bytes);

        var fileName = $"PositionUnits_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";

        await JS.InvokeVoidAsync(
            "blazorDownloadFile",
            fileName,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            base64);
    }
}
