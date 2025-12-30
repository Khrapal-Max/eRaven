//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ImportExportToolbar
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
    [Parameter] public EventCallback OnChanged { get; set; } // щоб сторінка могла Reload

    [Inject] public IPositionUnitExcelService Excel { get; set; } = default!;
    [Inject] public IPositionUnitRepository Repo { get; set; } = default!;
    [Inject] public IJSRuntime JS { get; set; } = default!;

    private bool _importOpen;
    private PositionUnitImportResult? _importResult;
    private IBrowserFile? _file;

    private Task OpenImport()
    {
        _importResult = null;
        _file = null;
        _importOpen = true;
        return Task.CompletedTask;
    }

    private async Task OnFileSelected(InputFileChangeEventArgs e)
    {
        _file = e.File;
        if (_file is null) return;

        // наприклад 10MB
        await using var stream = _file.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024);
        _importResult = await Excel.ParseAsync(stream, CancellationToken.None);
        await InvokeAsync(StateHasChanged);
    }

    private async Task<bool> CommitImportAsync()
    {
        if (_importResult is null) return false;

        // якщо є помилки — не даємо імпортувати
        if (_importResult.Errors.Count > 0) return false;

        // Додаткові перевірки на дублікати в БД (коди/активні номери)
        foreach (var x in _importResult.ValidItems)
        {
            if (await Repo.CodeExistsAsync(x.Code, CancellationToken.None))
            {
                _importResult.Errors.Add(new PositionUnitImportError(0, nameof(PositionUnit.Code),
                    $"Код '{x.Code}' вже існує в базі."));
            }

            if (await Repo.ActiveNumberExistsAsync(x.Number, CancellationToken.None))
            {
                _importResult.Errors.Add(new PositionUnitImportError(0, nameof(PositionUnit.Number),
                    $"Номер '{x.Number}' вже активний у базі."));
            }
        }

        if (_importResult.Errors.Count > 0)
        {
            await InvokeAsync(StateHasChanged);
            return false;
        }

        foreach (var x in _importResult.ValidItems)
            await Repo.AddPositionUnitAsync(x, CancellationToken.None);

        if (OnChanged.HasDelegate)
            await OnChanged.InvokeAsync();

        return true; // закрити модал
    }

    private async Task ExportAsync()
    {
        // тут можна попросити сторінку передати список — але простіше взяти з БД:
        var items = await Repo.GetAllPositionUnitsAsync(CancellationToken.None);

        var bytes = Excel.Export(items);
        var base64 = Convert.ToBase64String(bytes);

        var fileName = $"PositionUnits_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";
        await JS.InvokeVoidAsync("blazorDownloadFile", fileName,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            base64);
    }
}
