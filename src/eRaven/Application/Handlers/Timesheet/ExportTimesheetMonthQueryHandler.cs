//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ExportTimesheetMonthQueryHandler
//-----------------------------------------------------------------------------

using ClosedXML.Excel;
using eRaven.Application.DTOs.Excel;
using eRaven.Application.DTOs.Timesheet;
using eRaven.Application.Queries;
using eRaven.Application.Queries.Timesheet;
using eRaven.Domain.Enums;
using eRaven.Infrastructure.Repositories.TimesheetRepository;

namespace eRaven.Application.Handlers.Timesheet;

public sealed class ExportTimesheetMonthQueryHandler(
    ITimesheetMonthRepository repo)
    : IQueryHandler<ExportTimesheetMonthQuery, DownloadFileDto>
{
    public const string XlsxContentType =
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    private readonly ITimesheetMonthRepository _repo = repo;

    public async Task<DownloadFileDto> HandleAsync(
        ExportTimesheetMonthQuery query,
        CancellationToken ct = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(query.Year, 2000, nameof(query.Year));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(query.Year, 2100, nameof(query.Year));

        ArgumentOutOfRangeException.ThrowIfLessThan(query.Month, 1, nameof(query.Month));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(query.Month, 12, nameof(query.Month));

        var daysInMonth = DateTime.DaysInMonth(query.Year, query.Month);
        var search = string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim();

        // NEW: repo повертає rows (без grid wrapper)
        var rows = await _repo.GetTimesheetMonthAsync(
            year: query.Year,
            month: query.Month,
            search: search,
            ct: ct);

        var bytes = BuildMonthlyTimesheetXlsx(
            year: query.Year,
            month: query.Month,
            daysInMonth: daysInMonth,
            rows: rows);

        return new DownloadFileDto(
            FileName: BuildFileName(query.Year, query.Month),
            ContentType: XlsxContentType,
            Base64: Convert.ToBase64String(bytes));
    }

    private static byte[] BuildMonthlyTimesheetXlsx(
        int year,
        int month,
        int daysInMonth,
        IReadOnlyList<TimesheetPersonMonthRowDto> rows)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add($"Табель {MonthAbbrUa(month)} {year}");

        var monthAbbr = MonthAbbrUa(month);

        // Header: Тип Посада Звання ПІБ РНКОПП + дні
        var col = 1;
        ws.Cell(1, col++).Value = "Тип";
        ws.Cell(1, col++).Value = "Посада";
        ws.Cell(1, col++).Value = "Звання";
        ws.Cell(1, col++).Value = "ПІБ";
        ws.Cell(1, col++).Value = "РНКОПП";

        for (var d = 1; d <= daysInMonth; d++)
            ws.Cell(1, col++).Value = $"{d} {monthAbbr}";

        var lastCol = col - 1;

        // Header style + freeze + filter
        ws.Row(1).Style.Font.Bold = true;
        ws.Row(1).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        ws.Row(1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Row(1).Height = 20;

        ws.SheetView.FreezeRows(1);
        ws.Range(1, 1, 1, lastCol).SetAutoFilter();

        // widths
        ws.Column(1).Width = 6;   // Тип
        ws.Column(2).Width = 26;  // Посада
        ws.Column(3).Width = 14;  // Звання
        ws.Column(4).Width = 24;  // ПІБ
        ws.Column(5).Width = 14;  // РНКОПП

        for (var c = 6; c <= lastCol; c++)
        {
            ws.Column(c).Width = 5;
            ws.Column(c).Style.Font.FontSize = 8;
            ws.Column(c).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Column(c).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            ws.Column(c).Style.Alignment.WrapText = true;
        }

        // Body
        var rIdx = 2;

        foreach (var r in rows)
        {
            var c = 1;

            ws.Cell(rIdx, c++).Value = GetSign(r.EnrollmentKind);
            ws.Cell(rIdx, c++).Value = r.Position ?? "";
            ws.Cell(rIdx, c++).Value = r.Rank ?? "";
            ws.Cell(rIdx, c++).Value = r.FullName ?? "";
            ws.Cell(rIdx, c++).Value = r.RNOKPP ?? "";

            for (var day = 1; day <= daysInMonth; day++)
            {
                var main = GetCode(r.MainCodes, day);

                var cell = ws.Cell(rIdx, c++);
                cell.Value = main ?? "";

                // style only by main (task ignored)
                ApplyDayCellStyle(cell, main, task: "");

                // comment for 100
                if (IsAlert(NormalizeCode(main)))
                {
                    var ref100 = GetText(r.MainRef, day); // потрібно поле в DTO
                    if (!string.IsNullOrWhiteSpace(ref100))
                    {
                        var comment = cell.CreateComment();   // replaces existing comment
                        comment.AddText(ref100);
                        comment.Visible = false;
                    }
                }
            }

            rIdx++;
        }

        // borders
        var used = ws.RangeUsed();
        if (used is not null)
        {
            used.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            used.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        }

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    // -------------------------
    // Helpers
    // -------------------------

    private static string? GetText(IReadOnlyList<string?>? items, int day)
    {
        if (items is null) return null;
        var idx = day - 1;
        if (idx < 0 || idx >= items.Count) return null;
        var s = items[idx];
        return string.IsNullOrWhiteSpace(s) ? null : s.Trim();
    }

    private static string GetCode(IReadOnlyList<string>? codes, int day)
    {
        if (codes is null) return "";
        var idx = day - 1;
        if (idx < 0 || idx >= codes.Count) return "";
        return (codes[idx] ?? "").Trim();
    }

    // -------------------------
    // Colors for codes
    // -------------------------

    private static readonly XLColor BgNb = XLColor.FromHtml("#e7f5ff");// light blue
    private static readonly XLColor FgNb = XLColor.FromHtml("#1864ab");

    private static readonly XLColor Bg30 = XLColor.White;
    private static readonly XLColor Fg30 = XLColor.FromHtml("#1f2937");// dark gray

    private static readonly XLColor BgVac = XLColor.FromHtml("#fff9db");// light yellow
    private static readonly XLColor FgVac = XLColor.FromHtml("#5f3dc4");

    private static readonly XLColor BgAlert = XLColor.FromHtml("#fff5f5"); // light red
    private static readonly XLColor FgAlert = XLColor.FromHtml("#c92a2a");

    private static readonly XLColor BgOther = XLColor.FromHtml("#e6fcf5");// light green
    private static readonly XLColor FgOther = XLColor.FromHtml("#0b7285");

    private static readonly XLColor BgTask = XLColor.FromHtml("#f8f0fc");// light purple
    private static readonly XLColor FgTask = XLColor.FromHtml("#862e9c");

    private static void ApplyDayCellStyle(IXLCell cell, string? main, string? task)
    {
        var m = NormalizeCode(main);
        var t = NormalizeCode(task);

        var hasMain = !string.IsNullOrWhiteSpace(m);
        var hasTask = !string.IsNullOrWhiteSpace(t);

        cell.Style.Font.Bold = true;
        cell.Style.Font.FontSize = 10;

        if (!hasMain && !hasTask)
        {
            SetCellColors(cell, BgNb, FgNb);
            return;
        }

        if (IsAlert(m) || IsAlert(t))
        {
            SetCellColors(cell, BgAlert, FgAlert);
            return;
        }

        if (string.Equals(m, "НБ", StringComparison.OrdinalIgnoreCase))
        {
            SetCellColors(cell, BgNb, FgNb);
            cell.Style.Font.Bold = false;
            cell.Style.Font.FontSize = 8;
            return;
        }

        if (string.Equals(m, "30", StringComparison.OrdinalIgnoreCase))
        {
            SetCellColors(cell, Bg30, Fg30);
            return;
        }

        if (m is "ВП" or "ВПХ" or "ВПП")
        {
            SetCellColors(cell, BgVac, FgVac);
            return;
        }

        if (!hasMain && hasTask)
        {
            SetCellColors(cell, BgTask, FgTask);
            return;
        }

        SetCellColors(cell, BgOther, FgOther);

        if (hasMain && hasTask)
        {
            cell.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            cell.Style.Border.BottomBorderColor = XLColor.FromHtml("#adb5bd");
        }
    }

    private static void SetCellColors(IXLCell cell, XLColor bg, XLColor fg)
    {
        cell.Style.Fill.BackgroundColor = bg;
        cell.Style.Font.FontColor = fg;
    }

    private static bool IsAlert(string? code)
        => string.Equals(code, "100", StringComparison.OrdinalIgnoreCase)
           || string.Equals(code, "ПБД", StringComparison.OrdinalIgnoreCase)
           || string.Equals(code, "Ф100", StringComparison.OrdinalIgnoreCase);

    private static string NormalizeCode(string? code)
    {
        var s = (code ?? "").Trim();
        if (s.Length == 0) return "";

        var sp = s.IndexOf(' ');
        if (sp > 0) s = s[..sp];

        var nl = s.IndexOfAny(['\n', '\r']);
        if (nl > 0) s = s[..nl];

        return s.Trim().ToUpperInvariant();
    }

    private static string BuildFileName(int year, int month)
        => $"Timesheet_{year}-{month:00}_{MonthAbbrUa(month)}.xlsx";

    private static string GetSign(EnrollmentKind? kind)
        => kind switch
        {
            EnrollmentKind.Unit => "ШТ",
            EnrollmentKind.AttachedByList => "НК",
            EnrollmentKind.AttachedByOrder => "БР",
            _ => "ВИКЛ"
        };

    private static string MonthAbbrUa(int month) => month switch
    {
        1 => "Січ",
        2 => "Лют",
        3 => "Бер",
        4 => "Кві",
        5 => "Тра",
        6 => "Чер",
        7 => "Лип",
        8 => "Сер",
        9 => "Вер",
        10 => "Жов",
        11 => "Лис",
        12 => "Гру",
        _ => "???"
    };
}