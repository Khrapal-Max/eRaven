//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetShellTests
//-----------------------------------------------------------------------------

using Bunit;
using Bunit.TestDoubles;
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

    private static TimesheetMonthPerPersonDto BuildOneRow(int year, int month)
    {
        var personId = Guid.NewGuid();

        var ts = new MonthlyTimesheetReadModelDto(
            PersonId: personId,
            Year: year,
            Month: month,
            UpdatedAtUtc: new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc),
            Days:
            [
                new(null, 1, TimesheetLane.Main, "30"),
                new(null, 2, TimesheetLane.Main, "30"),
                new(null, 2, TimesheetLane.Task, "RPT-1"),
                new(null, 3, TimesheetLane.Main, "НБ"),
            ]);

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

    [Fact]
    public void OnInitialized_calls_query_and_renders_toolbar_controls_year_month_search_export_refresh()
    {
        // arrange
        var today = DateTime.Today;

        var calls = new List<GetTimesheetMonthQuery>();

        var mock = new Mock<IQueryHandler<GetTimesheetMonthQuery, IReadOnlyList<TimesheetMonthPerPersonDto>>>(MockBehavior.Strict);
        mock.Setup(x => x.HandleAsync(It.IsAny<GetTimesheetMonthQuery>(), It.IsAny<CancellationToken>()))
            .Callback<GetTimesheetMonthQuery, CancellationToken>((q, _) => calls.Add(q))
            .ReturnsAsync([]);

        Services.AddSingleton(mock.Object);

        // act
        var cut = Render<TimesheetShell>();

        // assert: handler
        cut.WaitForAssertion(() =>
        {
            Assert.Single(calls);
            Assert.Equal(today.Year, calls[0].Year);
            Assert.Equal(today.Month, calls[0].Month);
            Assert.Null(calls[0].Search);
        });

        // assert: toolbar controls exist + значення
        // Year input
        var yearInput = cut.Find("input[type='number']");
        Assert.Equal(today.Year.ToString(), yearInput.GetAttribute("value"));

        // Month select (перевіряємо value або selected option)
        var monthSelect = cut.Find("select");
        var monthValueAttr = monthSelect.GetAttribute("value");

        if (!string.IsNullOrWhiteSpace(monthValueAttr))
        {
            Assert.Equal(today.Month.ToString(), monthValueAttr);
        }
        else
        {
            // fallback: шукаємо selected option
            var selected = monthSelect.QuerySelector("option[selected]");
            Assert.NotNull(selected);
            Assert.Equal(today.Month.ToString(), selected!.GetAttribute("value"));
        }

        // Search input
        var searchInput = cut.Find("input[placeholder*='Іванов']");
        Assert.NotNull(searchInput);

        // Export component present (stub)
        _ = cut.FindComponent<Stub<TimesheetExport>>();

        // Refresh button present
        // (твій shared Button рендерить звичайний <button>, тож просто шукаємо по тексту)
        Assert.Contains(cut.FindAll("button"), b => b.TextContent.Contains("Оновити", StringComparison.OrdinalIgnoreCase));

        Assert.NotNull(cut.Instance.Query);
    }

    [Fact]
    public void Renders_table_with_one_row_and_day_cells()
    {
        // arrange
        var today = DateTime.Today;
        var daysInMonth = DateTime.DaysInMonth(today.Year, today.Month);

        var row = BuildOneRow(today.Year, today.Month);

        var calls = new List<GetTimesheetMonthQuery>();

        var mock = new Mock<IQueryHandler<GetTimesheetMonthQuery, IReadOnlyList<TimesheetMonthPerPersonDto>>>(MockBehavior.Strict);
        mock.Setup(x => x.HandleAsync(It.IsAny<GetTimesheetMonthQuery>(), It.IsAny<CancellationToken>()))
            .Callback<GetTimesheetMonthQuery, CancellationToken>((q, _) => calls.Add(q))
            .ReturnsAsync([row]);

        Services.AddSingleton(mock.Object);

        // act
        var cut = Render<TimesheetShell>();

        // assert
        cut.WaitForAssertion(() =>
        {
            // 1) таблиця є
            var table = cut.Find("table");
            Assert.Contains("timesheet-table", table.ClassName);

            // 2) хедер має 3 службові колонки + дні місяця
            var ths = cut.FindAll("thead th");
            Assert.Equal(3 + daysInMonth, ths.Count);
            Assert.Contains(ths, th => th.TextContent.Trim() == "Тип");
            Assert.Contains(ths, th => th.TextContent.Trim() == "ПІБ");

            // 3) 1 рядок
            var tr = cut.Find("tbody tr");
            var tds = tr.QuerySelectorAll("td").ToList();

            // 3 службові + дні
            Assert.Equal(3 + daysInMonth, tds.Count);

            // 4) перші 3 колонки: Тип / ПІБ-ініціали / кнопка деталей
            Assert.Equal("ШТ", tds[0].TextContent.Trim());               // EnrollmentKind.Unit => ШТ
            Assert.Contains("Іванов І.І.", tds[1].TextContent);          // ToInitials
            Assert.Contains("...", tds[2].TextContent);                  // shared Button label

            // 5) день 1: main=30 => клас 30 і текст 30
            var day1 = tds[3];
            Assert.Contains("ts-cell--30", day1.ClassName);
            Assert.Contains("30", day1.TextContent);

            // 6) день 2: main=30 + task=RPT-1 => has-task, і task скорочений
            var day2 = tds[4];
            Assert.Contains("ts-cell--30", day2.ClassName);
            Assert.Contains("ts-cell--has-task", day2.ClassName);
            Assert.Contains("30", day2.TextContent);
            Assert.Contains("RPT-", day2.TextContent); // "RPT-…" (може бути з … залежно від рендера)

            // 7) день 3: main=НБ => клас nb і текст НБ
            var day3 = tds[5];
            Assert.Contains("ts-cell--nb", day3.ClassName);
            Assert.Contains("НБ", day3.TextContent);
        });

        // додатково: переконаємось, що хендлер викликався на today.Year/today.Month
        Assert.Single(calls);
        Assert.Equal(today.Year, calls[0].Year);
        Assert.Equal(today.Month, calls[0].Month);
    }

    [Fact]
    public void Click_details_button_opens_person_modal_with_correct_person()
    {
        // arrange
        var today = DateTime.Today;

        var row = BuildOneRow(today.Year, today.Month);

        var mock = new Mock<IQueryHandler<GetTimesheetMonthQuery, IReadOnlyList<TimesheetMonthPerPersonDto>>>(MockBehavior.Strict);
        mock.Setup(x => x.HandleAsync(It.IsAny<GetTimesheetMonthQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([row]);

        Services.AddSingleton(mock.Object);

        // потрібен контроль параметрів модала
        ComponentFactories.AddStub<TimesheetPersonModal>();

        // act
        var cut = Render<TimesheetShell>();

        // дочекаймось рядка
        cut.WaitForElement("tbody tr");

        // modal initially closed
        var modal0 = cut.FindComponent<Stub<TimesheetPersonModal>>();

        // IsOpen завжди передається (булевий), тому можна так:
        Assert.False((bool)modal0.Instance.Parameters["IsOpen"]!);

        // Person передається як null на старті:
        Assert.Null(modal0.Instance.Parameters["Person"]);

        cut.Find("tbody tr td:nth-child(3) button").Click();

        cut.WaitForAssertion(() =>
        {
            var modal = cut.FindComponent<Stub<TimesheetPersonModal>>();
            Assert.True((bool)modal.Instance.Parameters["IsOpen"]!);

            var p = (TimesheetMonthPerPersonDto?)modal.Instance.Parameters["Person"];
            Assert.NotNull(p);
            Assert.Equal(row.PersonId, p!.PersonId);
        });
    }
}
