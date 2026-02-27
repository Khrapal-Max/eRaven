//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TransitionTimesheetStateCommandHandlerTests
//-----------------------------------------------------------------------------

using eRaven.Application.Abstractions.TimesheetPolicyRepository;
using eRaven.Application.Abstractions.TimesheetRepository;
using eRaven.Application.Commands.Timesheets;
using eRaven.Application.Handlers.Timesheets;
using eRaven.Domain.Entities;
using eRaven.Domain.Enums;
using Moq;

namespace eRaven.Tests.Application.Handlers.Timesheets;

public sealed class TransitionTimesheetStateCommandHandlerTests
{
    private readonly Mock<ITimesheetEntryQueryRepository> _entries = new(MockBehavior.Strict);
    private readonly Mock<ITimesheetEntryWriterRepository> _writer = new(MockBehavior.Strict);
    private readonly Mock<ITimesheetPolicyRepository> _policy = new(MockBehavior.Strict);

    [Fact]
    public async Task HandleAsync_WhenPrevEntryMissing_Throws()
    {
        // arrange
        var personId = Guid.NewGuid();

        var command = new TransitionTimesheetStateCommand(
            PersonId: personId,
            AnchorDate: new DateOnly(2026, 02, 10),
            InputDate: new DateOnly(2026, 02, 10),
            NextCode: Guid.NewGuid(),
            Reference: null,
            Note: null,
            IsCorrection: false,
            Author: "tester",
            NowUtc: new DateTime(2026, 02, 10, 10, 0, 0, DateTimeKind.Utc));

        _entries.Setup(r => r.GetActiveEntryOnDateAsync(personId, command.InputDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TimesheetEntry?)null);

        var sut = new TransitionTimesheetStateCommandHandler(_entries.Object, _writer.Object, _policy.Object);

        // act
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.HandleAsync(command));

        // assert
        Assert.Contains("немає активного запису", ex.Message, StringComparison.OrdinalIgnoreCase);
        _entries.VerifyAll();
        _writer.VerifyNoOtherCalls();
        _policy.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_UsesInputDateForPrevEntryLookup_WhenAnchorDiffers()
    {
        // arrange
        var personId = Guid.NewGuid();
        var prevDefId = Guid.NewGuid();
        var nextDefId = Guid.NewGuid();

        var prevDef = new TimesheetCodeDefinition { Id = prevDefId, Code = "Т", RoleCode = RoleCode.TransitionCode };
        var nextDef = new TimesheetCodeDefinition { Id = nextDefId, Code = "100", RoleCode = RoleCode.EmergencyCode };

        var prevEntry = new TimesheetEntry
        {
            Id = Guid.NewGuid(),
            PersonId = personId,
            TimesheetCodeDefinitionId = prevDefId,
            TimesheetCodeDefinition = prevDef,
            From = new DateOnly(2026, 02, 01),
            CreatedAtUtc = new DateTime(2026, 02, 01, 0, 0, 0, DateTimeKind.Utc)
        };

        // AnchorDate is only for correction guard.
        var command = new TransitionTimesheetStateCommand(
            PersonId: personId,
            AnchorDate: new DateOnly(2026, 02, 10),
            InputDate: new DateOnly(2026, 02, 15),
            NextCode: nextDefId,
            Reference: "R",
            Note: "N",
            IsCorrection: false,
            Author: "tester",
            NowUtc: new DateTime(2026, 02, 10, 10, 0, 0, DateTimeKind.Utc));

        // IMPORTANT: current entry must be loaded by InputDate (not AnchorDate).
        _entries.Setup(r => r.GetActiveEntryOnDateAsync(personId, command.InputDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(prevEntry);

        _policy.Setup(r => r.GetCodesAsync(includeInactive: true, It.IsAny<CancellationToken>()))
            .ReturnsAsync([prevDef, nextDef]);

        _writer.Setup(w => w.TransitionAsync(
                personId,
                command.InputDate,
                nextDefId,
                command.Reference,
                command.Note,
                command.Author,
                command.NowUtc,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid());

        var sut = new TransitionTimesheetStateCommandHandler(_entries.Object, _writer.Object, _policy.Object);

        // act
        await sut.HandleAsync(command);

        // assert
        _entries.VerifyAll();
        _policy.VerifyAll();
        _writer.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenNotCorrection_AndInputBeforeAnchor_Throws_BeforeLoadingPrevEntry()
    {
        // arrange
        var personId = Guid.NewGuid();

        var command = new TransitionTimesheetStateCommand(
            PersonId: personId,
            AnchorDate: new DateOnly(2026, 02, 10),
            InputDate: new DateOnly(2026, 02, 09),
            NextCode: Guid.NewGuid(),
            Reference: null,
            Note: null,
            IsCorrection: false,
            Author: "tester",
            NowUtc: new DateTime(2026, 02, 10, 10, 0, 0, DateTimeKind.Utc));

        var sut = new TransitionTimesheetStateCommandHandler(_entries.Object, _writer.Object, _policy.Object);

        // act
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.HandleAsync(command));

        // assert
        Assert.Contains("раніше операційної", ex.Message, StringComparison.OrdinalIgnoreCase);
        _entries.VerifyNoOtherCalls();
        _policy.VerifyNoOtherCalls();
        _writer.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_WhenNextCodeIsSystemCode_Throws()
    {
        // arrange
        var personId = Guid.NewGuid();
        var prevDefId = Guid.NewGuid();
        var nextDefId = Guid.NewGuid();

        var prevDef = new TimesheetCodeDefinition { Id = prevDefId, Code = "Т", RoleCode = RoleCode.TransitionCode };

        var prevEntry = new TimesheetEntry
        {
            Id = Guid.NewGuid(),
            PersonId = personId,
            TimesheetCodeDefinitionId = prevDefId,
            TimesheetCodeDefinition = prevDef,
            From = new DateOnly(2026, 02, 01),
            CreatedAtUtc = new DateTime(2026, 02, 01, 0, 0, 0, DateTimeKind.Utc)
        };

        var command = new TransitionTimesheetStateCommand(
            PersonId: personId,
            AnchorDate: new DateOnly(2026, 02, 10),
            InputDate: new DateOnly(2026, 02, 10),
            NextCode: nextDefId,
            Reference: null,
            Note: null,
            IsCorrection: false,
            Author: "tester",
            NowUtc: new DateTime(2026, 02, 10, 10, 0, 0, DateTimeKind.Utc));

        _entries.Setup(r => r.GetActiveEntryOnDateAsync(personId, command.InputDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(prevEntry);

        _policy.Setup(r => r.GetCodesAsync(includeInactive: true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                prevDef,
                new() { Id = nextDefId, Code = "SYS", RoleCode = RoleCode.SystemCode }
            ]);

        var sut = new TransitionTimesheetStateCommandHandler(_entries.Object, _writer.Object, _policy.Object);

        // act
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.HandleAsync(command));

        // assert
        Assert.Contains("SystemCode", ex.Message, StringComparison.OrdinalIgnoreCase);
        _entries.VerifyAll();
        _policy.VerifyAll();
        _writer.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_WhenNextIsEmergency_TransitionsAtInputDate_WithoutMatrixCheck()
    {
        // arrange
        var personId = Guid.NewGuid();
        var prevDefId = Guid.NewGuid();
        var emergencyId = Guid.NewGuid();

        var prevDef = new TimesheetCodeDefinition { Id = prevDefId, Code = "Т", RoleCode = RoleCode.TransitionCode };

        var prevEntry = new TimesheetEntry
        {
            Id = Guid.NewGuid(),
            PersonId = personId,
            TimesheetCodeDefinitionId = prevDefId,
            TimesheetCodeDefinition = prevDef,
            From = new DateOnly(2026, 02, 01),
            CreatedAtUtc = new DateTime(2026, 02, 01, 0, 0, 0, DateTimeKind.Utc)
        };

        var nowUtc = new DateTime(2026, 02, 10, 10, 0, 0, DateTimeKind.Utc);

        var command = new TransitionTimesheetStateCommand(
            PersonId: personId,
            AnchorDate: new DateOnly(2026, 02, 10),
            InputDate: new DateOnly(2026, 02, 10),
            NextCode: emergencyId,
            Reference: "R",
            Note: "N",
            IsCorrection: false,
            Author: "tester",
            NowUtc: nowUtc);

        _entries.Setup(r => r.GetActiveEntryOnDateAsync(personId, command.InputDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(prevEntry);

        _policy.Setup(r => r.GetCodesAsync(includeInactive: true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                prevDef,
                new() { Id = emergencyId, Code = "100", RoleCode = RoleCode.EmergencyCode }
            ]);

        _writer.Setup(w => w.TransitionAsync(
                personId,
                command.InputDate,
                emergencyId,
                command.Reference,
                command.Note,
                command.Author,
                command.NowUtc,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid());

        var sut = new TransitionTimesheetStateCommandHandler(_entries.Object, _writer.Object, _policy.Object);

        // act
        await sut.HandleAsync(command);

        // assert
        _entries.VerifyAll();
        _policy.VerifyAll();
        _writer.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_TransitionCode_ShiftsByRule_AndWritesAtShiftedDate()
    {
        // arrange
        var personId = Guid.NewGuid();
        var prevDefId = Guid.NewGuid();
        var nextDefId = Guid.NewGuid();

        var prevDef = new TimesheetCodeDefinition { Id = prevDefId, Code = "ВДР", RoleCode = RoleCode.TransitionCode };

        var prevEntry = new TimesheetEntry
        {
            Id = Guid.NewGuid(),
            PersonId = personId,
            TimesheetCodeDefinitionId = prevDefId,
            TimesheetCodeDefinition = prevDef,
            From = new DateOnly(2026, 02, 01),
            CreatedAtUtc = new DateTime(2026, 02, 01, 0, 0, 0, DateTimeKind.Utc)
        };

        var nowUtc = new DateTime(2026, 02, 10, 10, 0, 0, DateTimeKind.Utc);

        var command = new TransitionTimesheetStateCommand(
            PersonId: personId,
            AnchorDate: new DateOnly(2026, 02, 10),
            InputDate: new DateOnly(2026, 02, 10),
            NextCode: nextDefId,
            Reference: "R",
            Note: "N",
            IsCorrection: false,
            Author: "tester",
            NowUtc: nowUtc);

        var nextDef = new TimesheetCodeDefinition { Id = nextDefId, Code = "Т", RoleCode = RoleCode.TransitionCode };

        _entries.Setup(r => r.GetActiveEntryOnDateAsync(personId, command.InputDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(prevEntry);

        _policy.Setup(r => r.GetCodesAsync(includeInactive: true, It.IsAny<CancellationToken>()))
            .ReturnsAsync([prevDef, nextDef]);

        _policy.Setup(r => r.GetAllowedCodesAsync(prevDefId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new() { Id = Guid.NewGuid(), FromCodeId = prevDefId, ToCodeId = nextDefId, StartShiftDays = 1 }
            ]);

        var shifted = command.InputDate.AddDays(1);

        _entries.Setup(r => r.GetNextEntryAfterDateAsync(personId, shifted, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TimesheetEntry?)null);

        _writer.Setup(w => w.TransitionAsync(
                personId,
                shifted,
                nextDefId,
                command.Reference,
                command.Note,
                command.Author,
                command.NowUtc,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid());

        var sut = new TransitionTimesheetStateCommandHandler(_entries.Object, _writer.Object, _policy.Object);

        // act
        await sut.HandleAsync(command);

        // assert
        _entries.VerifyAll();
        _policy.VerifyAll();
        _writer.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_TransitionCode_NotCorrection_WhenNextExists_Throws_AndDoesNotWrite()
    {
        // arrange
        var personId = Guid.NewGuid();
        var prevDefId = Guid.NewGuid();
        var nextDefId = Guid.NewGuid();

        var prevDef = new TimesheetCodeDefinition { Id = prevDefId, Code = "ВДР", RoleCode = RoleCode.TransitionCode };
        var nextDef = new TimesheetCodeDefinition { Id = nextDefId, Code = "Т", RoleCode = RoleCode.TransitionCode };

        var prevEntry = new TimesheetEntry
        {
            Id = Guid.NewGuid(),
            PersonId = personId,
            TimesheetCodeDefinitionId = prevDefId,
            TimesheetCodeDefinition = prevDef,
            From = new DateOnly(2026, 02, 01),
            CreatedAtUtc = new DateTime(2026, 02, 01, 0, 0, 0, DateTimeKind.Utc)
        };

        var command = new TransitionTimesheetStateCommand(
            PersonId: personId,
            AnchorDate: new DateOnly(2026, 02, 10),
            InputDate: new DateOnly(2026, 02, 10),
            NextCode: nextDefId,
            Reference: null,
            Note: null,
            IsCorrection: false,
            Author: "tester",
            NowUtc: new DateTime(2026, 02, 10, 10, 0, 0, DateTimeKind.Utc));

        _entries.Setup(r => r.GetActiveEntryOnDateAsync(personId, command.InputDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(prevEntry);

        _policy.Setup(r => r.GetCodesAsync(includeInactive: true, It.IsAny<CancellationToken>()))
            .ReturnsAsync([prevDef, nextDef]);

        _policy.Setup(r => r.GetAllowedCodesAsync(prevDefId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new() { Id = Guid.NewGuid(), FromCodeId = prevDefId, ToCodeId = nextDefId, StartShiftDays = 0 }
            ]);

        var nextFrom = command.InputDate;

        _entries.Setup(r => r.GetNextEntryAfterDateAsync(personId, nextFrom, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TimesheetEntry { Id = Guid.NewGuid(), PersonId = personId, From = nextFrom });

        var sut = new TransitionTimesheetStateCommandHandler(_entries.Object, _writer.Object, _policy.Object);

        // act
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.HandleAsync(command));

        // assert
        Assert.Contains("вже існує наступна подія", ex.Message, StringComparison.OrdinalIgnoreCase);
        _entries.VerifyAll();
        _policy.VerifyAll();
        _writer.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_TransitionCode_Correction_DoesNotCheckNextExisting_AndWrites()
    {
        // arrange
        var personId = Guid.NewGuid();
        var prevDefId = Guid.NewGuid();
        var nextDefId = Guid.NewGuid();

        var prevDef = new TimesheetCodeDefinition { Id = prevDefId, Code = "ВДР", RoleCode = RoleCode.TransitionCode };
        var nextDef = new TimesheetCodeDefinition { Id = nextDefId, Code = "Т", RoleCode = RoleCode.TransitionCode };

        var prevEntry = new TimesheetEntry
        {
            Id = Guid.NewGuid(),
            PersonId = personId,
            TimesheetCodeDefinitionId = prevDefId,
            TimesheetCodeDefinition = prevDef,
            From = new DateOnly(2026, 02, 01),
            CreatedAtUtc = new DateTime(2026, 02, 01, 0, 0, 0, DateTimeKind.Utc)
        };

        var command = new TransitionTimesheetStateCommand(
            PersonId: personId,
            AnchorDate: new DateOnly(2026, 02, 10),
            InputDate: new DateOnly(2026, 02, 10),
            NextCode: nextDefId,
            Reference: "R",
            Note: "N",
            IsCorrection: true,
            Author: "tester",
            NowUtc: new DateTime(2026, 02, 10, 10, 0, 0, DateTimeKind.Utc));

        _entries.Setup(r => r.GetActiveEntryOnDateAsync(personId, command.InputDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(prevEntry);

        _policy.Setup(r => r.GetCodesAsync(includeInactive: true, It.IsAny<CancellationToken>()))
            .ReturnsAsync([prevDef, nextDef]);

        _policy.Setup(r => r.GetAllowedCodesAsync(prevDefId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new() { Id = Guid.NewGuid(), FromCodeId = prevDefId, ToCodeId = nextDefId, StartShiftDays = 0 }
            ]);

        // IMPORTANT: should NOT call GetNextEntryAfterDateAsync in correction flow.

        _writer.Setup(w => w.TransitionAsync(
                personId,
                command.InputDate,
                nextDefId,
                command.Reference,
                command.Note,
                command.Author,
                command.NowUtc,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid());

        var sut = new TransitionTimesheetStateCommandHandler(_entries.Object, _writer.Object, _policy.Object);

        // act
        await sut.HandleAsync(command);

        // assert
        _entries.VerifyAll();
        _policy.VerifyAll();
        _writer.VerifyAll();
    }
}
