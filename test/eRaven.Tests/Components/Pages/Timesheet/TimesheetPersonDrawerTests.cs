//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetPersonDrawerTests
//-----------------------------------------------------------------------------

using Bunit;
using eRaven.Application.DTOs.Timesheet;
using eRaven.Components.Pages.Timesheet.Drawers;
using eRaven.Domain.Enums;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace eRaven.Tests.Components.Pages.Timesheet;

public sealed class TimesheetPersonDrawerTests : BunitContext
{
    [Fact]
    public void Render_when_person_is_null_shows_placeholder_and_action_button()
    {
        // arrange
        var cut = Render<TimesheetPersonDrawer>(ps => ps
            .Add(p => p.IsOpen, true)
            .Add(p => p.Person, null)
            .Add(p => p.IsOpenChanged, EventCallback.Factory.Create<bool>(this, _ => { }))
            .Add(p => p.OnClosed, EventCallback.Factory.Create(this, () => { }))
        );

        // assert
        cut.Markup.Contains("Картка в табелі");
        cut.Markup.Contains("Немає даних.");
        cut.Markup.Contains("Особовий табель");
        cut.Markup.Contains("Закрити");
    }

    [Fact]
    public void Render_when_person_provided_shows_fields_and_dates()
    {
        // arrange
        var personId = Guid.NewGuid();

        var dto = new TimesheetPersonMonthRowDto(
            PersonId: personId,
            FullName: "Іванов Іван Іванович",
            RNOKPP: "1234567890",
            Rank: "Солдат",
            Position: "Стрілець",
            EnrollmentKind: EnrollmentKind.Unit,
            EnrolledAt: new DateOnly(2026, 01, 10),
            ExcludedAt: null,
            MainCodes: [],
            MainRef: [],
            TaskCodes: []
        );

        var cut = Render<TimesheetPersonDrawer>(ps => ps
            .Add(p => p.IsOpen, true)
            .Add(p => p.Person, dto)
            .Add(p => p.IsOpenChanged, EventCallback.Factory.Create<bool>(this, _ => { }))
            .Add(p => p.OnClosed, EventCallback.Factory.Create(this, () => { }))
        );

        // assert
        cut.Markup.Contains("Іванов Іван Іванович");
        cut.Markup.Contains("1234567890");
        cut.Markup.Contains("Солдат");
        cut.Markup.Contains("Стрілець");

        // EnrollmentKind.Unit => "Штат"
        cut.Markup.Contains("Штат");

        // EnrolledAt formatted dd.MM.yyyy
        cut.Markup.Contains("10.01.2026");
    }

    [Fact]
    public void Click_person_timesheet_navigates_to_person_month_page()
    {
        // arrange
        var nav = Services.GetRequiredService<NavigationManager>();
        Assert.NotNull(nav);

        var personId = Guid.NewGuid();

        var dto = new TimesheetPersonMonthRowDto(
            PersonId: personId,
            FullName: "Іванов Іван",
            RNOKPP: "123",
            Rank: null,
            Position: null,
            EnrollmentKind: EnrollmentKind.Unit,
            EnrolledAt: null,
            ExcludedAt: null,
            MainCodes: [],
            MainRef: [],
            TaskCodes: []
        );

        var cut = Render<TimesheetPersonDrawer>(ps => ps
            .Add(p => p.IsOpen, true)
            .Add(p => p.Person, dto)
            .Add(p => p.IsOpenChanged, EventCallback.Factory.Create<bool>(this, _ => { }))
            .Add(p => p.OnClosed, EventCallback.Factory.Create(this, () => { }))
        );

        // act
        cut.FindAll("button")
            .Single(b => b.TextContent.Contains("Особовий табель", StringComparison.OrdinalIgnoreCase))
            .Click();

        // assert
        Assert.EndsWith($"/timesheet/person/{personId}", nav.Uri, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Click_close_invokes_IsOpenChanged_false_and_OnClosed()
    {
        // arrange
        var personId = Guid.NewGuid();

        var dto = new TimesheetPersonMonthRowDto(
            PersonId: personId,
            FullName: "Іванов Іван",
            RNOKPP: "123",
            Rank: null,
            Position: null,
            EnrollmentKind: EnrollmentKind.Unit,
            EnrolledAt: null,
            ExcludedAt: null,
            MainCodes: [],
            MainRef: [],
            TaskCodes: []
        );

        bool? newIsOpen = null;
        var closed = 0;

        var cut = Render<TimesheetPersonDrawer>(ps => ps
            .Add(p => p.IsOpen, true)
            .Add(p => p.Person, dto)
            .Add(p => p.IsOpenChanged, EventCallback.Factory.Create<bool>(this, v => newIsOpen = v))
            .Add(p => p.OnClosed, EventCallback.Factory.Create(this, () => closed++))
        );

        // act
        cut.FindAll("button")
            .Single(b => b.TextContent.Trim().Equals("Закрити", StringComparison.OrdinalIgnoreCase))
            .Click();

        // assert
        Assert.False(newIsOpen);
        Assert.Equal(1, closed);
    }
}
