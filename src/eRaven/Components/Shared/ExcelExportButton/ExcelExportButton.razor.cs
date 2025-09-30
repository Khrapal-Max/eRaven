//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ExcelExportButton
//-----------------------------------------------------------------------------

using eRaven.Application.Services.ExcelService;
using eRaven.Application.Services.JsInterop;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;

namespace eRaven.Components.Shared.ExcelExportButton;

public partial class ExcelExportButton<TItem> : ComponentBase where TItem : class
{
    private const string ExcelContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    private static readonly TimeSpan DownloadTimeout = TimeSpan.FromSeconds(15);

    /// <summary>Дані для експорту.</summary>
    [Parameter, EditorRequired] public IEnumerable<TItem>? Items { get; set; }

    /// <summary>Назва файлу (без розширення). За замовчуванням — назва типу.</summary>
    [Parameter] public string? FileName { get; set; }

    /// <summary>Підпис на кнопці.</summary>
    [Parameter] public string Text { get; set; } = "Експорт";

    /// <summary>CSS клас кнопки.</summary>
    [Parameter] public string ButtonClass { get; set; } = "btn btn-sm btn-success";

    /// <summary>CSS клас іконки (Bootstrap Icons).</summary>
    [Parameter] public string IconClass { get; set; } = "bi bi-file-earmark-excel";

    /// <summary>Title атрибут.</summary>
    [Parameter] public string Title { get; set; } = "Експорт в Excel";

    /// <summary>Зовнішній флаг вимкнення.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Подія на початок/кінець роботи (для зовнішнього Busy-спінера).</summary>
    [Parameter] public EventCallback<bool> OnBusyChanged { get; set; }

    [Inject] public IExcelService ExcelService { get; set; } = default!;
    [Inject] public IJSRuntime JS { get; set; } = default!;

    private async Task ExportAsync()
    {
        // повна відсічка входу: або зовнішній Disabled, або вже виконується
        if (Disabled)
            return;

        await OnBusyChanged.InvokeAsync(true);

        try
        {
            var data = Items ?? [];

            await using var stream = await ExcelService.ExportAsync(data);

            using var ms = new MemoryStream(
                stream.CanSeek && stream.Length <= int.MaxValue ? (int)stream.Length : 0);
            await stream.CopyToAsync(ms);

            // простіше і без TryGetBuffer: менше гілок — стабільніший код і тести
            var base64 = Convert.ToBase64String(ms.ToArray());

            var name = string.IsNullOrWhiteSpace(FileName) ? typeof(TItem).Name : FileName!;
            await JS.InvokeVoidWithCancellationAsync(
                "downloadFile",
                timeout: DownloadTimeout,
                $"{name}.xlsx",
                ExcelContentType,
                base64);
        }
        finally
        {
            await OnBusyChanged.InvokeAsync(false);
            await InvokeAsync(StateHasChanged);
        }
    }
}
