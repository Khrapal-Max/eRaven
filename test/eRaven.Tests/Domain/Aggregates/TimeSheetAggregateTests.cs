//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimeSheetAggregateTests
//-----------------------------------------------------------------------------

using eRaven.Domain.Aggregates;
using eRaven.Domain.Entities;

namespace eRaven.Tests.Domain.Aggregates;

/// <summary>
/// Unit tests for <see cref="TimeSheetAggregate"/>.
///
/// <para>
/// These tests validate the "combat task as derived entry" approach:
/// combat tasks are persisted as <see cref="TimesheetEntry"/> with
/// <see cref="TimesheetEntry.SourceKind"/> = <see cref="TimesheetEntrySourceKind.CombatTask"/>
/// and code id = "100" (by convention).
/// </para>
/// </summary>
public sealed class TimeSheetAggregateTests
{
    private static TimeSheetAggregate CreateEpisode(DateOnly openedAt)
        => new()
        {
            Id = Guid.NewGuid(),
            PersonId = Guid.NewGuid(),
            OpenedAt = openedAt,
            ClosedAt = null,
            CreatedBy = "test",
            CreatedAtUtc = new DateTime(2026, 02, 21, 12, 0, 0, DateTimeKind.Utc)
        };

    [Fact]
    public void EnsureNotClosed_WhenClosed_Throws()
    {
        var a = CreateEpisode(new DateOnly(2026, 02, 01));
        a.ClosedAt = new DateOnly(2026, 02, 28);

        var ex = Assert.Throws<InvalidOperationException>(() => a.EnsureNotClosed());
        Assert.Contains("closed", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void EnsureInBounds_BeforeOpenedAt_Throws()
    {
        var a = CreateEpisode(new DateOnly(2026, 02, 10));

        Assert.Throws<InvalidOperationException>(() => a.EnsureInBounds(new DateOnly(2026, 02, 09)));
    }

    [Fact]
    public void EnsureInBounds_AfterClosedAt_Throws()
    {
        var a = CreateEpisode(new DateOnly(2026, 02, 01));
        a.ClosedAt = new DateOnly(2026, 02, 10);

        Assert.Throws<InvalidOperationException>(() => a.EnsureInBounds(new DateOnly(2026, 02, 11)));
    }

    [Fact]
    public void AddTimesheetEntry_SortsAndSetsTo()
    {
        var a = CreateEpisode(new DateOnly(2026, 02, 01));

        var codeA = Guid.NewGuid();
        var codeB = Guid.NewGuid();
        var now = new DateTime(2026, 02, 21, 12, 0, 0, DateTimeKind.Utc);

        // Add out of order.
        a.AddTimesheetEntry(codeA, new DateOnly(2026, 02, 10), "A", "u", now);
        a.AddTimesheetEntry(codeB, new DateOnly(2026, 02, 01), "B", "u", now);

        Assert.Equal(2, a.Entries.Count);

        var e1 = a.Entries.Single(x => x.From == new DateOnly(2026, 02, 01));
        var e2 = a.Entries.Single(x => x.From == new DateOnly(2026, 02, 10));

        Assert.Equal(new DateOnly(2026, 02, 10), e1.To);
        Assert.Null(e2.To);

        Assert.Equal(codeB, e1.TimesheetCodeDefinitionId);
        Assert.Equal(codeA, e2.TimesheetCodeDefinitionId);
    }

    [Fact]
    public void AddTimesheetEntry_ReferenceNull_StoredAsEmptyString()
    {
        var a = CreateEpisode(new DateOnly(2026, 02, 01));
        var code = Guid.NewGuid();

        a.AddTimesheetEntry(code, new DateOnly(2026, 02, 01), null, "u", DateTime.UtcNow);

        var e = a.Entries.Single();
        Assert.NotNull(e.Reference);
        Assert.Equal(string.Empty, e.Reference);
    }

    [Fact]
    public void AddTimesheetEntry_DuplicateSameDateSameCode_MergesReferences_AndKeepsSingleEntry()
    {
        var a = CreateEpisode(new DateOnly(2026, 02, 01));

        var code = Guid.NewGuid();
        var now = new DateTime(2026, 02, 21, 12, 0, 0, DateTimeKind.Utc);

        a.AddTimesheetEntry(code, new DateOnly(2026, 02, 03), "A", "u", now);
        a.AddTimesheetEntry(code, new DateOnly(2026, 02, 03), "B", "u", now);

        Assert.Single(a.Entries);

        var e = a.Entries.Single();
        Assert.Equal(new DateOnly(2026, 02, 03), e.From);
        Assert.Null(e.To);
        Assert.Equal("A. B.", e.Reference);
    }

    [Fact]
    public void AddTimesheetEntry_DuplicateSameDateSameCode_DedupesCommaSeparatedParts()
    {
        var a = CreateEpisode(new DateOnly(2026, 02, 01));

        var code = Guid.NewGuid();
        var now = new DateTime(2026, 02, 21, 12, 0, 0, DateTimeKind.Utc);

        a.AddTimesheetEntry(code, new DateOnly(2026, 02, 03), "A, B", "u", now);
        a.AddTimesheetEntry(code, new DateOnly(2026, 02, 03), "B, C", "u", now);

        var e = a.Entries.Single();
        Assert.Equal("A. B. C.", e.Reference);
    }

    [Fact]
    public void AddTimesheetEntry_DuplicateSameDateDifferentCode_Throws()
    {
        var a = CreateEpisode(new DateOnly(2026, 02, 01));

        var code1 = Guid.NewGuid();
        var code2 = Guid.NewGuid();
        var now = new DateTime(2026, 02, 21, 12, 0, 0, DateTimeKind.Utc);

        a.AddTimesheetEntry(code1, new DateOnly(2026, 02, 03), "A", "u", now);

        Assert.Throws<InvalidOperationException>(() =>
            a.AddTimesheetEntry(code2, new DateOnly(2026, 02, 03), "B", "u", now));
    }

    [Fact]
    public void AddTimesheetEntry_AfterClosedAt_Throws()
    {
        var a = CreateEpisode(new DateOnly(2026, 02, 01));
        a.ClosedAt = new DateOnly(2026, 02, 10);

        Assert.Throws<InvalidOperationException>(() =>
            a.AddTimesheetEntry(Guid.NewGuid(), new DateOnly(2026, 02, 11), "X", "u", DateTime.UtcNow));
    }

    [Fact]
    public void CorrectionTimesheetEntry_UpdatesFields_AndReordersAndFixesTo()
    {
        var a = CreateEpisode(new DateOnly(2026, 02, 01));

        var code1 = Guid.NewGuid();
        var code2 = Guid.NewGuid();
        var code3 = Guid.NewGuid();

        var now = new DateTime(2026, 02, 21, 12, 0, 0, DateTimeKind.Utc);

        a.AddTimesheetEntry(code1, new DateOnly(2026, 02, 10), "A", "u", now);
        a.AddTimesheetEntry(code2, new DateOnly(2026, 02, 20), "B", "u", now);

        var entryId = a.Entries.Single(x => x.From == new DateOnly(2026, 02, 10)).Id;

        var correctionTime = new DateTime(2026, 02, 21, 13, 0, 0, DateTimeKind.Utc);
        a.CorrectionTimesheetEntry(entryId, code3, new DateOnly(2026, 02, 05), "C", "admin", correctionTime);

        var e1 = a.Entries.Single(x => x.From == new DateOnly(2026, 02, 05));
        var e2 = a.Entries.Single(x => x.From == new DateOnly(2026, 02, 20));

        Assert.Equal(code3, e1.TimesheetCodeDefinitionId);
        Assert.Equal("C", e1.Reference);
        Assert.Equal("admin", e1.UpdatedBy);
        Assert.Equal(correctionTime, e1.UpdatedAtUtc);

        Assert.Equal(new DateOnly(2026, 02, 20), e1.To);
        Assert.Null(e2.To);
    }

    [Fact]
    public void CorrectionTimesheetEntry_WhenClosed_Throws()
    {
        var a = CreateEpisode(new DateOnly(2026, 02, 01));

        var code = Guid.NewGuid();
        a.AddTimesheetEntry(code, new DateOnly(2026, 02, 03), "A", "u", DateTime.UtcNow);

        var entryId = a.Entries.Single().Id;
        a.ClosedAt = new DateOnly(2026, 02, 28);

        Assert.Throws<InvalidOperationException>(() =>
            a.CorrectionTimesheetEntry(entryId, Guid.NewGuid(), new DateOnly(2026, 02, 04), "B", "u", DateTime.UtcNow));
    }

    [Fact]
    public void RemoveTimesheetEntry_RecalculatesTo()
    {
        var a = CreateEpisode(new DateOnly(2026, 02, 01));

        var c1 = Guid.NewGuid();
        var c2 = Guid.NewGuid();
        var c3 = Guid.NewGuid();

        var now = new DateTime(2026, 02, 21, 12, 0, 0, DateTimeKind.Utc);

        a.AddTimesheetEntry(c1, new DateOnly(2026, 02, 01), "A", "u", now);
        a.AddTimesheetEntry(c2, new DateOnly(2026, 02, 05), "B", "u", now);
        a.AddTimesheetEntry(c3, new DateOnly(2026, 02, 10), "C", "u", now);

        var middleId = a.Entries.Single(x => x.From == new DateOnly(2026, 02, 05)).Id;
        a.RemoveTimesheetEntry(middleId);

        Assert.Equal(2, a.Entries.Count);

        var e1 = a.Entries.Single(x => x.From == new DateOnly(2026, 02, 01));
        var e3 = a.Entries.Single(x => x.From == new DateOnly(2026, 02, 10));

        Assert.Equal(new DateOnly(2026, 02, 10), e1.To);
        Assert.Null(e3.To);
    }

    [Fact]
    public void RemoveTimesheetEntry_AllowsRemovalWhenClosed()
    {
        var a = CreateEpisode(new DateOnly(2026, 02, 01));

        var c1 = Guid.NewGuid();
        var c2 = Guid.NewGuid();
        var now = new DateTime(2026, 02, 21, 12, 0, 0, DateTimeKind.Utc);

        a.AddTimesheetEntry(c1, new DateOnly(2026, 02, 01), "A", "u", now);
        a.AddTimesheetEntry(c2, new DateOnly(2026, 02, 10), "B", "u", now);

        a.ClosedAt = new DateOnly(2026, 02, 28);

        var id = a.Entries.Single(x => x.From == new DateOnly(2026, 02, 10)).Id;
        a.RemoveTimesheetEntry(id);

        Assert.Single(a.Entries);
        Assert.Null(a.Entries.Single().To);
    }
}
