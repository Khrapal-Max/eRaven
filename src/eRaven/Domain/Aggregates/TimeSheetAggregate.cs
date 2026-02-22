//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimeSheetAggregate
//-----------------------------------------------------------------------------

using eRaven.Domain.Entities;

namespace eRaven.Domain.Aggregates;

/// <summary>
/// Епізод табеля (episode) з подіями (<see cref="TimesheetEntry"/>).
///
/// <para>
/// <b>Семантика дат (half-open):</b>
/// <list type="bullet">
/// <item><description><see cref="TimesheetEntry.From"/> — inclusive.</description></item>
/// <item><description><see cref="TimesheetEntry.To"/> — <b>exclusive</b> (інтервал <c>[From..To)</c>).</description></item>
/// <item><description><c>To = null</c> — інтервал відкритий у майбутнє (до наступної події або до закриття епізоду).</description></item>
/// </list>
/// </para>
///
/// <para>
/// Агрегат відповідає за:
/// <list type="bullet">
/// <item><description>додавання/корекцію/видалення подій в межах епізоду;</description></item>
/// <item><description>soft-delete подій (audit) без фізичного видалення;</description></item>
/// <item><description>структурні інваріанти часової шкали: порядок, межі, узгодженість <see cref="TimesheetEntry.To"/>.</description></item>
/// </list>
/// </para>
///
/// <para>
/// ВАЖЛИВО: керування потоком подій (які події створювати) — зовнішня відповідальність.
/// Агрегат лише гарантує інваріанти та коректно модифікує власні події.
/// </para>
/// </summary>
public sealed class TimeSheetAggregate
{
    public Guid Id { get; set; }
    public Guid PersonId { get; set; }

    public DateOnly OpenedAt { get; set; }
    public DateOnly? ClosedAt { get; set; }

    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }

    public string? ClosedBy { get; set; }
    public DateTime? ClosedAtUtc { get; set; }

    /// <summary>
    /// Події епізоду у вигляді записів часової шкали.
    /// <para>Після будь-яких змін агрегат нормалізує список (див. <see cref="NormalizeEntries"/>).</para>
    /// </summary>
    public ICollection<TimesheetEntry> Entries { get; set; } = [];

    public bool IsClosed => ClosedAt.HasValue;

    //======================================================================
    // Guards
    //======================================================================

    /// <summary>
    /// Кидає виняток, якщо епізод вже закритий.
    /// </summary>
    public void EnsureNotClosed()
    {
        if (IsClosed)
            throw new InvalidOperationException("Timesheet episode is closed.");
    }

    /// <summary>
    /// Кидає виняток, якщо дата виходить за межі епізоду:
    /// <list type="bullet">
    /// <item><description><c>d &lt; OpenedAt</c></description></item>
    /// <item><description><c>ClosedAt != null</c> та <c>d &gt; ClosedAt</c></description></item>
    /// </list>
    /// </summary>
    public void EnsureInBounds(DateOnly d)
    {
        if (d < OpenedAt)
            throw new InvalidOperationException($"Date {d} is before OpenedAt {OpenedAt}.");

        if (ClosedAt.HasValue && d > ClosedAt.Value)
            throw new InvalidOperationException($"Date {d} is after ClosedAt {ClosedAt.Value}.");
    }

    //======================================================================
    // Read helpers (over own entries)
    //======================================================================

    /// <summary>
    /// Повертає активну подію на дату <paramref name="d"/>.
    /// <para>Half-open: <c>From &lt;= d</c> та <c>(To == null || d &lt; To)</c>.</para>
    /// </summary>
    public TimesheetEntry? GetActiveEntryOnDate(DateOnly d)
    {
        EnsureInBounds(d);

        TimesheetEntry? best = null;

        foreach (var e in Entries)
        {
            if (e.IsDeleted) continue;
            if (e.From > d) continue;
            if (e.To.HasValue && d >= e.To.Value) continue;

            if (best is null || e.From > best.From)
                best = e;
        }

        return best;
    }

    /// <summary>
    /// Повертає наступну подію після дати <paramref name="d"/> (за <see cref="TimesheetEntry.From"/>).
    /// </summary>
    public TimesheetEntry? GetNextEntryAfterDate(DateOnly d)
    {
        EnsureInBounds(d);

        TimesheetEntry? best = null;

        foreach (var e in Entries)
        {
            if (e.IsDeleted) continue;
            if (e.From <= d) continue;

            if (best is null || e.From < best.From)
                best = e;
        }

        return best;
    }

    /// <summary>
    /// Повертає подію, яка починається у точці <paramref name="from"/> (change-point), або <c>null</c>.
    /// </summary>
    public TimesheetEntry? GetEntryByFrom(DateOnly from)
        => Entries.FirstOrDefault(e => !e.IsDeleted && e.From == from);

    //======================================================================
    // CRUD entries (events)
    //======================================================================

    /// <summary>
    /// Додає подію (anchor-event) у точці <paramref name="effectiveAt"/>.
    /// <para>
    /// <see cref="TimesheetEntry.To"/> агрегат встановлює в <see cref="NormalizeEntries"/>:
    /// <c>To = Next.From</c>, а для останньої події — <c>null</c> або <c>ClosedAt+1</c> (для закритого епізоду).
    /// </para>
    /// </summary>
    public void AddTimesheetEntry(
        Guid nextCodeId,
        DateOnly effectiveAt,
        string? reference,
        string author,
        DateTime nowUtc,
        string? note = null)
        => AddTimesheetEntry(Guid.NewGuid(), nextCodeId, effectiveAt, reference, note, author, nowUtc);

    /// <summary>
    /// Додає подію з фіксованим <paramref name="entryId"/> (legacy-сценарій transition).
    /// </summary>
    public void AddTimesheetEntry(
        Guid entryId,
        Guid nextCodeId,
        DateOnly effectiveAt,
        string? reference,
        string? note,
        string author,
        DateTime nowUtc)
    {
        EnsureNotClosed();
        EnsureInBounds(effectiveAt);

        if (entryId == Guid.Empty)
            entryId = Guid.NewGuid();

        Entries.Add(new TimesheetEntry
        {
            Id = entryId,
            TimesheetId = Id,
            PersonId = PersonId,
            TimesheetCodeDefinitionId = nextCodeId,
            From = effectiveAt,
            To = null,
            Reference = reference ?? string.Empty,
            Note = note,
            CreatedBy = author,
            CreatedAtUtc = nowUtc
        });

        NormalizeEntries();
    }

    /// <summary>
    /// Soft-delete події (з audit).
    /// <para>
    /// Видалена подія не бере участі в нормалізації та не повертається read-методами.
    /// </para>
    /// </summary>
    public void SoftDeleteTimesheetEntry(Guid entryId, string reason, string author, DateTime nowUtc)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Delete reason is required.", nameof(reason));

        var entry = Entries.FirstOrDefault(x => x.Id == entryId);
        if (entry is null)
            return;

        EnsureInBounds(entry.From);

        if (entry.IsDeleted)
            return;

        entry.IsDeleted = true;
        entry.DeletedBy = author;
        entry.DeletedAtUtc = nowUtc;
        entry.DeleteReason = reason.Trim();

        NormalizeEntries();
    }

    /// <summary>
    /// Фізично видаляє подію з епізоду.
    /// <para>
    /// Дозволяємо видалення і в закритому епізоді (як корекцію історичних даних),
    /// але завжди перевіряємо межі епізоду.
    /// </para>
    /// </summary>
    public void RemoveTimesheetEntry(Guid entryId)
    {
        var entry = Entries.FirstOrDefault(x => x.Id == entryId)
            ?? throw new InvalidOperationException($"Entry with Id {entryId} not found.");

        EnsureInBounds(entry.From);

        Entries.Remove(entry);
        NormalizeEntries();
    }

    /// <summary>
    /// Корекція події епізоду.
    /// <para>
    /// За замовчуванням забороняємо корекцію у закритих епізодах.
    /// </para>
    /// </summary>
    public void CorrectionTimesheetEntry(
        Guid entyId,
        Guid nextCodeId,
        DateOnly fromEffectiveAt,
        string? reference,
        string author,
        DateTime nowUtc,
        string? note = null)
    {
        EnsureNotClosed();
        EnsureInBounds(fromEffectiveAt);

        var entry = Entries.FirstOrDefault(x => x.Id == entyId)
            ?? throw new InvalidOperationException($"Entry with Id {entyId} not found.");

        entry.TimesheetCodeDefinitionId = nextCodeId;
        entry.From = fromEffectiveAt;
        entry.Reference = reference ?? string.Empty;
        entry.Note = note;
        entry.UpdatedBy = author;
        entry.UpdatedAtUtc = nowUtc;

        NormalizeEntries();
    }

    /// <summary>
    /// Оновлює audit-поля події (без зміни коду/дат).
    /// <para>
    /// Потрібно для legacy-переходів, де prevUpdated передається як "вже оновлений".
    /// </para>
    /// </summary>
    public void TouchEntryAudit(Guid entryId, string author, DateTime nowUtc)
    {
        var entry = Entries.FirstOrDefault(x => x.Id == entryId)
            ?? throw new InvalidOperationException($"Entry with Id {entryId} not found.");

        entry.UpdatedBy = author;
        entry.UpdatedAtUtc = nowUtc;
    }

    //======================================================================
    // Episode lifecycle
    //======================================================================

    /// <summary>
    /// Закриває епізод на дату <paramref name="closedAtInclusive"/> (inclusive).
    /// <para>
    /// Також soft-delete всі майбутні події (ті, що починаються з <c>ClosedAt+1</c>).
    /// </para>
    /// </summary>
    public void CloseEpisode(DateOnly closedAtInclusive, string? reason, string author, DateTime nowUtc)
    {
        EnsureNotClosed();
        EnsureInBounds(closedAtInclusive);

        var closeExclusive = closedAtInclusive.AddDays(1);

        // 1) Soft-delete future entries (From >= closeExclusive)
        var deleteReason = string.IsNullOrWhiteSpace(reason) ? "Auto: episode closed" : reason.Trim();

        foreach (var e in Entries.Where(e => !e.IsDeleted && e.From >= closeExclusive).ToList())
        {
            e.IsDeleted = true;
            e.DeletedBy = author;
            e.DeletedAtUtc = nowUtc;
            e.DeleteReason = deleteReason;
        }

        // 2) Close episode
        ClosedAt = closedAtInclusive;
        ClosedBy = author;
        ClosedAtUtc = nowUtc;

        NormalizeEntries();
    }

    //======================================================================
    // Timeline normalization (invariants)
    //======================================================================

    /// <summary>
    /// Нормалізує часову шкалу після будь-яких змін:
    /// <list type="bullet">
    /// <item><description>сортує події за <see cref="TimesheetEntry.From"/>;</description></item>
    /// <item><description>забороняє два різні коди на одну й ту саму дату;</description></item>
    /// <item><description>обʼєднує дублікати (одна дата + один код) — склеює <see cref="TimesheetEntry.Reference"/>;</description></item>
    /// <item><description>проставляє <see cref="TimesheetEntry.To"/> як <c>Next.From</c>, а останній — <c>null</c> або <c>ClosedAt+1</c>.</description></item>
    /// </list>
    /// </summary>
    private void NormalizeEntries()
    {
        var ordered = Entries
            .Where(e => !e.IsDeleted)
            .OrderBy(e => e.From)
            .ThenBy(e => e.CreatedAtUtc)
            .ToList();

        if (ordered.Count == 0)
            return;

        var normalized = new List<TimesheetEntry>(ordered.Count);

        foreach (var cur in ordered)
        {
            EnsureInBounds(cur.From);

            if (normalized.Count == 0)
            {
                normalized.Add(cur);
                continue;
            }

            var last = normalized[^1];

            if (cur.From != last.From)
            {
                normalized.Add(cur);
                continue;
            }

            if (cur.TimesheetCodeDefinitionId != last.TimesheetCodeDefinitionId)
                throw new InvalidOperationException($"Two different codes are not allowed at the same date {cur.From}.");

            last.Reference = MergeReferences(last.Reference, cur.Reference);

            // remove duplicate anchor
            Entries.Remove(cur);
        }

        for (var i = 0; i < normalized.Count; i++)
        {
            if (i < normalized.Count - 1)
            {
                normalized[i].To = normalized[i + 1].From;
                continue;
            }

            // last entry: open-ended, or clamped to ClosedAt+1 for closed episode
            normalized[i].To = ClosedAt.HasValue ? ClosedAt.Value.AddDays(1) : null;
        }
    }

    private static string MergeReferences(string? a, string? b)
    {
        var left = (a ?? string.Empty).Trim();
        var right = (b ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(left))
            return right;

        if (string.IsNullOrWhiteSpace(right))
            return left;

        var parts = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        void addParts(string s)
        {
            foreach (var p in s.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (seen.Add(p))
                    parts.Add(p + ".");
            }
        }

        addParts(left);
        addParts(right);

        return string.Join(" ", parts);
    }
}
