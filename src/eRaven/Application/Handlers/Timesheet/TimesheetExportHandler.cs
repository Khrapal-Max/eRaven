//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetExportHandler
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
    ITimesheetRepository repo)
    : IQueryHandler<ExportTimesheetMonthQuery, DownloadFileDto>
{
    public const string XlsxContentType =
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    private readonly ITimesheetRepository _repo = repo;

    public async Task<DownloadFileDto> HandleAsync(
        ExportTimesheetMonthQuery query,
        CancellationToken ct = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(query.Year, 2000, nameof(query.Year));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(query.Year, 2100, nameof(query.Year));

        ArgumentOutOfRangeException.ThrowIfLessThan(query.Month, 1, nameof(query.Month));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(query.Month, 12, nameof(query.Month));

        var daysInMonth = DateTime.DaysInMonth(query.Year, query.Month);

        var rows = await _repo.GetMonthlyTimesheetAsync(
            year: query.Year,
            month: query.Month,
            search: string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim(),
            ct: ct);

        var bytes = BuildMonthlyTimesheetXlsx(query.Year, query.Month, daysInMonth, rows);

        return new DownloadFileDto(
            FileName: BuildFileName(query.Year, query.Month),
            ContentType: XlsxContentType,
            Base64: Convert.ToBase64String(bytes));
    }

    private static byte[] BuildMonthlyTimesheetXlsx(
        int year,
        int month,
        int daysInMonth,
        IReadOnlyList<TimesheetMonthPerPersonDto> rows)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add($"Табель {MonthAbbrUa(month)} {year}");

        var monthAbbr = MonthAbbrUa(month);

        // Header: Тип Посада Звання ПІБ РНКОПП + дни
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

            // ВАЖНО: эти поля должны быть в TimesheetMonthPerPersonDto:
            // EnrollmentKind, Rnokpp. (у тебя они уже используются в UI)
            ws.Cell(rIdx, c++).Value = GetSign(r.EnrollmentKind);
            ws.Cell(rIdx, c++).Value = r.Position ?? "";
            ws.Cell(rIdx, c++).Value = r.Rank ?? "";
            ws.Cell(rIdx, c++).Value = r.FullName ?? "";
            ws.Cell(rIdx, c++).Value = r.RNOKPP ?? "";

            var map = BuildDayMap(r.Timesheet?.Days);

            for (var day = 1; day <= daysInMonth; day++)
            {
                var main = map.TryGetValue((day, TimesheetLane.Main), out var m) ? m : "";
                var task = map.TryGetValue((day, TimesheetLane.Task), out var t) ? t : "";

                var cellText = string.IsNullOrWhiteSpace(task)
                    ? main
                    : string.IsNullOrWhiteSpace(main) ? task : $"{main}\n{task}";

                ws.Cell(rIdx, c++).Value = cellText ?? "";
            }

            rIdx++;
        }

        // borders
        var used = ws.RangeUsed();
        used?.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    private static Dictionary<(int Day, TimesheetLane Lane), string> BuildDayMap(IReadOnlyList<MonthlyTimesheetDayDto>? days)
    {
        var dict = new Dictionary<(int, TimesheetLane), string>();
        if (days is null) return dict;

        foreach (var d in days)
            dict[(d.Day, d.Lane)] = d.Code ?? "";

        return dict;
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
