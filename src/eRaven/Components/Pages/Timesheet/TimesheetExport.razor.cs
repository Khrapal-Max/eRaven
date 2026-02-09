//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetExport
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.Excel;
using eRaven.Application.Queries;
using eRaven.Application.Queries.Timesheet;
using eRaven.Presentation.Toasts;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace eRaven.Components.Pages.Timesheet;

/// <summary>
/// UI-компонент експорту місячного табеля в файл (XLSX).
/// Викликає read-query, а потім ініціює завантаження через JS interop.
/// </summary>
public partial class TimesheetExport : ComponentBase
{
    //======================================================================
    // DI
    //======================================================================

    /// <summary>
    /// Query-обробник експорту місячного табеля.
    /// Повертає DTO з base64-вмістом файлу, назвою та MIME-типом.
    /// </summary>
    [Inject]
    public IQueryHandler<ExportTimesheetMonthQuery, DownloadFileDto> ExportQuery { get; set; } = default!;

    /// <summary>
    /// JS interop для виклику функції завантаження файлу (наприклад: blazorDownloadFile).
    /// </summary>
    [Inject]
    public IJSRuntime JS { get; set; } = default!;

    /// <summary>
    /// Сервіс повідомлень/тостів для показу помилок користувачу.
    /// </summary>
    [Inject]
    public ToastService ToastService { get; set; } = default!;

    //======================================================================
    // Parameters
    //======================================================================

    /// <summary>
    /// Рік експорту.
    /// </summary>
    [Parameter] public int Year { get; set; }

    /// <summary>
    /// Місяць експорту (1..12).
    /// </summary>
    [Parameter] public int Month { get; set; }

    /// <summary>
    /// Опційний пошук (ПІБ/РНОКПП). Якщо null/порожній — експортуємо весь набір.
    /// </summary>
    [Parameter] public string? Search { get; set; }

    //======================================================================
    // State
    //======================================================================

    private bool _busy;

    //======================================================================
    // Actions
    //======================================================================

    /// <summary>
    /// Виконує експорт:
    /// 1) запитує файл через <see cref="ExportQuery"/>,
    /// 2) запускає завантаження у браузері через JS ("blazorDownloadFile").
    /// </summary>
    private async Task ExportAsync()
    {
        _busy = true;

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
            ToastService.Error(ex.Message);
        }
        finally
        {
            _busy = false;
        }
    }
}
