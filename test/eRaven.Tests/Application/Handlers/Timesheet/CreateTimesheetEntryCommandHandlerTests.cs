//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CreateTimesheetEntryCommandHandlerTests
//-----------------------------------------------------------------------------

using eRaven.Application.Commands.Timesheet;
using eRaven.Application.Handlers.Timesheet;
using eRaven.Domain.Entities;
using eRaven.Domain.Enums;
using eRaven.Infrastructure.Repositories.TimesheetPolicyRepository;
using eRaven.Infrastructure.Repositories.TimesheetRepository;
using Moq;

namespace eRaven.Tests.Application.Handlers.Timesheet;

public sealed class CreateTimesheetEntryCommandHandlerTests
{
    private static readonly DateTime NowUtc = new(2026, 01, 25, 12, 00, 00, DateTimeKind.Utc);

    [Fact]
    public async Task HandleAsync_when_no_history_should_throw_and_not_write()
    {
        // arrange
        var repo = new Mock<ITimesheetEntryRepository>(MockBehavior.Strict);
        var policyRepo = new Mock<ITimesheetPolicyRepository>(MockBehavior.Strict);

        var personId = Guid.NewGuid();
        var from = new DateOnly(2026, 1, 10);

        repo.Setup(x => x.GetActiveEntryOnDateAsync(
                personId,
                TimesheetLane.Main,
                from,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((TimesheetEntry?)null);

        var sut = new CreateTimesheetEntryCommandHandler(repo.Object, policyRepo.Object);

        var cmd = new CreateTimesheetEntryCommand(
            PersonId: personId,
            Lane: TimesheetLane.Main,
            Code: "100",
            From: from,
            To: null,
            Reference: "REF",
            Note: null,
            Author: "tester",
            NowUtc: NowUtc
        );

        // act + assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.HandleAsync(cmd, CancellationToken.None));

        // verify
        repo.Verify(x => x.GetActiveEntryOnDateAsync(personId, TimesheetLane.Main, from, It.IsAny<CancellationToken>()), Times.Once);
        repo.VerifyNoOtherCalls();
        policyRepo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_should_close_prev_and_add_new_open_when_to_is_null_last_day_meaning()
    {
        // arrange
        var repo = new Mock<ITimesheetEntryRepository>(MockBehavior.Strict);
        var policyRepo = new Mock<ITimesheetPolicyRepository>(MockBehavior.Strict);

        var personId = Guid.NewGuid();
        var timelineId = Guid.NewGuid();
        var from = new DateOnly(2026, 1, 10);

        var prev = NewPrev(personId, timelineId, TimesheetLane.Main, "30", from: new DateOnly(2026, 1, 1), to: null);

        repo.Setup(x => x.GetActiveEntryOnDateAsync(personId, TimesheetLane.Main, from, It.IsAny<CancellationToken>()))
            .ReturnsAsync(prev);

        var def = NewCode(TimesheetLane.Main, code: "100", meaning: TimesheetEndDateMeaning.LastDayOfThisCode, nextCodeOnEnd: null);
        policyRepo.Setup(x => x.GetCodesAsync(TimesheetLane.Main, It.IsAny<CancellationToken>()))
            .ReturnsAsync([def]);

        TimesheetEntry? updatedPrev = null;
        repo.Setup(x => x.UpdateAsync(It.IsAny<TimesheetEntry>(), It.IsAny<CancellationToken>()))
            .Callback<TimesheetEntry, CancellationToken>((e, _) => updatedPrev = e)
            .Returns(Task.CompletedTask);

        var added = new List<TimesheetEntry>();
        repo.Setup(x => x.AddAsync(It.IsAny<TimesheetEntry>(), It.IsAny<CancellationToken>()))
            .Callback<TimesheetEntry, CancellationToken>((e, _) => added.Add(e))
            .Returns(Task.CompletedTask);

        var sut = new CreateTimesheetEntryCommandHandler(repo.Object, policyRepo.Object);

        var cmd = new CreateTimesheetEntryCommand(
            PersonId: personId,
            Lane: TimesheetLane.Main,
            Code: "100",
            From: from,
            To: null,
            Reference: "REF-1",
            Note: "NOTE-1",
            Author: "tester",
            NowUtc: NowUtc
        );

        // act
        await sut.HandleAsync(cmd, CancellationToken.None);

        // assert: prev closed at From-1
        Assert.NotNull(updatedPrev);
        Assert.Same(prev, updatedPrev);
        Assert.Equal(from.AddDays(-1), updatedPrev!.To);
        Assert.Equal("tester", updatedPrev.UpdatedBy);
        Assert.Equal(NowUtc, updatedPrev.UpdatedAtUtc);

        // assert: only one add (new entry), open-ended
        Assert.Single(added);

        var e1 = added[0];
        Assert.Equal(timelineId, e1.TimelineId);
        Assert.Equal(personId, e1.PersonId);
        Assert.Equal(TimesheetLane.Main, e1.Lane);
        Assert.Equal("100", e1.Code);
        Assert.Equal(from, e1.From);
        Assert.Null(e1.To);
        Assert.Equal("REF-1", e1.Reference);
        Assert.Equal("NOTE-1", e1.Note);
        Assert.Equal("tester", e1.CreatedBy);
        Assert.Equal(NowUtc, e1.CreatedAtUtc);

        // verify
        repo.Verify(x => x.GetActiveEntryOnDateAsync(personId, TimesheetLane.Main, from, It.IsAny<CancellationToken>()), Times.Once);
        policyRepo.Verify(x => x.GetCodesAsync(TimesheetLane.Main, It.IsAny<CancellationToken>()), Times.Once);
        repo.Verify(x => x.UpdateAsync(It.Is<TimesheetEntry>(e => ReferenceEquals(e, prev)), It.IsAny<CancellationToken>()), Times.Once);
        repo.Verify(x => x.AddAsync(It.IsAny<TimesheetEntry>(), It.IsAny<CancellationToken>()), Times.Once);

        repo.VerifyNoOtherCalls();
        policyRepo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_when_to_provided_last_day_meaning_should_add_return_to_prev_from_next_day()
    {
        // arrange
        var repo = new Mock<ITimesheetEntryRepository>(MockBehavior.Strict);
        var policyRepo = new Mock<ITimesheetPolicyRepository>(MockBehavior.Strict);

        var personId = Guid.NewGuid();
        var timelineId = Guid.NewGuid();

        var from = new DateOnly(2026, 1, 10);
        var to = new DateOnly(2026, 1, 12);

        var prev = NewPrev(personId, timelineId, TimesheetLane.Main, "30", from: new DateOnly(2026, 1, 1), to: null);

        repo.Setup(x => x.GetActiveEntryOnDateAsync(personId, TimesheetLane.Main, from, It.IsAny<CancellationToken>()))
            .ReturnsAsync(prev);

        var def = NewCode(TimesheetLane.Main, code: "100", meaning: TimesheetEndDateMeaning.LastDayOfThisCode);
        policyRepo.Setup(x => x.GetCodesAsync(TimesheetLane.Main, It.IsAny<CancellationToken>()))
            .ReturnsAsync([def]);

        repo.Setup(x => x.UpdateAsync(It.IsAny<TimesheetEntry>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var added = new List<TimesheetEntry>();
        repo.Setup(x => x.AddAsync(It.IsAny<TimesheetEntry>(), It.IsAny<CancellationToken>()))
            .Callback<TimesheetEntry, CancellationToken>((e, _) => added.Add(e))
            .Returns(Task.CompletedTask);

        var sut = new CreateTimesheetEntryCommandHandler(repo.Object, policyRepo.Object);

        var cmd = new CreateTimesheetEntryCommand(
            PersonId: personId,
            Lane: TimesheetLane.Main,
            Code: "100",
            From: from,
            To: to,
            Reference: "REF",
            Note: null,
            Author: "tester",
            NowUtc: NowUtc
        );

        // act
        await sut.HandleAsync(cmd, CancellationToken.None);

        // assert: 2 adds (new entry + return)
        Assert.Equal(2, added.Count);

        var newEntry = added[0];
        Assert.Equal("100", newEntry.Code);
        Assert.Equal(from, newEntry.From);
        Assert.Equal(to, newEntry.To); // unchanged for LastDayOfThisCode

        var ret = added[1];
        Assert.Equal("30", ret.Code);          // return to previous code
        Assert.Equal(to.AddDays(1), ret.From); // next day after last day
        Assert.Null(ret.To);

        // verify
        repo.Verify(x => x.GetActiveEntryOnDateAsync(personId, TimesheetLane.Main, from, It.IsAny<CancellationToken>()), Times.Once);
        policyRepo.Verify(x => x.GetCodesAsync(TimesheetLane.Main, It.IsAny<CancellationToken>()), Times.Once);
        repo.Verify(x => x.UpdateAsync(It.Is<TimesheetEntry>(e => ReferenceEquals(e, prev)), It.IsAny<CancellationToken>()), Times.Once);
        repo.Verify(x => x.AddAsync(It.IsAny<TimesheetEntry>(), It.IsAny<CancellationToken>()), Times.Exactly(2));

        repo.VerifyNoOtherCalls();
        policyRepo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_when_to_provided_first_day_of_next_should_shift_new_to_minus1_and_add_next_code_on_end_from_to()
    {
        // arrange
        var repo = new Mock<ITimesheetEntryRepository>(MockBehavior.Strict);
        var policyRepo = new Mock<ITimesheetPolicyRepository>(MockBehavior.Strict);

        var personId = Guid.NewGuid();
        var timelineId = Guid.NewGuid();

        var from = new DateOnly(2026, 1, 10);
        var to = new DateOnly(2026, 1, 12); // first day of next/base code

        var prev = NewPrev(personId, timelineId, TimesheetLane.Main, "30", from: new DateOnly(2026, 1, 1), to: null);

        repo.Setup(x => x.GetActiveEntryOnDateAsync(personId, TimesheetLane.Main, from, It.IsAny<CancellationToken>()))
            .ReturnsAsync(prev);

        var def = NewCode(
            TimesheetLane.Main,
            code: "ВП",
            meaning: TimesheetEndDateMeaning.FirstDayOfNextCode,
            nextCodeOnEnd: "30");

        policyRepo.Setup(x => x.GetCodesAsync(TimesheetLane.Main, It.IsAny<CancellationToken>()))
            .ReturnsAsync([def]);

        repo.Setup(x => x.UpdateAsync(It.IsAny<TimesheetEntry>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var added = new List<TimesheetEntry>();
        repo.Setup(x => x.AddAsync(It.IsAny<TimesheetEntry>(), It.IsAny<CancellationToken>()))
            .Callback<TimesheetEntry, CancellationToken>((e, _) => added.Add(e))
            .Returns(Task.CompletedTask);

        var sut = new CreateTimesheetEntryCommandHandler(repo.Object, policyRepo.Object);

        var cmd = new CreateTimesheetEntryCommand(
            PersonId: personId,
            Lane: TimesheetLane.Main,
            Code: "ВП",
            From: from,
            To: to,
            Reference: null,
            Note: null,
            Author: "tester",
            NowUtc: NowUtc
        );

        // act
        await sut.HandleAsync(cmd, CancellationToken.None);

        // assert: 2 adds (new entry + next/base code entry on end date)
        Assert.Equal(2, added.Count);

        var newEntry = added[0];
        Assert.Equal("ВП", newEntry.Code);
        Assert.Equal(from, newEntry.From);
        Assert.Equal(to.AddDays(-1), newEntry.To); // shifted -1 day

        var ret = added[1];
        Assert.Equal("30", ret.Code);
        Assert.Equal(to, ret.From); // exactly command.To
        Assert.Null(ret.To);

        // verify
        repo.Verify(x => x.GetActiveEntryOnDateAsync(personId, TimesheetLane.Main, from, It.IsAny<CancellationToken>()), Times.Once);
        policyRepo.Verify(x => x.GetCodesAsync(TimesheetLane.Main, It.IsAny<CancellationToken>()), Times.Once);
        repo.Verify(x => x.UpdateAsync(It.Is<TimesheetEntry>(e => ReferenceEquals(e, prev)), It.IsAny<CancellationToken>()), Times.Once);
        repo.Verify(x => x.AddAsync(It.IsAny<TimesheetEntry>(), It.IsAny<CancellationToken>()), Times.Exactly(2));

        repo.VerifyNoOtherCalls();
        policyRepo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_first_day_of_next_without_next_code_should_not_add_return_entry()
    {
        // arrange
        var repo = new Mock<ITimesheetEntryRepository>(MockBehavior.Strict);
        var policyRepo = new Mock<ITimesheetPolicyRepository>(MockBehavior.Strict);

        var personId = Guid.NewGuid();
        var timelineId = Guid.NewGuid();

        var from = new DateOnly(2026, 1, 10);
        var to = new DateOnly(2026, 1, 12);

        var prev = NewPrev(personId, timelineId, TimesheetLane.Main, "30", from: new DateOnly(2026, 1, 1), to: null);

        repo.Setup(x => x.GetActiveEntryOnDateAsync(personId, TimesheetLane.Main, from, It.IsAny<CancellationToken>()))
            .ReturnsAsync(prev);

        var def = NewCode(
            TimesheetLane.Main,
            code: "ВП",
            meaning: TimesheetEndDateMeaning.FirstDayOfNextCode,
            nextCodeOnEnd: "   "); // whitespace => ignored

        policyRepo.Setup(x => x.GetCodesAsync(TimesheetLane.Main, It.IsAny<CancellationToken>()))
            .ReturnsAsync([def]);

        repo.Setup(x => x.UpdateAsync(It.IsAny<TimesheetEntry>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var added = new List<TimesheetEntry>();
        repo.Setup(x => x.AddAsync(It.IsAny<TimesheetEntry>(), It.IsAny<CancellationToken>()))
            .Callback<TimesheetEntry, CancellationToken>((e, _) => added.Add(e))
            .Returns(Task.CompletedTask);

        var sut = new CreateTimesheetEntryCommandHandler(repo.Object, policyRepo.Object);

        var cmd = new CreateTimesheetEntryCommand(
            PersonId: personId,
            Lane: TimesheetLane.Main,
            Code: "ВП",
            From: from,
            To: to,
            Reference: null,
            Note: null,
            Author: "tester",
            NowUtc: NowUtc
        );

        // act
        await sut.HandleAsync(cmd, CancellationToken.None);

        // assert: only new entry
        Assert.Single(added);
        var newEntry = added[0];
        Assert.Equal("ВП", newEntry.Code);
        Assert.Equal(from, newEntry.From);
        Assert.Equal(to.AddDays(-1), newEntry.To); // shifted -1 day
        Assert.Null(newEntry.Reference);
        Assert.Null(newEntry.Note);

        // verify
        repo.Verify(x => x.GetActiveEntryOnDateAsync(personId, TimesheetLane.Main, from, It.IsAny<CancellationToken>()), Times.Once);
        policyRepo.Verify(x => x.GetCodesAsync(TimesheetLane.Main, It.IsAny<CancellationToken>()), Times.Once);
        repo.Verify(x => x.UpdateAsync(It.Is<TimesheetEntry>(e => ReferenceEquals(e, prev)), It.IsAny<CancellationToken>()), Times.Once);
        repo.Verify(x => x.AddAsync(It.IsAny<TimesheetEntry>(), It.IsAny<CancellationToken>()), Times.Once);

        repo.VerifyNoOtherCalls();
        policyRepo.VerifyNoOtherCalls();
    }

    // -----------------------
    // Helpers
    // -----------------------

    private static TimesheetEntry NewPrev(
        Guid personId,
        Guid timelineId,
        TimesheetLane lane,
        string code,
        DateOnly from,
        DateOnly? to)
        => new()
        {
            Id = Guid.NewGuid(),
            TimelineId = timelineId,
            PersonId = personId,
            Lane = lane,
            Code = code,
            From = from,
            To = to,
            CreatedBy = "seed",
            CreatedAtUtc = NowUtc.AddDays(-1)
        };

    private static TimesheetCodeDefinition NewCode(
        TimesheetLane lane,
        string code,
        TimesheetEndDateMeaning meaning,
        string? nextCodeOnEnd = null)
        => new()
        {
            Id = Guid.NewGuid(),
            Lane = lane,
            Code = code,
            Title = code,
            EndMode = TimesheetEndMode.PeriodOptional,
            EndDateMeaning = meaning,
            NextCodeOnEnd = nextCodeOnEnd,
            SortOrder = 0,
            IsActive = true,
            CreatedBy = "tester",
            CreatedAtUtc = NowUtc
        };
}
