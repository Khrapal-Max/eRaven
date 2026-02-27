//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ExportTimesheetMonthQueryHandler
//-----------------------------------------------------------------------------

using ClosedXML.Excel;
using eRaven.Application.Abstractions.PersonRepository;
using eRaven.Application.Abstractions.TimesheetRepository;
using eRaven.Application.Abstractions.TimesheetRepository.ReadModels;
using eRaven.Application.DTOs.Enums;
using eRaven.Application.DTOs.Excel;
using eRaven.Application.Mapper;
using eRaven.Application.Queries;
using eRaven.Application.Queries.Timesheets;
using eRaven.Domain.Entities;
using eRaven.Domain.Enums;

namespace eRaven.Application.Handlers.Timesheets;

/// <summary>
/// Query handler: формує Excel (.xlsx) експорт місячного табеля.
/// </summary>
public sealed class ExportTimesheetMonthQueryHandler(
    ITimesheetViewRepository repo,
    IPersonRepository persons)
    : IQueryHandler<ExportTimesheetMonthQuery, DownloadFileDto>
{
    public const string XlsxContentType =
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    private readonly ITimesheetViewRepository _repo = repo;
    private readonly IPersonRepository _persons = persons;

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

        var periods = await _repo.GetTimesheetsMonthAsync(query.Year, query.Month, ct);
        if (periods.Count == 0)
        {
            return new DownloadFileDto(
                FileName: BuildFileName(query.Year, query.Month),
                ContentType: XlsxContentType,
                Base64: Convert.ToBase64String(BuildEmptyXlsx(query.Year, query.Month, daysInMonth)));
        }

        var personIds = periods.Select(x => x.PersonId).Distinct().ToArray();
        var cards = await _persons.GetByIdsAsync(personIds, ct);

        var byId = cards
            .Where(p => search is null || TimesheetDtoMapper.MatchesSearch(p, search))
            .ToDictionary(p => p.Id, p => p);

        var periodByPersonId = periods.ToDictionary(x => x.PersonId, x => x);

        var rows = byId.Values
            .OrderBy(x => x.PositionSort ?? int.MaxValue)
            .ThenBy(x => x.FullName)
            .Select(p => (Person: p, Period: periodByPersonId[p.Id]))
            .ToList();

        var bytes = BuildMonthlyTimesheetXlsx(query.Year, query.Month, daysInMonth, rows);

        return new DownloadFileDto(
            FileName: BuildFileName(query.Year, query.Month),
            ContentType: XlsxContentType,
            Base64: Convert.ToBase64String(bytes));
    }

    //======================================================================
    // Excel builder
    //======================================================================

    private static byte[] BuildMonthlyTimesheetXlsx(
        int year,
        int month,
        int daysInMonth,
        IReadOnlyList<(PersonReadModel Person, TimesheetPeriodRm Period)> rows)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add($"Табель {MonthAbbrUa(month)} {year}");

        var monthAbbr = MonthAbbrUa(month);

        // Header
        var col = 1;
        ws.Cell(1, col++).Value = "Тип";
        ws.Cell(1, col++).Value = "Посада";
        ws.Cell(1, col++).Value = "Звання";
        ws.Cell(1, col++).Value = "ПІБ";
        ws.Cell(1, col++).Value = "РНКОПП";

        for (var d = 1; d <= daysInMonth; d++)
            ws.Cell(1, col++).Value = $"{d} {monthAbbr}";

        var lastCol = col - 1;

        ws.Row(1).Style.Font.Bold = true;
        ws.Row(1).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        ws.Row(1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Row(1).Height = 20;

        ws.SheetView.FreezeRows(1);
        ws.Range(1, 1, 1, lastCol).SetAutoFilter();

        ws.Column(1).Width = 6;
        ws.Column(2).Width = 26;
        ws.Column(3).Width = 14;
        ws.Column(4).Width = 24;
        ws.Column(5).Width = 14;

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

        foreach (var (p, period) in rows)
        {
            var c = 1;

            var ek = TimesheetDtoMapper.MapEnrollmentKind(p.EnrollmentKind);

            ws.Cell(rIdx, c++).Value = GetSign(ek);
            ws.Cell(rIdx, c++).Value = p.Position ?? "";
            ws.Cell(rIdx, c++).Value = p.Rank ?? "";
            ws.Cell(rIdx, c++).Value = p.FullName ?? "";
            ws.Cell(rIdx, c++).Value = p.Rnokpp ?? "";

            for (var day = 1; day <= daysInMonth; day++)
            {
                var idx = day - 1;
                var snap = (idx >= 0 && idx < period.Days.Count) ? period.Days[idx] : null;
                var main = (snap?.Code ?? string.Empty).Trim();

                var cell = ws.Cell(rIdx, c++);
                cell.Value = main;

                ApplyDayCellStyle(cell, snap);

                if (snap is not null
                    && !string.IsNullOrWhiteSpace(snap.Reference)
                    && (snap.UiStyle == TimesheetUiStyle.Danger
                        || snap.UiStyle == TimesheetUiStyle.SystemFact))
                {
                    var ref100 = snap.Reference!.Trim();
                    if (!string.IsNullOrWhiteSpace(ref100))
                    {
                        var comment = cell.CreateComment();
                        comment.AddText(ref100);
                        comment.Visible = false;
                    }
                }
            }

            rIdx++;
        }

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

    private static byte[] BuildEmptyXlsx(int year, int month, int daysInMonth)
        => BuildMonthlyTimesheetXlsx(year, month, daysInMonth, []);

    //======================================================================
    // Helpers
    //======================================================================

    private static string BuildFileName(int year, int month)
        => $"timesheet_{year:D4}-{month:D2}.xlsx";

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
        _ => month.ToString()
    };

    private static string GetSign(EnrollmentKindDto kind)
        => kind switch
        {
            EnrollmentKindDto.Unit => "ШТ",
            EnrollmentKindDto.AttachedByList => "НК",
            EnrollmentKindDto.AttachedByOrder => "БР",
            _ => "ВКЛ"
        };

    // Simple styles
    private static readonly XLColor BgNb = XLColor.FromHtml("#d6f0ff");
    private static readonly XLColor FgNb = XLColor.FromHtml("#1864ab");

    private static readonly XLColor BgWarning = XLColor.FromHtml("#fff3cd");
    private static readonly XLColor FgWarning = XLColor.FromHtml("#5c4c00");

    private static readonly XLColor BgReady = XLColor.FromHtml("#e6fcf5");
    private static readonly XLColor FgReady = XLColor.FromHtml("#0b7285");

    private static readonly XLColor BgDanger = XLColor.FromHtml("#fff5f5");
    private static readonly XLColor FgDanger = XLColor.FromHtml("#c92a2a");

    private static readonly XLColor BgSystem = XLColor.FromHtml("#f3f0ff");
    private static readonly XLColor FgSystem = XLColor.FromHtml("#5f3dc4");

    private static void ApplyDayCellStyle(IXLCell cell, TimesheetDayRm? snap)
    {
        if (snap is null)
            return;


        if (snap.UiStyle == TimesheetUiStyle.NotInTimesheet)
        {
            cell.Style.Fill.BackgroundColor = BgNb;
            cell.Style.Font.FontColor = FgNb;
            return;
        }

        if (snap.UiStyle == TimesheetUiStyle.Warning)
        {
            cell.Style.Fill.BackgroundColor = BgWarning;
            cell.Style.Font.FontColor = FgWarning;
            cell.Style.Font.Bold = true;
            return;
        }

        if (snap.UiStyle == TimesheetUiStyle.Ready)
        {
            cell.Style.Fill.BackgroundColor = BgReady;
            cell.Style.Font.FontColor = FgReady;
            cell.Style.Font.Bold = true;
            return;
        }

        if (snap.UiStyle == TimesheetUiStyle.Danger)
        {
            cell.Style.Fill.BackgroundColor = BgDanger;
            cell.Style.Font.FontColor = FgDanger;
            cell.Style.Font.Bold = true;
            return;
        }

        if (snap.UiStyle == TimesheetUiStyle.SystemFact)
        {
            cell.Style.Fill.BackgroundColor = BgSystem;
            cell.Style.Font.FontColor = FgSystem;
            cell.Style.Font.Bold = true;
            return;
        }
    }
}
