//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetWeekShellTests
//-----------------------------------------------------------------------------

using Bunit;
using eRaven.Application.Commands;
using eRaven.Application.Commands.Timesheets;
using eRaven.Application.DTOs.Enums;
using eRaven.Application.DTOs.Timesheets;
using eRaven.Application.DTOs.Timesheets.Policy;
using eRaven.Application.Queries;
using eRaven.Application.Queries.Timesheets;
using eRaven.Components.Pages.Timesheets;
using eRaven.Components.Pages.Timesheets.Drawers;
using eRaven.Domain.Consts;
using eRaven.Presentation.Toasts;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Globalization;

namespace eRaven.Tests.Components.Pages.Timesheets;

public sealed class TimesheetWeekShellTests : BunitContext
{
    private readonly Mock<IQueryHandler<GetTimesheetsRangeQuery, IReadOnlyList<TimesheetPersonRangeRowDto>>> _query = new(MockBehavior.Strict);
    private readonly Mock<IQueryHandler<GetTimesheetTransitionContextQuery, TimesheetTransitionContextDto>> _context = new(MockBehavior.Strict);
    private readonly Mock<ICommandHandler<TransitionTimesheetStateCommand, Guid>> _transition = new(MockBehavior.Strict);

    public TimesheetWeekShellTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        Services.AddSingleton(_query.Object);
        Services.AddSingleton(_context.Object);
        Services.AddSingleton(_transition.Object);

        // ToastService has parameterless ctor in the project.
        Services.AddSingleton(new ToastService());
    }

    [Fact]
    public void Render_ShowsCoreControls_AndWeekHeaderHas7Days()
    {
        // arrange
        var today = DateTime.Today;
        var anchor = DateOnly.FromDateTime(today);
        var from = anchor.AddDays(-2);
        var to = anchor.AddDays(4);

        _query
            .Setup(q => q.HandleAsync(
                It.Is<GetTimesheetsRangeQuery>(x => x.From == from && x.To == to && x.Search == null),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        // act
        var cut = Render<TimesheetWeekShell>();

        // assert
        cut.WaitForAssertionAsync(() =>
        {
            Assert.Contains("Операційний табель (тиждень)", cut.Markup, StringComparison.Ordinal);
            Assert.Contains(from.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture), cut.Markup, StringComparison.Ordinal);
            Assert.Contains(to.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture), cut.Markup, StringComparison.Ordinal);

            // date navigation + search
            Assert.Contains("Сьогодні", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("Оновити", cut.Markup, StringComparison.Ordinal);
            Assert.NotNull(cut.Find("input[placeholder='Пошук: ПІБ або РНОКПП']"));

            // header cells for 7 days
            var dayHeaders = cut.FindAll("th.ts-day");
            Assert.Equal(7, dayHeaders.Count);

            // exactly one anchor header
            var anchorHeaders = cut.FindAll("th.ts-day.bg-primary.text-white");
            Assert.Single(anchorHeaders);
        });

        Assert.NotNull(cut.Instance.GetTimesheetRangeQueryHandler);
        Assert.NotNull(cut.Instance.ToastService);
        _query.VerifyAll();
    }

    [Fact]
    public void InjectedQuery_IsCalledOnInit_AndSearchReloadRespectsThreshold()
    {
        // arrange
        var today = DateTime.Today;
        var anchor = DateOnly.FromDateTime(today);
        var from = anchor.AddDays(-2);
        var to = anchor.AddDays(4);

        var calls = new List<GetTimesheetsRangeQuery>();

        _query
            .Setup(q => q.HandleAsync(It.IsAny<GetTimesheetsRangeQuery>(), It.IsAny<CancellationToken>()))
            .Callback<GetTimesheetsRangeQuery, CancellationToken>((q, _) => calls.Add(q))
            .ReturnsAsync([]);

        // act
        var cut = Render<TimesheetWeekShell>();

        // assert init
        cut.WaitForAssertion(() => Assert.Single(calls));
        Assert.Equal(from, calls[0].From);
        Assert.Equal(to, calls[0].To);
        Assert.Null(calls[0].Search);

        var search = cut.Find("input[placeholder='Пошук: ПІБ або РНОКПП']");

        // 1 char => no reload
        search.Input("a");
        cut.WaitForAssertion(() => Assert.Single(calls));

        // 2 chars => reload
        search.Input("ab");
        cut.WaitForAssertion(() => Assert.Equal(2, calls.Count));
        Assert.Equal("ab", calls[1].Search);
        Assert.Equal(from, calls[1].From);
        Assert.Equal(to, calls[1].To);

        // clear => reload
        search.Input("");
        cut.WaitForAssertion(() => Assert.Equal(3, calls.Count));
        Assert.Null(calls[2].Search);

        _query.VerifyAll();
    }

    [Fact]
    public void ClickCreateEvent_OpensTransitionDrawer_WithExpectedParameters()
    {
        // arrange
        var today = DateTime.Today;
        var anchor = DateOnly.FromDateTime(today);
        var from = anchor.AddDays(-2);
        var to = anchor.AddDays(4);

        var personId = Guid.NewGuid();

        var row = CreateRowForWeek(personId, "Alpha", "1234567890", EnrollmentKindDto.Unit, from, anchor);

        _query
            .Setup(q => q.HandleAsync(
                It.Is<GetTimesheetsRangeQuery>(x => x.From == from && x.To == to && x.Search == null),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([row]);

        var cut = Render<TimesheetWeekShell>();

        cut.WaitForAssertion(() =>
        {
            // plus button enabled
            var plus = cut.Find("button[title='Створити подію']");
            Assert.False(plus.HasAttribute("disabled"));
        });

        // act
        cut.Find("button[title='Створити подію']").Click();

        // assert
        cut.WaitForAssertion(() =>
        {
            var drawer = cut.FindComponent<TimesheetTransitionDrawer>();
            Assert.True(drawer.Instance.IsOpen);
            Assert.Equal(personId, drawer.Instance.PersonId);
            Assert.Equal(anchor, drawer.Instance.InitialDate);
            Assert.Equal(anchor, drawer.Instance.OperatorDate);
            Assert.Contains("1234567890", drawer.Instance.PersonLabel ?? string.Empty, StringComparison.Ordinal);
        });

        _query.VerifyAll();
    }

    [Fact]
    public void CreateEventButton_IsDisabled_WhenAnchorIsDerivedNb()
    {
        // arrange
        var today = DateTime.Today;
        var anchor = DateOnly.FromDateTime(today);
        var from = anchor.AddDays(-2);
        var to = anchor.AddDays(4);

        var personId = Guid.NewGuid();

        var row = CreateRowForWeek(personId, "Alpha", "1234567890", EnrollmentKindDto.Unit, from, anchor, anchorIsDerived: true);

        _query
            .Setup(q => q.HandleAsync(
                It.Is<GetTimesheetsRangeQuery>(x => x.From == from && x.To == to && x.Search == null),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([row]);

        // act
        var cut = Render<TimesheetWeekShell>();

        // assert
        cut.WaitForAssertion(() =>
        {
            var plus = cut.Find("button[title='Створити подію']");
            Assert.True(plus.HasAttribute("disabled"));
        });

        _query.VerifyAll();
    }

    //======================================================================
    // Helpers
    //======================================================================

    private static TimesheetPersonRangeRowDto CreateRowForWeek(
        Guid personId,
        string fullName,
        string rnokpp,
        EnrollmentKindDto kind,
        DateOnly from,
        DateOnly anchor,
        bool anchorIsDerived = false)
    {
        var person = new TimesheetPersonInfoDto(
            PersonId: personId,
            FullName: fullName,
            Rnokpp: rnokpp,
            Rank: null,
            PositionSort: 1,
            Position: null,
            EnrollmentKindDto: kind,
            EnrolledAt: anchor,
            ExcludedAt: null);

        var days = new List<TimesheetDaySnapshotDto>(capacity: 7);
        for (var i = 0; i < 7; i++)
        {
            var d = from.AddDays(i);

            if (d == anchor && anchorIsDerived)
            {
                days.Add(new TimesheetDaySnapshotDto(
                    Date: d,
                    CodeId: null,
                    Code: TimesheetDerivedCodes.NotInTimesheet,
                    Reference: null,
                    Note: null,
                    IsDerived: true,
                    IsChangePoint: false,
                    UiStyle: TimesheetUiStyleDto.NotInTimesheet));
            }
            else
            {
                days.Add(new TimesheetDaySnapshotDto(
                    Date: d,
                    CodeId: Guid.NewGuid(),
                    Code: "Т",
                    Reference: null,
                    Note: null,
                    IsDerived: false,
                    IsChangePoint: d == anchor,
                    UiStyle: TimesheetUiStyleDto.Ready));
            }
        }

        return new TimesheetPersonRangeRowDto(person, days);
    }
}
