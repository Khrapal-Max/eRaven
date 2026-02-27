//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// RegistryImportExport
//-----------------------------------------------------------------------------

using ClosedXML.Excel;
using eRaven.Application.Commands;
using eRaven.Application.Commands.Excel;
using eRaven.Application.DTOs.Enums;
using eRaven.Application.DTOs.Excel;
using eRaven.Application.DTOs.Person;
using eRaven.Application.Queries;
using eRaven.Application.Queries.Personal;
using eRaven.Presentation.Toasts;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using System.Globalization;

namespace eRaven.Components.Pages.Persons.Registry.Drawers;

public partial class RegistryImportExport : ComponentBase
{
    private enum Tab { Import, Export }

    // =========================
    // DI
    // =========================
    [Inject] public IQueryHandler<GetPersonsPageQuery, PagedResult<PersonListItemDto>> GetPersonsPageQueryQueryHandler { get; set; } = default!;
    [Inject] public ICommandHandler<BootstrapPersonsCommand, BootstrapPersonsResult> BootstrapPersonsCommandHandler { get; set; } = default!;
    [Inject] public IJSRuntime JS { get; set; } = default!;
    [Inject] public ToastService ToastService { get; set; } = default!;

    // =========================
    // Parameters
    // =========================
    [Parameter] public bool IsOpen { get; set; }
    [Parameter] public EventCallback<bool> IsOpenChanged { get; set; }

    [Parameter] public PersonsRegistryFilters Filters { get; set; } = new();
    [Parameter] public EventCallback OnImported { get; set; }           // <- тільки “перезавантаж таблицю”
    [Parameter] public string Author { get; set; } = "system";          // <- TODO: auth user    

    // =========================
    // State
    // =========================
    private Tab _tab = Tab.Import;
    private bool _busy;
    private string? _fileName;

    private readonly List<PersonBootstrapRowDto> _rows = [];
    private readonly List<string> _globalErrors = [];

    private int _importTotalRows;
    private int _importValidRows;
    private int _importErrorRows;

    private BootstrapPersonsResult? _importResult;

    private const long MaxFileSize = 5 * 1024 * 1024; // 5MB

    // field error stats (counts + row samples)
    private int _errRnokpp, _errLastName, _errFirstName, _errKind, _errEnrollDate, _errReason, _errRank, _errPosition, _errPositionSort;

    private readonly List<int> _errRnokppRows = [];
    private readonly List<int> _errLastNameRows = [];
    private readonly List<int> _errFirstNameRows = [];
    private readonly List<int> _errKindRows = [];
    private readonly List<int> _errEnrollDateRows = [];
    private readonly List<int> _errReasonRows = [];
    private readonly List<int> _errRankRows = [];
    private readonly List<int> _errPositionRows = [];
    private readonly List<int> _errPositionSortRows = [];

    private sealed record ErrorStat(string Field, int Count, IReadOnlyList<int> Samples);
    private sealed record ImportErrorStat(string Message, int Count, IReadOnlyList<int> Rows);

    private bool IsImportDisabled => _busy || _importValidRows == 0 || _importErrorRows > 0;

    private IEnumerable<ErrorStat> ErrorStats()
    {
        if (_errRnokpp > 0) yield return new("РНОКПП", _errRnokpp, _errRnokppRows);
        if (_errLastName > 0) yield return new("Прізвище", _errLastName, _errLastNameRows);
        if (_errFirstName > 0) yield return new("Імʼя", _errFirstName, _errFirstNameRows);
        if (_errKind > 0) yield return new("Тип", _errKind, _errKindRows);
        if (_errEnrollDate > 0) yield return new("Дата зарахування", _errEnrollDate, _errEnrollDateRows);
        if (_errReason > 0) yield return new("Підстава", _errReason, _errReasonRows);
        if (_errRank > 0) yield return new("Звання", _errRank, _errRankRows);
        if (_errPositionSort > 0) yield return new("Порядок посади", _errPositionSort, _errPositionSortRows);
        if (_errPosition > 0) yield return new("Посада", _errPosition, _errPositionRows);
    }

    private IEnumerable<ImportErrorStat> ImportErrorStats()
    {
        if (_importResult is null || _importResult.Errors.Count == 0)
            yield break;

        foreach (var g in _importResult.Errors
            .GroupBy(x => x.Message)
            .OrderByDescending(g => g.Count())
            .Take(8))
        {
            var rows = g.Select(x => x.RowNumber).Distinct().OrderBy(x => x).Take(7).ToList();
            yield return new ImportErrorStat(g.Key, g.Count(), rows);
        }
    }

    // =========================
    // Import
    // =========================
    private async Task OnFileSelected(InputFileChangeEventArgs e)
    {
        if (_busy) return;

        ResetImportState();

        var file = e.File;
        _fileName = file.Name;

        if (!file.Name.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            _globalErrors.Add("Підтримується лише .xlsx");
            return;
        }

        _busy = true;
        try
        {
            await using var stream = file.OpenReadStream(MaxFileSize);
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            ms.Position = 0;

            using var wb = new XLWorkbook(ms);
            var ws = wb.Worksheets.FirstOrDefault();
            if (ws is null)
            {
                _globalErrors.Add("Не знайдено аркуш у файлі.");
                return;
            }

            ParseBootstrapWorksheet(ws);

            if (_importTotalRows == 0 && _globalErrors.Count == 0)
                _globalErrors.Add("Файл не містить даних для імпорту.");
        }
        catch (Exception ex)
        {
            _globalErrors.Add("Не вдалося прочитати Excel: " + ex.Message);
        }
        finally
        {
            _busy = false;
        }
    }

    private void ParseBootstrapWorksheet(IXLWorksheet ws)
    {
        var headerRow = ws.FirstRowUsed();
        if (headerRow is null)
        {
            _globalErrors.Add("Порожній файл.");
            return;
        }

        for (int i = 0; i < ImportHeadersUa.Length; i++)
        {
            var got = (headerRow.Cell(i + 1).GetString() ?? "").Trim();
            if (!HeaderEq(got, ImportHeadersUa[i]))
            {
                _globalErrors.Add("Невірний шаблон Excel. Завантаж шаблон і заповни його без зміни колонок/порядку.");
                _globalErrors.Add($"Очікується колонка #{i + 1}: '{ImportHeadersUa[i]}', а у файлі: '{got}'.");
                return;
            }
        }

        var firstDataRow = headerRow.RowNumber() + 1;
        var lastRow = ws.LastRowUsed()?.RowNumber() ?? firstDataRow - 1;

        for (int r = firstDataRow; r <= lastRow; r++)
        {
            var row = ws.Row(r);
            if (row.IsEmpty()) continue;
            if (LooksLikeDuplicateHeaderRow(row)) continue;

            _importTotalRows++;

            var hasErrors = false;

            string rnokpp = row.Cell(1).GetString().Trim();
            string lastName = row.Cell(2).GetString().Trim();
            string firstName = row.Cell(3).GetString().Trim();
            string? middleName = NormalizeOpt(row.Cell(4).GetString());

            var kindRaw = row.Cell(5).GetString().Trim();
            if (!TryParseKind(kindRaw, out var kind)) { MarkErr(ref _errKind, _errKindRows, r); hasErrors = true; }

            string? reference = NormalizeOpt(row.Cell(6).GetString());

            if (!TryParseDateOnly(row.Cell(7).Value, out var enrollDate)) { MarkErr(ref _errEnrollDate, _errEnrollDateRows, r); hasErrors = true; }

            string reason = row.Cell(8).GetString().Trim();
            string rank = row.Cell(9).GetString().Trim();

            var sortStr = row.Cell(10).GetString().Trim();
            if (!int.TryParse(sortStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out int positionSort) || positionSort <= 0)
            {
                MarkErr(ref _errPositionSort, _errPositionSortRows, r);
                hasErrors = true;
            }

            string position = row.Cell(11).GetString().Trim();

            string? bzvp = NormalizeOpt(row.Cell(12).GetString());
            string? weapon = NormalizeOpt(row.Cell(13).GetString());
            string? callsign = NormalizeOpt(row.Cell(14).GetString());

            if (rnokpp.Length != 10 || rnokpp.Any(ch => !char.IsDigit(ch))) { MarkErr(ref _errRnokpp, _errRnokppRows, r); hasErrors = true; }
            if (string.IsNullOrWhiteSpace(lastName)) { MarkErr(ref _errLastName, _errLastNameRows, r); hasErrors = true; }
            if (string.IsNullOrWhiteSpace(firstName)) { MarkErr(ref _errFirstName, _errFirstNameRows, r); hasErrors = true; }
            if (string.IsNullOrWhiteSpace(reason)) { MarkErr(ref _errReason, _errReasonRows, r); hasErrors = true; }
            if (string.IsNullOrWhiteSpace(rank)) { MarkErr(ref _errRank, _errRankRows, r); hasErrors = true; }
            if (string.IsNullOrWhiteSpace(position)) { MarkErr(ref _errPosition, _errPositionRows, r); hasErrors = true; }

            if (hasErrors)
            {
                _importErrorRows++;
                continue;
            }

            if (kind != EnrollmentKindDto.Unit)
                positionSort = 9999;

            _rows.Add(new PersonBootstrapRowDto(
                RowNumber: r,
                Rnokpp: rnokpp,
                LastName: lastName,
                FirstName: firstName,
                MiddleName: middleName,
                Kind: kind,
                Reference: reference,
                EnrollDate: enrollDate,
                Reason: reason,
                Rank: rank,
                PositionSort: positionSort,
                Position: position,
                Bzvp: bzvp,
                Weapon: weapon,
                Callsign: callsign
            ));

            _importValidRows++;
        }

        static bool HeaderEq(string a, string b) => Normalize(a) == Normalize(b);

        static string Normalize(string s)
        {
            s = (s ?? "").Trim().ToLowerInvariant();
            s = s.Replace("’", "'").Replace("ʼ", "'").Replace("`", "'");
            s = new string([.. s.Where(ch => !char.IsWhiteSpace(ch) && ch != '_' && ch != '.')]);
            return s;
        }

        static void MarkErr(ref int counter, List<int> sampleRows, int rowNumber)
        {
            counter++;
            if (sampleRows.Count < 5) sampleRows.Add(rowNumber);
        }
    }

    private static bool LooksLikeDuplicateHeaderRow(IXLRow row)
    {
        var c1 = (row.Cell(1).GetString() ?? "").Trim();
        var c2 = (row.Cell(2).GetString() ?? "").Trim();
        var c3 = (row.Cell(3).GetString() ?? "").Trim();

        return c1.Equals("РНОКПП", StringComparison.OrdinalIgnoreCase)
               || c2.Equals("Прізвище", StringComparison.OrdinalIgnoreCase)
               || c3.Equals("Імʼя", StringComparison.OrdinalIgnoreCase)
               || c3.Equals("Ім'я", StringComparison.OrdinalIgnoreCase);
    }

    private static string? NormalizeOpt(string? s)
        => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    private static bool TryParseDateOnly(object value, out DateOnly date)
    {
        if (value is DateTime dt)
        {
            date = DateOnly.FromDateTime(dt);
            return true;
        }

        if (value is double d)
        {
            try
            {
                var dtOa = DateTime.FromOADate(d);
                date = DateOnly.FromDateTime(dtOa);
                return true;
            }
            catch { /* ignore */ }
        }

        var s = value?.ToString()?.Trim();
        if (string.IsNullOrWhiteSpace(s))
        {
            date = default;
            return false;
        }

        if (DateOnly.TryParseExact(s, "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
            return true;

        if (DateOnly.TryParseExact(s, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
            return true;

        if (DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var dt2))
        {
            date = DateOnly.FromDateTime(dt2);
            return true;
        }

        date = default;
        return false;
    }

    private static bool TryParseKind(string raw, out EnrollmentKindDto kind)
    {
        raw = (raw ?? "").Trim();

        if (raw.Equals("Штат", StringComparison.OrdinalIgnoreCase)) { kind = EnrollmentKindDto.Unit; return true; }
        if (raw.Equals("БР", StringComparison.OrdinalIgnoreCase)) { kind = EnrollmentKindDto.AttachedByOrder; return true; }
        if (raw.Equals("Наказ", StringComparison.OrdinalIgnoreCase)) { kind = EnrollmentKindDto.AttachedByList; return true; }

        if (Enum.TryParse(raw, ignoreCase: true, out kind))
            return true;

        kind = default;
        return false;
    }

    private async Task ImportAsync()
    {
        if (IsImportDisabled) return;

        _busy = true;
        try
        {
            var author = string.IsNullOrWhiteSpace(Author) ? "system" : Author.Trim();
            var nowUtc = DateTime.UtcNow;

            _importResult = await BootstrapPersonsCommandHandler.HandleAsync(
                new BootstrapPersonsCommand(_rows, author, nowUtc));

            if (_importResult.Errors.Count == 0)
            {
                ToastService.Success($"Імпортовано: {_importResult.CreatedCount}");
            }
            else
            {
                ToastService.Warning(
               "Імпорт завершено з помилками",
               $"Створено: {_importResult.CreatedCount}, пропущено: {_importResult.SkippedCount}, помилок: {_importResult.Errors.Count}");
            }

            if (OnImported.HasDelegate)
                await OnImported.InvokeAsync();
        }
        finally
        {
            _busy = false;
        }
    }

    private Task Close() => IsOpenChanged.InvokeAsync(false);

    private Task OnDrawerClosed()
    {
        ResetState();
        return Task.CompletedTask;
    }

    private void ResetState()
    {
        _busy = false;
        _fileName = null;
        ResetImportState();
        _tab = Tab.Import;
    }

    private void ResetImportState()
    {
        _rows.Clear();
        _globalErrors.Clear();
        _importResult = null;

        _importTotalRows = 0;
        _importValidRows = 0;
        _importErrorRows = 0;

        _errRnokpp = _errLastName = _errFirstName = _errKind = _errEnrollDate = _errReason = _errRank = _errPosition = _errPositionSort = 0;

        _errRnokppRows.Clear();
        _errLastNameRows.Clear();
        _errFirstNameRows.Clear();
        _errKindRows.Clear();
        _errEnrollDateRows.Clear();
        _errReasonRows.Clear();
        _errRankRows.Clear();
        _errPositionRows.Clear();
        _errPositionSortRows.Clear();
    }

    // =========================
    // Export (xlsx only)
    // =========================
    private async Task ExportAllByFiltersXlsxAsync()
    {
        if (_busy) return;

        _busy = true;
        try
        {
            var items = await LoadAllByFiltersAsync();

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Особи");

            var headersUa = new[]
            {
                "ПІБ",
                "РНОКПП",
                "Статус",
                "Тип",
                "Звання",
                "Посада",
                "Зарах.",
                "Викл.",
                "Оновлено"
            };

            for (int c = 0; c < headersUa.Length; c++)
            {
                ws.Cell(1, c + 1).Value = headersUa[c];
                ws.Cell(1, c + 1).Style.Font.Bold = true;
            }

            var r = 2;
            foreach (var x in items)
            {
                ws.Cell(r, 1).Value = x.FullName;
                ws.Cell(r, 2).Value = x.Rnokpp;
                ws.Cell(r, 3).Value = LifecycleUa(x.Lifecycle);
                ws.Cell(r, 4).Value = KindUa(x.EnrollmentKind);
                ws.Cell(r, 5).Value = x.Rank ?? "";
                ws.Cell(r, 6).Value = x.Position ?? "";

                if (x.EnrolledAt is not null)
                {
                    ws.Cell(r, 7).Value = x.EnrolledAt.Value.ToDateTime(TimeOnly.MinValue);
                    ws.Cell(r, 7).Style.DateFormat.Format = "dd.MM.yyyy";
                }

                if (x.ExcludedAt is not null)
                {
                    ws.Cell(r, 8).Value = x.ExcludedAt.Value.ToDateTime(TimeOnly.MinValue);
                    ws.Cell(r, 8).Style.DateFormat.Format = "dd.MM.yyyy";
                }

                ws.Cell(r, 9).Value = x.UpdatedAtUtc.ToLocalTime();
                ws.Cell(r, 9).Style.DateFormat.Format = "dd.MM.yyyy HH:mm";

                r++;
            }

            var used = ws.RangeUsed();
            if (used is not null)
            {
                used.CreateTable();
                ws.SheetView.FreezeRows(1);
                ws.Columns().AdjustToContents();
            }

            await using var ms = new MemoryStream();
            wb.SaveAs(ms);

            await DownloadBytesAsync(
                $"persons_export_{DateTime.UtcNow:yyyyMMdd_HHmm}.xlsx",
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ms.ToArray());

            ToastService.Success($"Експортовано: {items.Count}");
        }
        finally
        {
            _busy = false;
        }

        static string LifecycleUa(PersonLifecycleDto lc) => lc switch
        {
            PersonLifecycleDto.Reserved => "Резерв",
            PersonLifecycleDto.Enrolled => "В табелі",
            _ => lc.ToString()
        };

        static string KindUa(EnrollmentKindDto? k) => k switch
        {
            EnrollmentKindDto.Unit => "Штат",
            EnrollmentKindDto.AttachedByOrder => "БР",
            EnrollmentKindDto.AttachedByList => "Наказ",
            null => "",
            _ => k.ToString()!
        };
    }

    private async Task<List<PersonListItemDto>> LoadAllByFiltersAsync()
    {
        const int size = 200;
        var page = 1;
        var result = new List<PersonListItemDto>();

        while (true)
        {
            var res = await GetPersonsPageQueryQueryHandler.HandleAsync(new GetPersonsPageQuery(
                Page: page,
                PageSize: size,
                Search: Filters.Search,
                Lifecycle: Filters.Lifecycle,
                EnrollmentKind: Filters.EnrollmentKind
            ));

            result.AddRange(res.Items);

            if (result.Count >= res.TotalCount || res.Items.Count == 0)
                break;

            page++;
        }

        return result;
    }

    // =========================
    // Template
    // =========================
    private static readonly string[] ImportHeadersUa =
    [
        "РНОКПП",
        "Прізвище",
        "Імʼя",
        "По батькові",
        "Тип",
        "Номер/посилання",
        "Дата зарахування",
        "Підстава",
        "Звання",
        "Порядок посади",
        "Посада",
        "БЗВП",
        "Зброя",
        "Позивний"
    ];

    private async Task DownloadImportTemplateXlsxAsync()
    {
        if (_busy) return;

        _busy = true;
        try
        {
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Імпорт");

            for (int c = 0; c < ImportHeadersUa.Length; c++)
            {
                ws.Cell(1, c + 1).Value = ImportHeadersUa[c];
                ws.Cell(1, c + 1).Style.Font.Bold = true;
            }

            ws.Cell(2, 1).Value = "1234567890";
            ws.Cell(2, 2).Value = "Іванов";
            ws.Cell(2, 3).Value = "Іван";
            ws.Cell(2, 4).Value = "Іванович";
            ws.Cell(2, 5).Value = "Штат";
            ws.Cell(2, 6).Value = "REF-001";
            ws.Cell(2, 7).Value = DateTime.Today;
            ws.Cell(2, 7).Style.DateFormat.Format = "dd.MM.yyyy";
            ws.Cell(2, 8).Value = "Підстава імпорту";
            ws.Cell(2, 9).Value = "Солдат";
            ws.Cell(2, 10).Value = 1;
            ws.Cell(2, 11).Value = "Стрілець";

            ws.Column(1).Style.NumberFormat.Format = "@";

            var dvKind = ws.Range(2, 5, 5000, 5).CreateDataValidation();
            dvKind.List("Штат,БР,Наказ", inCellDropdown: true);

            var dvDate = ws.Range(2, 7, 5000, 7).CreateDataValidation();
            dvDate.Date.Between(DateTime.Today.AddYears(-10), DateTime.Today.AddYears(10));

            ws.SheetView.FreezeRows(1);
            ws.Columns().AdjustToContents();

            await using var ms = new MemoryStream();
            wb.SaveAs(ms);

            await DownloadBytesAsync(
                $"persons_import_template_{DateTime.UtcNow:yyyyMMdd_HHmm}.xlsx",
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ms.ToArray());
        }
        finally
        {
            _busy = false;
        }
    }

    private async Task DownloadBytesAsync(string fileName, string contentType, byte[] bytes)
    {
        var base64 = Convert.ToBase64String(bytes);
        await JS.InvokeVoidAsync("blazorDownloadFile", fileName, contentType, base64);
    }
}
