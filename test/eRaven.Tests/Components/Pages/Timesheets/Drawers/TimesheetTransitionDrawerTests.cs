//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetTransitionDrawerTests
//-----------------------------------------------------------------------------

using Bunit;
using eRaven.Application.Commands;
using eRaven.Application.Commands.Timesheets;
using eRaven.Application.DTOs.Enums;
using eRaven.Application.DTOs.Timesheets.Policy;
using eRaven.Application.Queries;
using eRaven.Application.Queries.Timesheets;
using eRaven.Components.Pages.Timesheets.Drawers;
using eRaven.Domain.Consts;
using eRaven.Presentation.Toasts;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace eRaven.Tests.Components.Pages.Timesheets.Drawers;

public sealed class TimesheetTransitionDrawerTests : BunitContext
{
    private readonly ToastService _toast = new();

    private readonly Mock<ICommandHandler<TransitionTimesheetStateCommand, Guid>> _transition;
    private readonly Mock<IQueryHandler<GetTimesheetTransitionContextQuery, TimesheetTransitionContextDto>> _context;

    public TimesheetTransitionDrawerTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        _transition = new Mock<ICommandHandler<TransitionTimesheetStateCommand, Guid>>(MockBehavior.Strict);
        _context = new Mock<IQueryHandler<GetTimesheetTransitionContextQuery, TimesheetTransitionContextDto>>(MockBehavior.Strict);

        Services.AddSingleton(_toast);
        Services.AddSingleton(_transition.Object);
        Services.AddSingleton(_context.Object);
    }

    [Fact]
    public void Render_UsesParameters_AndLoadsContext_ForInitialDate()
    {
        // arrange
        var personId = Guid.NewGuid();
        var operatorDate = new DateOnly(2026, 2, 10);
        var initialDate = new DateOnly(2026, 2, 12);

        _context
            .Setup(q => q.HandleAsync(
                It.Is<GetTimesheetTransitionContextQuery>(x => x.PersonId == personId && x.OnDate == initialDate),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateContext(personId, initialDate, isDerived: false, isSystem: false));

        // act
        var cut = Render<TimesheetTransitionDrawer>(ps => ps
            .Add(p => p.IsOpen, true)
            .Add(p => p.PersonId, personId)
            .Add(p => p.InitialDate, initialDate)
            .Add(p => p.OperatorDate, operatorDate)
            .Add(p => p.PersonLabel, "Alpha"));

        // assert
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Контекст:", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("Alpha", cut.Markup, StringComparison.Ordinal);
            Assert.Contains(initialDate.ToString("yyyy-MM-dd"), cut.Markup, StringComparison.Ordinal);
            Assert.Contains(operatorDate.ToString("yyyy-MM-dd"), cut.Markup, StringComparison.Ordinal);

            Assert.Contains("Нова подія", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("Дата події", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("Наступний код", cut.Markup, StringComparison.Ordinal);

            Assert.NotEmpty(cut.FindAll("input"));
            Assert.NotEmpty(cut.FindAll("select"));

            Assert.Contains("Застосувати", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("Скасувати", cut.Markup, StringComparison.Ordinal);

            Assert.NotNull(cut.Instance.GetTimesheetTransitionContextQueryHandler);
            Assert.NotNull(cut.Instance.TransitionTimesheetStateCommandHandler);
            Assert.NotNull(cut.Instance.ToastService);
        });

        _context.VerifyAll();
    }

    [Fact]
    public void CancelButton_InvokesIsOpenChangedFalse()
    {
        // arrange
        var personId = Guid.NewGuid();
        var operatorDate = new DateOnly(2026, 2, 10);
        var initialDate = new DateOnly(2026, 2, 12);

        _context
            .Setup(q => q.HandleAsync(It.IsAny<GetTimesheetTransitionContextQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateContext(personId, initialDate, isDerived: false, isSystem: false));

        var isOpenChangedCalls = 0;
        bool? lastIsOpen = null;

        var cut = Render<TimesheetTransitionDrawer>(ps => ps
            .Add(p => p.IsOpen, true)
            .Add(p => p.PersonId, personId)
            .Add(p => p.InitialDate, initialDate)
            .Add(p => p.OperatorDate, operatorDate)
            .Add(p => p.IsOpenChanged, EventCallback.Factory.Create<bool>(this, v =>
            {
                isOpenChangedCalls++;
                lastIsOpen = v;
            })));

        cut.WaitForAssertion(() => Assert.Contains("Нова подія", cut.Markup, StringComparison.Ordinal));

        // act
        cut.FindAll("button")
            .Single(x => x.TextContent.Contains("Скасувати", StringComparison.Ordinal))
            .Click();

        // assert
        Assert.Equal(1, isOpenChangedCalls);
        Assert.False(lastIsOpen);
    }

    [Fact]
    public void ApplyButton_InvokesOnApplied_AndClosesDrawer()
    {
        // arrange
        var personId = Guid.NewGuid();
        var operatorDate = new DateOnly(2026, 2, 10);
        var initialDate = new DateOnly(2026, 2, 12);

        var ctx = CreateContext(personId, initialDate, isDerived: false, isSystem: false);

        _context
            .Setup(q => q.HandleAsync(It.IsAny<GetTimesheetTransitionContextQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ctx);

        var selected = ctx.TransitionOptions[0];

        _transition
            .Setup(c => c.HandleAsync(
                It.Is<TransitionTimesheetStateCommand>(cmd =>
                    cmd.PersonId == personId
                    && cmd.AnchorDate == operatorDate
                    && cmd.InputDate == initialDate
                    && cmd.NextCode == selected.TransitionCodeId
                    && cmd.IsCorrection == false),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid());

        var appliedCalls = 0;
        var isOpenChangedCalls = 0;
        bool? lastIsOpen = null;

        var cut = Render<TimesheetTransitionDrawer>(ps => ps
            .Add(p => p.IsOpen, true)
            .Add(p => p.PersonId, personId)
            .Add(p => p.InitialDate, initialDate)
            .Add(p => p.OperatorDate, operatorDate)
            .Add(p => p.OnApplied, EventCallback.Factory.Create(this, () => appliedCalls++))
            .Add(p => p.IsOpenChanged, EventCallback.Factory.Create<bool>(this, v =>
            {
                isOpenChangedCalls++;
                lastIsOpen = v;
            })));

        cut.WaitForAssertion(() => Assert.Contains("Нова подія", cut.Markup, StringComparison.Ordinal));

        // act
        cut.FindAll("button")
            .Single(x => x.TextContent.Contains("Застосувати", StringComparison.Ordinal))
            .Click();

        // assert
        cut.WaitForAssertion(() =>
        {
            Assert.Equal(1, appliedCalls);
            Assert.Equal(1, isOpenChangedCalls);
            Assert.False(lastIsOpen);
        });

        _transition.VerifyAll();
        _context.VerifyAll();
    }

    private static TimesheetTransitionContextDto CreateContext(Guid personId, DateOnly onDate, bool isDerived, bool isSystem)
    {
        var role = isDerived
            ? (RoleCodeDto?)null
            : isSystem
                ? RoleCodeDto.SystemCode
                : RoleCodeDto.TransitionCode;

        var transitionOptions = isDerived || isSystem
            ? []
            : new[]
            {
                new TimesheetTransitionOptionDto(
                    TransitionCodeId: Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    Code: "30",
                    Display: "Готовність",
                    StartShiftDays: 0)
            };

        return new TimesheetTransitionContextDto(
            PersonId: personId,
            OnDate: onDate,
            CurrentCodeId: isDerived ? null : Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            CurrentCode: isDerived ? TimesheetDerivedCodes.NotInTimesheet : "Т",
            IsDerived: isDerived,
            CurrentRole: role,
            TransitionOptions: transitionOptions,
            EmergencyOptions: []);
    }
}
