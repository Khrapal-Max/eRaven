//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimeSheetAggregateTests
//-----------------------------------------------------------------------------

using eRaven.Domain.Aggregates;

namespace eRaven.Tests.Domain.Aggregates;

public sealed class TimeSheetAggregateTests
{
    private static readonly DateTime NowUtc = new(2026, 02, 25, 12, 00, 00, DateTimeKind.Utc);

    [Fact]
    public void EnsureInBounds_Throws_WhenBeforeOpenedAt()
    {
        var ts = CreateOpenEpisode(openedAt: new DateOnly(2026, 02, 10));

        var ex = Assert.Throws<InvalidOperationException>(() => ts.EnsureInBounds(new DateOnly(2026, 02, 09)));
        Assert.Contains("before OpenedAt", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void EnsureInBounds_Throws_WhenAfterClosedAt()
    {
        var ts = CreateOpenEpisode(openedAt: new DateOnly(2026, 02, 01));
        ts.ClosedAt = new DateOnly(2026, 02, 10);

        var ex = Assert.Throws<InvalidOperationException>(() => ts.EnsureInBounds(new DateOnly(2026, 02, 11)));
        Assert.Contains("after ClosedAt", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AddTimesheetEntry_SingleEntry_OpenEpisode_LastToIsNull()
    {
        var ts = CreateOpenEpisode(openedAt: new DateOnly(2026, 02, 01));

        var code1 = Guid.NewGuid();
        ts.AddTimesheetEntry(code1, new DateOnly(2026, 02, 05), reference: "Doc1", author: "u", nowUtc: NowUtc);

        var entities = ts.Entries.Where(x => !x.IsDeleted);
        var entity = entities.SingleOrDefault();

        Assert.Single(entities);
        Assert.NotNull(entity);
        Assert.Equal(new DateOnly(2026, 02, 05), entity.From);
        Assert.Null(entity.To);
        Assert.Equal(code1, entity.TimesheetCodeDefinitionId);
        Assert.Equal("Doc1", entity.Reference);
    }

    [Fact]
    public void AddTimesheetEntry_MultipleEntries_SetsToAsNextFrom_AndOrdersByFrom()
    {
        var ts = CreateOpenEpisode(openedAt: new DateOnly(2026, 02, 01));

        var code1 = Guid.NewGuid();
        var code2 = Guid.NewGuid();
        var code3 = Guid.NewGuid();

        // add out of order intentionally
        ts.AddTimesheetEntry(code2, new DateOnly(2026, 02, 10), reference: "B", author: "u", nowUtc: NowUtc);
        ts.AddTimesheetEntry(code1, new DateOnly(2026, 02, 01), reference: "A", author: "u", nowUtc: NowUtc.AddMinutes(-1));
        ts.AddTimesheetEntry(code3, new DateOnly(2026, 02, 20), reference: "C", author: "u", nowUtc: NowUtc.AddMinutes(1));

        var active = ts.Entries.Where(x => !x.IsDeleted).OrderBy(x => x.From).ToList();
        Assert.Equal(3, active.Count);

        Assert.Equal(new DateOnly(2026, 02, 01), active[0].From);
        Assert.Equal(new DateOnly(2026, 02, 10), active[0].To);

        Assert.Equal(new DateOnly(2026, 02, 10), active[1].From);
        Assert.Equal(new DateOnly(2026, 02, 20), active[1].To);

        Assert.Equal(new DateOnly(2026, 02, 20), active[2].From);
        Assert.Null(active[2].To);
    }

    [Fact]
    public void AddTimesheetEntry_DuplicateSameDateSameCode_MergesReferences_AndKeepsSingleAnchor()
    {
        var ts = CreateOpenEpisode(openedAt: new DateOnly(2026, 02, 01));

        var code1 = Guid.NewGuid();
        var d = new DateOnly(2026, 02, 05);

        ts.AddTimesheetEntry(code1, d, reference: "DocA", author: "u", nowUtc: NowUtc);
        ts.AddTimesheetEntry(code1, d, reference: "DocB", author: "u", nowUtc: NowUtc.AddSeconds(1));

        var entities = ts.Entries.Where(x => !x.IsDeleted);
        var entity = entities.SingleOrDefault();

        Assert.Single(entities);
        Assert.NotNull(entity);
        Assert.Equal(d, entity.From);
        Assert.Null(entity.To);

        // MergeReferences adds '.' and joins with spaces.
        Assert.Equal("DocA. DocB.", entity.Reference);
    }

    [Fact]
    public void AddTimesheetEntry_DuplicateSameDateDifferentCode_Throws()
    {
        var ts = CreateOpenEpisode(openedAt: new DateOnly(2026, 02, 01));

        var d = new DateOnly(2026, 02, 05);
        ts.AddTimesheetEntry(Guid.NewGuid(), d, reference: "A", author: "u", nowUtc: NowUtc);

        Assert.Throws<InvalidOperationException>(() =>
            ts.AddTimesheetEntry(Guid.NewGuid(), d, reference: "B", author: "u", nowUtc: NowUtc.AddSeconds(1)));
    }

    [Fact]
    public void GetActiveEntryOnDate_ReturnsEntryWithLatestFromBeforeDate()
    {
        var ts = CreateOpenEpisode(openedAt: new DateOnly(2026, 02, 01));

        var code1 = Guid.NewGuid();
        var code2 = Guid.NewGuid();

        ts.AddTimesheetEntry(code1, new DateOnly(2026, 02, 01), reference: null, author: "u", nowUtc: NowUtc);
        ts.AddTimesheetEntry(code2, new DateOnly(2026, 02, 10), reference: null, author: "u", nowUtc: NowUtc.AddMinutes(1));

        var e = ts.GetActiveEntryOnDate(new DateOnly(2026, 02, 15));
        Assert.NotNull(e);
        Assert.Equal(new DateOnly(2026, 02, 10), e!.From);
        Assert.Equal(code2, e.TimesheetCodeDefinitionId);
    }

    [Fact]
    public void GetNextEntryAfterDate_ReturnsNextAnchor()
    {
        var ts = CreateOpenEpisode(openedAt: new DateOnly(2026, 02, 01));

        var code1 = Guid.NewGuid();
        var code2 = Guid.NewGuid();
        var code3 = Guid.NewGuid();

        ts.AddTimesheetEntry(code1, new DateOnly(2026, 02, 01), reference: null, author: "u", nowUtc: NowUtc);
        ts.AddTimesheetEntry(code2, new DateOnly(2026, 02, 10), reference: null, author: "u", nowUtc: NowUtc.AddMinutes(1));
        ts.AddTimesheetEntry(code3, new DateOnly(2026, 02, 20), reference: null, author: "u", nowUtc: NowUtc.AddMinutes(2));

        var next = ts.GetNextEntryAfterDate(new DateOnly(2026, 02, 10));
        Assert.NotNull(next);
        Assert.Equal(new DateOnly(2026, 02, 20), next!.From);
    }

    [Fact]
    public void SoftDeleteTimesheetEntry_RemovesEntryFromTimeline_AndRenormalizesTo()
    {
        var ts = CreateOpenEpisode(openedAt: new DateOnly(2026, 02, 01));

        var code1 = Guid.NewGuid();
        var code2 = Guid.NewGuid();
        var code3 = Guid.NewGuid();

        ts.AddTimesheetEntry(code1, new DateOnly(2026, 02, 01), reference: null, author: "u", nowUtc: NowUtc);
        ts.AddTimesheetEntry(code2, new DateOnly(2026, 02, 10), reference: null, author: "u", nowUtc: NowUtc.AddMinutes(1));
        ts.AddTimesheetEntry(code3, new DateOnly(2026, 02, 20), reference: null, author: "u", nowUtc: NowUtc.AddMinutes(2));

        var toDelete = ts.Entries.Single(x => !x.IsDeleted && x.From == new DateOnly(2026, 02, 10));
        ts.SoftDeleteTimesheetEntry(toDelete.Id, reason: "fix", author: "u", nowUtc: NowUtc.AddMinutes(3));

        var active = ts.Entries.Where(x => !x.IsDeleted).OrderBy(x => x.From).ToList();
        Assert.Equal(2, active.Count);
        Assert.Equal(new DateOnly(2026, 02, 01), active[0].From);
        Assert.Equal(new DateOnly(2026, 02, 20), active[0].To);
    }

    [Fact]
    public void RemoveTimesheetEntry_RemovesAnchor_AndRenormalizes()
    {
        var ts = CreateOpenEpisode(openedAt: new DateOnly(2026, 02, 01));

        var code1 = Guid.NewGuid();
        var code2 = Guid.NewGuid();

        ts.AddTimesheetEntry(code1, new DateOnly(2026, 02, 01), reference: null, author: "u", nowUtc: NowUtc);
        ts.AddTimesheetEntry(code2, new DateOnly(2026, 02, 10), reference: null, author: "u", nowUtc: NowUtc.AddMinutes(1));

        var e2 = ts.Entries.Single(x => !x.IsDeleted && x.From == new DateOnly(2026, 02, 10));
        ts.RemoveTimesheetEntry(e2.Id);

        var active = ts.Entries.Where(x => !x.IsDeleted).ToList();
        var e1 = Assert.Single(active);
        Assert.Null(e1.To);
    }

    [Fact]
    public void CorrectionTimesheetEntry_ChangesCodeAndDate_AndRenormalizes()
    {
        var ts = CreateOpenEpisode(openedAt: new DateOnly(2026, 02, 01));

        var code1 = Guid.NewGuid();
        var code2 = Guid.NewGuid();
        var code3 = Guid.NewGuid();

        ts.AddTimesheetEntry(code1, new DateOnly(2026, 02, 01), reference: "A", author: "u", nowUtc: NowUtc);
        ts.AddTimesheetEntry(code2, new DateOnly(2026, 02, 10), reference: "B", author: "u", nowUtc: NowUtc.AddMinutes(1));

        var e1 = ts.Entries.Single(x => !x.IsDeleted && x.From == new DateOnly(2026, 02, 01));

        // Move first entry to 2026-02-05 and change code.
        ts.CorrectionTimesheetEntry(
            entyId: e1.Id,
            nextCodeId: code3,
            fromEffectiveAt: new DateOnly(2026, 02, 05),
            reference: "C",
            author: "u",
            nowUtc: NowUtc.AddMinutes(2));

        var active = ts.Entries.Where(x => !x.IsDeleted).OrderBy(x => x.From).ToList();
        Assert.Equal(2, active.Count);

        Assert.Equal(new DateOnly(2026, 02, 05), active[0].From);
        Assert.Equal(code3, active[0].TimesheetCodeDefinitionId);
        Assert.Equal(new DateOnly(2026, 02, 10), active[0].To);
    }

    [Fact]
    public void CloseEpisode_SoftDeletesFutureEntries_ClampsLastToClosedAtPlus1()
    {
        var ts = CreateOpenEpisode(openedAt: new DateOnly(2026, 02, 01));

        var code1 = Guid.NewGuid();
        var code2 = Guid.NewGuid();
        var code3 = Guid.NewGuid();

        ts.AddTimesheetEntry(code1, new DateOnly(2026, 02, 01), reference: null, author: "u", nowUtc: NowUtc);
        ts.AddTimesheetEntry(code2, new DateOnly(2026, 02, 10), reference: null, author: "u", nowUtc: NowUtc.AddMinutes(1));
        ts.AddTimesheetEntry(code3, new DateOnly(2026, 02, 20), reference: null, author: "u", nowUtc: NowUtc.AddMinutes(2));

        ts.CloseEpisode(closedAtInclusive: new DateOnly(2026, 02, 15), reason: "done", author: "u", nowUtc: NowUtc.AddMinutes(3));

        Assert.True(ts.IsClosed);
        Assert.Equal(new DateOnly(2026, 02, 15), ts.ClosedAt);

        // Future entry from 2026-02-20 must be soft-deleted.
        var future = ts.Entries.Single(x => x.From == new DateOnly(2026, 02, 20));
        Assert.True(future.IsDeleted);

        // Active last entry is clamped to ClosedAt+1.
        var active = ts.Entries.Where(x => !x.IsDeleted).OrderBy(x => x.From).ToList();
        Assert.Equal(2, active.Count);
        Assert.Equal(new DateOnly(2026, 02, 10), active[1].From);
        Assert.Equal(new DateOnly(2026, 02, 16), active[1].To);
    }

    [Fact]
    public void CloseEpisode_PreventsFurtherMutations()
    {
        var ts = CreateOpenEpisode(openedAt: new DateOnly(2026, 02, 01));
        ts.AddTimesheetEntry(Guid.NewGuid(), new DateOnly(2026, 02, 01), reference: null, author: "u", nowUtc: NowUtc);

        ts.CloseEpisode(closedAtInclusive: new DateOnly(2026, 02, 01), reason: null, author: "u", nowUtc: NowUtc.AddMinutes(1));

        Assert.Throws<InvalidOperationException>(() =>
            ts.AddTimesheetEntry(Guid.NewGuid(), new DateOnly(2026, 02, 02), reference: null, author: "u", nowUtc: NowUtc.AddMinutes(2)));

        var e = ts.Entries.Single(x => !x.IsDeleted);
        Assert.Throws<InvalidOperationException>(() =>
            ts.CorrectionTimesheetEntry(e.Id, Guid.NewGuid(), new DateOnly(2026, 02, 01), reference: null, author: "u", nowUtc: NowUtc.AddMinutes(3)));
    }

    [Fact]
    public void TouchEntryAudit_UpdatesUpdatedByAndUtc()
    {
        var ts = CreateOpenEpisode(openedAt: new DateOnly(2026, 02, 01));
        ts.AddTimesheetEntry(Guid.NewGuid(), new DateOnly(2026, 02, 01), reference: null, author: "u", nowUtc: NowUtc);

        var e = ts.Entries.Single(x => !x.IsDeleted);
        ts.TouchEntryAudit(e.Id, author: "system", nowUtc: NowUtc.AddHours(1));

        Assert.Equal("system", e.UpdatedBy);
        Assert.Equal(NowUtc.AddHours(1), e.UpdatedAtUtc);
    }

    private static TimeSheetAggregate CreateOpenEpisode(DateOnly openedAt)
        => new()
        {
            Id = Guid.NewGuid(),
            PersonId = Guid.NewGuid(),
            OpenedAt = openedAt,
            ClosedAt = null,
            CreatedBy = "seed",
            CreatedAtUtc = NowUtc
        };
}
