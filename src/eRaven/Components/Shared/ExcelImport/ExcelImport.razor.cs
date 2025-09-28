//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ExcelImport
//-----------------------------------------------------------------------------

using eRaven.Application.Services.ExcelService;
using eRaven.Application.ViewModels;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace eRaven.Components.Shared.ExcelImport;

public partial class ExcelImport<TItem> : ComponentBase where TItem : class, new()
{
    private IBrowserFile? _file;
    private bool _busy;

    /// <summary>Лінк на шаблон (xlsx), опційно.</summary>
    [Parameter] public string? TemplateUrl { get; set; }

    /// <summary>Максимальний розмір файлу (МБ). За замовчуванням 10.</summary>
    [Parameter] public int MaxSizeMb { get; set; } = 10;

    /// <summary>
    /// Якщо true — при наявності помилок парсингу <see cref="ExcelService"/> 
    /// обробка (ProcessAsync) не викликається. За замовчуванням true.
    /// </summary>
    [Parameter] public bool StopOnParseErrors { get; set; } = true;

    /// <summary>
    /// Делегат, який виконує бізнес-обробку розпарсених рядків (upsert у БД)
    /// та повертає підсумковий звіт.
    /// Обов’язковий параметр.
    /// </summary>
    [Parameter, EditorRequired]
    public Func<IReadOnlyList<TItem>, Task<ImportReportViewModel>> ProcessAsync { get; set; } = default!;

    /// <summary>Подія на початок/кінець роботи (для зовнішнього Busy-спінера).</summary>
    [Parameter] public EventCallback<bool> OnBusyChanged { get; set; }

    /// <summary>
    /// Опційна подія з необробленими розпарсеними рядками (для попереднього перегляду/валідації).
    /// </summary>
    [Parameter] public EventCallback<IReadOnlyList<TItem>> OnParsed { get; set; }

    /// <summary>
    /// Фінальна подія зі звітом (Added/Updated/Errors), який повернув ProcessAsync.
    /// Тут ти вже малюєш помилки на сторінці.
    /// </summary>
    [Parameter] public EventCallback<ImportReportViewModel> OnCompleted { get; set; }

    [Inject] private IExcelService ExcelService { get; set; } = default!;

    private void HandleFileSelected(InputFileChangeEventArgs e)
    {
        _file = e.File;
    }

    private async Task ImportAsync()
    {
        if (_file is null) return;

        var maxBytes = MaxSizeMb * 1024L * 1024L;
        if (_file.Size > maxBytes)
        {
            // Репортуємо через OnCompleted як помилки верхнього рівня (без спроби парсингу)
            var tooBig = new ImportReportViewModel(0, 0, new List<string> { $"Файл перевищує {MaxSizeMb} МБ." });
            if (OnCompleted.HasDelegate) await OnCompleted.InvokeAsync(tooBig);
            return;
        }

        _busy = true;
        await OnBusyChanged.InvokeAsync(true);

        try
        {
            await using var stream = _file.OpenReadStream(maxBytes);

            // 1) Парсимо excel -> (rows, parseErrors)
            var (rows, parseErrors) = await ExcelService.ImportAsync<TItem>(stream);

            if (OnParsed.HasDelegate)
                await OnParsed.InvokeAsync(rows);

            // 2) Якщо треба зупинятись на помилках парсингу — репортуємо і завершуємо.
            if (StopOnParseErrors && parseErrors.Count > 0)
            {
                var report = new ImportReportViewModel(0, 0, parseErrors);
                if (OnCompleted.HasDelegate) await OnCompleted.InvokeAsync(report);
                return;
            }

            // 3) Викликаємо бізнес-обробку (upsert у БД) на стороні сторінки.
            var processReport = await ProcessAsync(rows);

            // 4) Зливаємо помилки парсингу з помилками процесингу (якщо є)
            List<string> mergedErrors;

            if (parseErrors.Count == 0)
                mergedErrors = processReport.Errors ?? [];
            else
                mergedErrors = [.. processReport.Errors ?? [], .. parseErrors];

            var finalReport = new ImportReportViewModel(
                Added: processReport.Added,
                Updated: processReport.Updated,
                Errors: mergedErrors
            );

            if (OnCompleted.HasDelegate)
                await OnCompleted.InvokeAsync(finalReport);
        }
        finally
        {
            _busy = false;
            await OnBusyChanged.InvokeAsync(false);
        }
    }
}
