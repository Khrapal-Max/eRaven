//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// GetTimesheetTransitionContextQueryHandlerTests
//-----------------------------------------------------------------------------

using eRaven.Application.Abstractions.TimesheetPolicyRepository;
using eRaven.Application.Abstractions.TimesheetRepository;
using eRaven.Application.DTOs.Enums;
using eRaven.Application.Handlers.Timesheets;
using eRaven.Application.Queries.Timesheets;
using eRaven.Domain.Consts;
using eRaven.Domain.Entities;
using eRaven.Domain.Enums;
using Moq;

namespace eRaven.Tests.Application.Handlers.Timesheets;

public sealed class GetTimesheetTransitionContextQueryHandlerTests
{
    private readonly Mock<ITimesheetEntryQueryRepository> _entries = new(MockBehavior.Strict);
    private readonly Mock<ITimesheetPolicyRepository> _policy = new(MockBehavior.Strict);

    [Fact]
    public async Task HandleAsync_WhenEntryMissing_ReturnsDerivedNb_AndSkipsPolicy()
    {
        // arrange
        var personId = Guid.NewGuid();
        var onDate = new DateOnly(2026, 2, 10);

        _entries.Setup(x => x.GetActiveEntryOnDateAsync(personId, onDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TimesheetEntry?)null);

        var handler = new GetTimesheetTransitionContextQueryHandler(_entries.Object, _policy.Object);

        // act
        var ctx = await handler.HandleAsync(new GetTimesheetTransitionContextQuery(personId, onDate));

        // assert
        Assert.Equal(personId, ctx.PersonId);
        Assert.Equal(onDate, ctx.OnDate);

        Assert.Null(ctx.CurrentCodeId);
        Assert.Equal(TimesheetDerivedCodes.NotInTimesheet, ctx.CurrentCode);
        Assert.True(ctx.IsDerived);
        Assert.Null(ctx.CurrentRole);

        Assert.Empty(ctx.TransitionOptions);
        Assert.Empty(ctx.EmergencyOptions);

        _entries.VerifyAll();
        _policy.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_WhenCurrentIsSystemCode_ReturnsEmergencyOptionsOnly()
    {
        // arrange
        var personId = Guid.NewGuid();
        var onDate = new DateOnly(2026, 2, 10);

        var system = CodeDef(
            id: Guid.NewGuid(),
            code: " SYS ",
            title: " System ",
            role: RoleCode.SystemCode,
            ui: TimesheetUiStyle.SystemFact,
            sort: 10,
            priority: 0);

        var entry = Entry(personId, onDate, system);

        _entries.Setup(x => x.GetActiveEntryOnDateAsync(personId, onDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entry);

        // emergency (active), one inactive emergency (must be filtered out), and one normal transition code.
        var e1 = CodeDef(Guid.NewGuid(), " 100 ", "  Alarm 100  ", RoleCode.EmergencyCode, TimesheetUiStyle.Danger, sort: 1, priority: 1);
        var e2 = CodeDef(Guid.NewGuid(), " Ф100 ", "  Alarm Ф100 ", RoleCode.EmergencyCode, TimesheetUiStyle.Danger, sort: 1, priority: 2);
        var eInactive = CodeDef(Guid.NewGuid(), "X", "Inactive", RoleCode.EmergencyCode, TimesheetUiStyle.Danger, sort: 0, priority: 0, active: false);
        var t = CodeDef(Guid.NewGuid(), "T", "Табель", RoleCode.TransitionCode, TimesheetUiStyle.Ready);

        _policy.Setup(x => x.GetCodesAsync(includeInactive: false, It.IsAny<CancellationToken>()))
            .ReturnsAsync([t, e2, eInactive, e1]);

        var handler = new GetTimesheetTransitionContextQueryHandler(_entries.Object, _policy.Object);

        // act
        var ctx = await handler.HandleAsync(new GetTimesheetTransitionContextQuery(personId, onDate));

        // assert
        Assert.False(ctx.IsDerived);
        Assert.Equal(system.Id, ctx.CurrentCodeId);
        Assert.Equal("SYS", ctx.CurrentCode);
        Assert.Equal(RoleCodeDto.SystemCode, ctx.CurrentRole);

        Assert.Empty(ctx.TransitionOptions);

        // ordered by SortOrder, Priority, Code (after trim)
        Assert.Collection(ctx.EmergencyOptions,
            o =>
            {
                Assert.Equal(e1.Id, o.TransitionCodeId);
                Assert.Equal("100", o.Code);
                Assert.Equal("Alarm 100", o.Display);
                Assert.Equal(0, o.StartShiftDays);
            },
            o =>
            {
                Assert.Equal(e2.Id, o.TransitionCodeId);
                Assert.Equal("Ф100", o.Code);
                Assert.Equal("Alarm Ф100", o.Display);
                Assert.Equal(0, o.StartShiftDays);
            });

        _entries.VerifyAll();
        _policy.VerifyAll();
        _policy.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_WhenCurrentIsTransitionCode_ReturnsMatrixTransitionOptions_AndEmergencyOptions()
    {
        // arrange
        var personId = Guid.NewGuid();
        var onDate = new DateOnly(2026, 2, 12);

        var current = CodeDef(
            id: Guid.NewGuid(),
            code: " Т ",
            title: "Табель",
            role: RoleCode.TransitionCode,
            ui: TimesheetUiStyle.Ready);

        _entries.Setup(x => x.GetActiveEntryOnDateAsync(personId, onDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Entry(personId, onDate, current));

        var emergency = CodeDef(Guid.NewGuid(), "100", "Alarm", RoleCode.EmergencyCode, TimesheetUiStyle.Danger, sort: 1, priority: 1);
        _policy.Setup(x => x.GetCodesAsync(includeInactive: false, It.IsAny<CancellationToken>()))
            .ReturnsAsync([emergency, current]);

        // allowed codes: includes 2 transition targets, plus an emergency (must be filtered out from TransitionOptions)
        var to30 = CodeDef(Guid.NewGuid(), " 30 ", " Ready ", RoleCode.TransitionCode, TimesheetUiStyle.Ready);
        var toBR = CodeDef(Guid.NewGuid(), " БР ", "Бойове", RoleCode.TransitionCode, TimesheetUiStyle.Warning);
        var toEmergency = CodeDef(Guid.NewGuid(), "100", "Alarm", RoleCode.EmergencyCode, TimesheetUiStyle.Danger);

        var rules = new List<TimesheetCodeTransition>
        {
            Rule(current.Id, toBR, shift: 0),
            Rule(current.Id, to30, shift: 1),
            Rule(current.Id, toEmergency, shift: 0), // must NOT go to TransitionOptions
        };

        _policy.Setup(x => x.GetAllowedCodesAsync(current.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rules);

        var handler = new GetTimesheetTransitionContextQueryHandler(_entries.Object, _policy.Object);

        // act
        var ctx = await handler.HandleAsync(new GetTimesheetTransitionContextQuery(personId, onDate));

        // assert
        Assert.False(ctx.IsDerived);
        Assert.Equal(current.Id, ctx.CurrentCodeId);
        Assert.Equal("Т", ctx.CurrentCode);
        Assert.Equal(RoleCodeDto.TransitionCode, ctx.CurrentRole);

        // TransitionOptions sorted by Code
        Assert.Collection(ctx.TransitionOptions,
            o =>
            {
                Assert.Equal(to30.Id, o.TransitionCodeId);
                Assert.Equal("30", o.Code);
                Assert.Equal("Ready", o.Display);
                Assert.Equal(1, o.StartShiftDays);
            },
            o =>
            {
                Assert.Equal(toBR.Id, o.TransitionCodeId);
                Assert.Equal("БР", o.Code);
                Assert.Equal("Бойове", o.Display);
                Assert.Equal(0, o.StartShiftDays);
            });

        Assert.Single(ctx.EmergencyOptions);
        Assert.Equal(emergency.Id, ctx.EmergencyOptions[0].TransitionCodeId);
        Assert.Equal("100", ctx.EmergencyOptions[0].Code);
        Assert.Equal("Alarm", ctx.EmergencyOptions[0].Display);
        Assert.Equal(0, ctx.EmergencyOptions[0].StartShiftDays);

        _entries.VerifyAll();
        _policy.VerifyAll();
        _policy.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_WhenPersonIdEmpty_Throws()
    {
        var handler = new GetTimesheetTransitionContextQueryHandler(_entries.Object, _policy.Object);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.HandleAsync(new GetTimesheetTransitionContextQuery(Guid.Empty, new DateOnly(2026, 2, 1))));

        Assert.Contains("PersonId", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_WhenOnDateDefault_Throws()
    {
        var handler = new GetTimesheetTransitionContextQueryHandler(_entries.Object, _policy.Object);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.HandleAsync(new GetTimesheetTransitionContextQuery(Guid.NewGuid(), default)));

        Assert.Contains("OnDate", ex.Message);
    }

    //======================================================================
    // Helpers
    //======================================================================

    private static TimesheetEntry Entry(Guid personId, DateOnly from, TimesheetCodeDefinition def)
        => new()
        {
            Id = Guid.NewGuid(),
            PersonId = personId,
            TimesheetCodeDefinitionId = def.Id,
            TimesheetCodeDefinition = def,
            From = from,
            To = null,
            CreatedBy = "test",
            CreatedAtUtc = DateTime.UtcNow
        };

    private static TimesheetCodeDefinition CodeDef(
        Guid id,
        string code,
        string title,
        RoleCode role,
        TimesheetUiStyle ui,
        int sort = 0,
        int priority = 0,
        bool active = true)
        => new()
        {
            Id = id,
            Code = code,
            Title = title,
            RoleCode = role,
            UiStyle = ui,
            SortOrder = sort,
            Priority = priority,
            IsActive = active
        };

    private static TimesheetCodeTransition Rule(Guid fromCodeId, TimesheetCodeDefinition to, int shift)
        => new()
        {
            Id = Guid.NewGuid(),
            FromCodeId = fromCodeId,
            ToCodeId = to.Id,
            ToCode = to,
            StartShiftDays = shift,
            CreatedBy = "test",
            CreatedAtUtc = DateTime.UtcNow
        };
}