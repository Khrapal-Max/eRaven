//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetExport
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.Excel;
using eRaven.Application.Queries;
using eRaven.Application.Queries.Timesheet;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace eRaven.Components.Pages.Timesheet;

public partial class TimesheetExport
{
    [Inject] public IQueryHandler<ExportTimesheetMonthQuery, DownloadFileDto> ExportQuery { get; set; } = default!;
    [Inject] public IJSRuntime JS { get; set; } = default!;

    [Parameter] public int Year { get; set; }
    [Parameter] public int Month { get; set; }
    [Parameter] public string? Search { get; set; }

    private bool _busy;
    private string? _error;

    private async Task ExportAsync()
    {
        _busy = true;
        _error = null;

        try
        {
            var file = await ExportQuery.HandleAsync(new ExportTimesheetMonthQuery(
                Year: Year,
                Month: Month,
                Search: string.IsNullOrWhiteSpace(Search) ? null : Search.Trim()));

            await JS.InvokeVoidAsync("blazorDownloadFile", file.FileName, file.ContentType, file.Base64);
        }
        catch (Exception ex)
        {
            _error = ex.Message;
        }
        finally
        {
            _busy = false;
        }
    }
}
