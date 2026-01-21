//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetShellTests
//-----------------------------------------------------------------------------

using Bunit;
using eRaven.Application.DTOs.Timesheet;
using eRaven.Application.Queries;
using eRaven.Application.Queries.Timesheet;
using eRaven.Components.Pages.Timesheet;
using eRaven.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace eRaven.Tests.Components.Pages.Timesheet;

public sealed class TimesheetShellTests : BunitContext
{
    public TimesheetShellTests()
    {
        // Щоб тестувати саме Shell, а не експорт/модалку (JS/DI).
        ComponentFactories.AddStub<TimesheetExport>();
        ComponentFactories.AddStub<TimesheetPersonModal>();
    }

    [Fact]
    public void OnInitialized_calls_query_with_today_year_month_and_renders_table_header_days()
    {
        // arrange
        var today = DateTime.Today;
        var daysInMonth = DateTime.DaysInMonth(today.Year, today.Month);

        var calls = new List<GetTimesheetMonthQuery>();

        var mock = new Mock<IQueryHandler<GetTimesheetMonthQuery, IReadOnlyList<TimesheetMonthPerPersonDto>>>(MockBehavior.Strict);
        mock.Setup(x => x.HandleAsync(It.IsAny<GetTimesheetMonthQuery>(), It.IsAny<CancellationToken>()))
            .Callback<GetTimesheetMonthQuery, CancellationToken>((q, _) => calls.Add(q))
            .ReturnsAsync([Row(today.Year, today.Month, daysInMonth)]);

        Services.AddSingleton(mock.Object);

        // act
        var cut = Render<TimesheetShell>();

        // assert
        cut.WaitForAssertion(() =>
        {
            Assert.Single(calls);
            Assert.Equal(today.Year, calls[0].Year);
            Assert.Equal(today.Month, calls[0].Month);
            Assert.Null(calls[0].Search);

            // є таблиця і заголовок на дні
            var ths = cut.FindAll("thead th");
            // 3 базові колонки: Тип, ПІБ, 🔍 + days
            Assert.Equal(3 + daysInMonth, ths.Count);
        });
    }

    [Fact]
    public void SearchInput_reloads_only_when_len_ge_2_or_cleared()
    {
        // arrange
        var today = DateTime.Today;
        var daysInMonth = DateTime.DaysInMonth(today.Year, today.Month);

        var calls = new List<GetTimesheetMonthQuery>();

        var mock = new Mock<IQueryHandler<GetTimesheetMonthQuery, IReadOnlyList<TimesheetMonthPerPersonDto>>>(MockBehavior.Strict);
        mock.Setup(x => x.HandleAsync(It.IsAny<GetTimesheetMonthQuery>(), It.IsAny<CancellationToken>()))
            .Callback<GetTimesheetMonthQuery, CancellationToken>((q, _) => calls.Add(q))
            .ReturnsAsync([Row(today.Year, today.Month, daysInMonth)]);

        Services.AddSingleton(mock.Object);

        var cut = Render<TimesheetShell>();
        cut.WaitForAssertion(() => Assert.True(calls.Count >= 1)); // первинний Reload

        var input = cut.Find("input[placeholder*='Іванов']");

        // act: 1 символ — НЕ перезавантажує
        input.Input("a");

        // assert
        Assert.Single(calls);

        // act: 2 символи — перезавантажує
        input.Input("ab");

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(2, calls.Count);
            Assert.Equal("ab", calls[^1].Search);
        });

        // act: очистили — теж перезавантажує
        input.Input("");

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(3, calls.Count);
            Assert.Null(calls[^1].Search);
        });
    }

    [Fact]
    public void Day_cells_have_expected_css_classes_for_known_codes()
    {
        // arrange
        var today = DateTime.Today;
        var daysInMonth = DateTime.DaysInMonth(today.Year, today.Month);

        var personId = Guid.NewGuid();

        // day1 main=30
        // day2 main=НБ
        // day3 main=ВП
        // day4 main=F100
        // day5 task-only
        var row = Row(
            year: today.Year,
            month: today.Month,
            daysInMonth: daysInMonth,
            personId: personId,
            days:
            [
                Day(personId, 1, TimesheetLane.Main, "30"),
                Day(personId, 2, TimesheetLane.Main, "НБ"),
                Day(personId, 3, TimesheetLane.Main, "ВП"),
                Day(personId, 4, TimesheetLane.Main, "F100"),
                Day(personId, 5, TimesheetLane.Task, "RPT-1"),
            ]);

        var mock = new Mock<IQueryHandler<GetTimesheetMonthQuery, IReadOnlyList<TimesheetMonthPerPersonDto>>>(MockBehavior.Strict);
        mock.Setup(x => x.HandleAsync(It.IsAny<GetTimesheetMonthQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([row]);

        Services.AddSingleton(mock.Object);

        // act
        var cut = Render<TimesheetShell>();

        // assert
        cut.WaitForAssertion(() =>
        {
            var tr = cut.Find("tbody tr");
            var tds = tr.QuerySelectorAll("td").ToList();

            // індекси: 0=Тип,1=ПІБ,2=кнопка, далі day1 => index = day + 2
            Assert.Contains("ts-cell--30", tds[1 + 2].ClassName);
            Assert.Contains("ts-cell--nb", tds[2 + 2].ClassName);
            Assert.Contains("ts-cell--vac", tds[3 + 2].ClassName);
            Assert.Contains("ts-cell--alert", tds[4 + 2].ClassName);

            // task-only має бути ts-cell--task
            Assert.Contains("ts-cell--task", tds[5 + 2].ClassName);
        });
    }

    // =====================================================
    // DTO фабрики під твої record-ctor
    // =====================================================

    private static TimesheetMonthPerPersonDto Row(int year, int month, int daysInMonth)
        => Row(year, month, daysInMonth, Guid.NewGuid(), []);

    private static TimesheetMonthPerPersonDto Row(int year, int month, int daysInMonth, Guid personId, MonthlyTimesheetDayDto[] days)
    {
        var ts = Ts(personId, year, month, days);

        return new TimesheetMonthPerPersonDto(
            PersonId: personId,
            FullName: "Іванов Іван Іванович",
            RNOKPP: "1234567890",
            Rank: "Солдат",
            Position: "Стрілець",
            EnrollmentKind: EnrollmentKind.Unit,
            EnrolledAt: new DateOnly(year, month, 1),
            ExcludedAt: null,
            Timesheet: ts);
    }

    private static MonthlyTimesheetReadModelDto Ts(Guid personId, int year, int month, MonthlyTimesheetDayDto[] days)
        => new(
            PersonId: personId,
            Year: year,
            Month: month,
            UpdatedAtUtc: DateTime.MinValue,
            Days: days);

    private static MonthlyTimesheetDayDto Day(Guid personId, int day, TimesheetLane lane, string code)
        => new(
            EntryId: personId,
            Day: day,
            Lane: lane,
            Code: code);
}
