//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetPersonDrawerTests
//-----------------------------------------------------------------------------

using Bunit;
using eRaven.Application.DTOs.Enums;
using eRaven.Application.DTOs.Timesheets;
using eRaven.Components.Pages.Timesheets.Drawers;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace eRaven.Tests.Components.Pages.Timesheets.Drawers;

public sealed class TimesheetPersonDrawerTests : BunitContext
{
    private readonly Mock<NavigationManager> _nav;

    public TimesheetPersonDrawerTests()
    {
        // Navigation
        _nav = new();
        Services.AddSingleton(_nav.Object);

        // If Drawer/Button components rely on JS, keep it loose.
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void Render_WhenPersonIsNull_ShowsEmptyState_AndCloseButton()
    {
        // arrange + act
        var cut = Render<TimesheetPersonDrawer>(ps => ps
            .Add(p => p.IsOpen, true)
            .Add(p => p.Person, null));

        // assert
        cut.Markup.Contains("Немає даних.");

        var close = cut.FindAll("button").SingleOrDefault(x => x.TextContent.Contains("Закрити"));
        Assert.NotNull(close);
    }

    [Fact]
    public void CloseButton_InvokesIsOpenChangedFalse_AndOnClosed()
    {
        // arrange
        var isOpen = true;
        var onClosedCalls = 0;

        var cut = Render<TimesheetPersonDrawer>(ps => ps
            .Add(p => p.IsOpen, true)
            .Add(p => p.Person, CreatePerson(Guid.NewGuid(), "Alpha"))
            .Add(p => p.IsOpenChanged, EventCallback.Factory.Create<bool>(this, v => isOpen = v))
            .Add(p => p.OnClosed, EventCallback.Factory.Create(this, () => onClosedCalls++)));

        // act
        cut.FindAll("button").Single(x => x.TextContent.Contains("Закрити")).Click();

        // assert
        Assert.False(isOpen);
        Assert.Equal(1, onClosedCalls);
    }

    private static TimesheetPersonInfoDto CreatePerson(Guid id, string fullName)
        => new(
            PersonId: id,
            FullName: fullName,
            Rnokpp: "1234567890",
            Rank: "солдат",
            PositionSort: 1,
            Position: "Командир",
            EnrollmentKindDto: EnrollmentKindDto.Unit,
            EnrolledAt: new DateOnly(2026, 2, 10),
            ExcludedAt: null);
}
