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

namespace eRaven.Tests.Components.Pages.Timesheet.Drawers;

public sealed class TimesheetPersonDrawerTests : BunitContext
{
    public TimesheetPersonDrawerTests()
    {
        // Drawer робить FocusAsync() -> JS interop, тому робимо loose.
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void Render_Open_PersonNull_ShowsEmptyState()
    {
        // arrange
        var cut = Render<TimesheetPersonDrawer>(ps => ps
            .Add(p => p.IsOpen, true)
            .Add(p => p.Person, null)
            .Add(p => p.IsOpenChanged, EventCallback.Factory.Create<bool>(this, _ => { }))
            .Add(p => p.OnClosed, EventCallback.Factory.Create(this, () => { }))
        );

        // assert
        Assert.Contains("drawer-panel show", cut.Markup);
        Assert.Contains("Немає даних.", cut.Markup);
        Assert.Contains("Картка в табелі", cut.Markup);
    }

    [Fact]
    public void Render_Open_PersonProvided_RendersFields()
    {
        // arrange
        var personId = Guid.NewGuid();
        var updatedAt = new DateTime(2026, 01, 22, 12, 34, 56, DateTimeKind.Utc);

        var ts = new MonthlyTimesheetReadModelDto(
            PersonId: personId,
            Year: 2026,
            Month: 1,
            UpdatedAtUtc: updatedAt,
            Days: []);

        var person = new TimesheetMonthPerPersonDto(
            PersonId: personId,
            FullName: "Іванов Іван Іванович",
            RNOKPP: "1234567890",
            Rank: "Солдат",
            Position: "Стрілець",
            EnrollmentKind: EnrollmentKind.Unit,
            EnrolledAt: new DateOnly(2025, 12, 1),
            ExcludedAt: null,
            Timesheet: ts);

        var cut = Render<TimesheetPersonDrawer>(ps => ps
            .Add(p => p.IsOpen, true)
            .Add(p => p.Person, person)
            .Add(p => p.IsOpenChanged, EventCallback.Factory.Create<bool>(this, _ => { }))
            .Add(p => p.OnClosed, EventCallback.Factory.Create(this, () => { }))
        );

        // assert
        Assert.Contains("Іванов Іван Іванович", cut.Markup);
        Assert.Contains("1234567890", cut.Markup);
        Assert.Contains("Солдат", cut.Markup);
        Assert.Contains("Стрілець", cut.Markup);
        Assert.Contains(personId.ToString(), cut.Markup);

        // EnrollmentKind показуємо через ToString()
        Assert.Contains("Штат", cut.Markup);

        // "u" формат для UTC
        Assert.Contains(updatedAt.ToString("u"), cut.Markup);
    }

    [Fact]
    public void CloseButton_Click_InvokesIsOpenChangedFalse_AndOnClosed()
    {
        // arrange
        var person = new TimesheetMonthPerPersonDto(
            PersonId: Guid.NewGuid(),
            FullName: "Петров Петро Петрович",
            RNOKPP: "0987654321",
            Rank: null,
            Position: null,
            EnrollmentKind: null,
            EnrolledAt: null,
            ExcludedAt: null,
            Timesheet: null);

        bool? isOpenChanged = null;
        var closedCalled = false;

        var cut = Render<TimesheetPersonDrawer>(ps => ps
            .Add(p => p.IsOpen, true)
            .Add(p => p.Person, person)
            .Add(p => p.IsOpenChanged, EventCallback.Factory.Create<bool>(this, v => isOpenChanged = v))
            .Add(p => p.OnClosed, EventCallback.Factory.Create(this, () => closedCalled = true))
        );

        // act: тиснемо саме кнопку “Закрити” у футері (не X у хедері)
        var btn = cut.FindAll("button")
            .FirstOrDefault(b => b.TextContent.Contains("Закрити"));

        btn?.Click();

        // assert (асинхронщина всередині Drawer.CloseAsync)
        cut.WaitForAssertion(() =>
        {
            Assert.Equal(false, isOpenChanged);
            Assert.True(closedCalled);
        });
    }
}
