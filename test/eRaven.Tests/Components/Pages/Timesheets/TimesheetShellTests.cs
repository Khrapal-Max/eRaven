//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetShellTests
//-----------------------------------------------------------------------------

using Bunit;
using eRaven.Application.DTOs.Enums;
using eRaven.Application.DTOs.Excel;
using eRaven.Application.DTOs.Timesheets;
using eRaven.Application.Queries;
using eRaven.Application.Queries.Timesheets;
using eRaven.Components.Pages.Timesheets;
using eRaven.Presentation.Toasts;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Globalization;

namespace eRaven.Tests.Components.Pages.Timesheets;

public sealed class TimesheetShellTests : BunitContext
{
    private readonly ToastService _toastService;
    private readonly Mock<IQueryHandler<GetTimesheetsMonthQuery, IReadOnlyList<TimesheetPersonRangeRowDto>>> _query;
    private readonly Mock<IQueryHandler<ExportTimesheetMonthQuery, DownloadFileDto>> _export;

    public TimesheetShellTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        _toastService = new();
        _query = new(MockBehavior.Strict);
        _export = new(MockBehavior.Strict);
        Services.AddSingleton(_query.Object);
        Services.AddSingleton(_export.Object);
        Services.AddSingleton(_toastService);
    }

    [Fact]
    public void Render_ShowsCoreControls_AndMonthGridHeader()
    {
        // arrange
        var today = DateTime.Today;
        var daysInMonth = DateTime.DaysInMonth(today.Year, today.Month);

        var rows = new List<TimesheetPersonRangeRowDto>
        {
            CreateRow(
                personId: Guid.NewGuid(),
                fullName: "Іванов Іван Іванович",
                rnokpp: "1234567890",
                kind: EnrollmentKindDto.Unit,
                year: today.Year,
                month: today.Month,
                daysInMonth: daysInMonth,
                mainCode: "Т")
        };

        _query
            .Setup(q => q.HandleAsync(It.IsAny<GetTimesheetsMonthQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(rows);

        // act
        var cut = Render<TimesheetShell>();

        // assert (basic controls)
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Табель (місяць)", cut.Markup, StringComparison.Ordinal);

            // year input
            Assert.NotNull(cut.Find("input[type=number]"));
            // month select
            Assert.NotNull(cut.Find("select"));
            // search input
            Assert.NotNull(cut.Find("input[placeholder^='Напр']"));

            // filter buttons
            var filterGroup = cut.Find("div.btn-group[aria-label='Фільтр типу обліку']");
            var buttons = filterGroup.QuerySelectorAll("button");
            Assert.Equal(5, buttons.Length);
            Assert.Contains(buttons, b => b.TextContent.Contains("ВСІ", StringComparison.Ordinal));
            Assert.Contains(buttons, b => b.TextContent.Contains("ШТАТ", StringComparison.Ordinal));
            Assert.Contains(buttons, b => b.TextContent.Contains("БР", StringComparison.Ordinal));
            Assert.Contains(buttons, b => b.TextContent.Contains("НАКАЗ", StringComparison.Ordinal));
            Assert.Contains(buttons, b => b.TextContent.Contains("ВИКЛ", StringComparison.Ordinal));

            // refresh
            Assert.Contains("Оновити", cut.Markup, StringComparison.Ordinal);

            // grid header: all day columns exist
            var dayHeaders = cut.FindAll("th.ts-day");
            Assert.Equal(daysInMonth, dayHeaders.Count);
            Assert.Equal("1", dayHeaders[0].TextContent.Trim());
            Assert.Equal(daysInMonth.ToString(CultureInfo.InvariantCulture), dayHeaders[^1].TextContent.Trim());
        });

        _query.VerifyAll();
    }

    [Fact]
    public void InjectedQuery_IsCalledOnInit_AndSearchReloadRespectsThreshold()
    {
        // arrange
        var calls = new List<GetTimesheetsMonthQuery>();

        _query
            .Setup(q => q.HandleAsync(It.IsAny<GetTimesheetsMonthQuery>(), It.IsAny<CancellationToken>()))
            .Callback<GetTimesheetsMonthQuery, CancellationToken>((q, _) => calls.Add(q))
            .ReturnsAsync([]);

        // act
        var cut = Render<TimesheetShell>();

        // init call
        cut.WaitForAssertion(() => Assert.Single(calls));

        // search: 1 char => should NOT reload
        var search = cut.Find("input[placeholder^='Напр']");
        search.Input("a");
        cut.WaitForAssertion(() => Assert.Single(calls));

        // search: 2 chars => reload
        search.Input("ab");
        cut.WaitForAssertion(() => Assert.Equal(2, calls.Count));

        // search: clear => reload
        search.Input("");
        cut.WaitForAssertion(() => Assert.Equal(3, calls.Count));

        // verify search normalization in query
        Assert.Null(calls[0].Search);
        Assert.Equal("ab", calls[1].Search);
        Assert.Null(calls[2].Search);

        _query.VerifyAll();
    }

    private static TimesheetPersonRangeRowDto CreateRow(
        Guid personId,
        string fullName,
        string rnokpp,
        EnrollmentKindDto kind,
        int year,
        int month,
        int daysInMonth,
        string mainCode)
    {
        var person = new TimesheetPersonInfoDto(
            PersonId: personId,
            FullName: fullName,
            Rnokpp: rnokpp,
            Rank: null,
            PositionSort: 1,
            Position: null,
            EnrollmentKindDto: kind,
            EnrolledAt: new DateOnly(year, month, Math.Min(10, daysInMonth)),
            ExcludedAt: null);

        var days = new List<TimesheetDaySnapshotDto>(daysInMonth);
        for (var d = 1; d <= daysInMonth; d++)
        {
            days.Add(new TimesheetDaySnapshotDto(
                Date: new DateOnly(year, month, d),
                CodeId: Guid.NewGuid(),
                Code: mainCode,
                Reference: null,
                Note: null,
                IsDerived: false,
                IsChangePoint: d == 1,
                UiStyle: TimesheetUiStyleDto.Ready));
        }

        return new TimesheetPersonRangeRowDto(person, days);
    }
}
