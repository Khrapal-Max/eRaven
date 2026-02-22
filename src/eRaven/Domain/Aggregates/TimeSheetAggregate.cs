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
/// <b>Семантика дат:</b>
/// <list type="bullet">
/// <item><description><see cref="TimesheetEntry.From"/> — inclusive.</description></item>
/// <item><description><see cref="TimesheetEntry.To"/> — <b>EXCLUSIVE</b> (half-open інтервал <c>[From..To)</c>).</description></item>
/// <item><description><c>To = null</c> — інтервал відкритий у майбутнє (до наступної події).</description></item>
/// </list>
/// </para>
///
/// <para>
/// Цей агрегат відповідає тільки за зберігання/корекцію подій в межах епізоду та за
/// структурні інваріанти часової шкали (порядок, межі, узгодженість To).
/// Правила дозволених переходів кодів визначаються зовнішньою політикою.
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

    /// <summary>
    /// Викликає помилку, якщо епізод вже закритий.
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    public void EnsureNotClosed()
    {
        if (IsClosed)
            throw new InvalidOperationException("Timesheet episode is closed.");
    }

    /// <summary>
    /// Викликає помилку, якщо подія раніше дати відкриття епізоду або пізніше дати його закриття.
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    public void EnsureInBounds(DateOnly d)
    {
        if (d < OpenedAt)
            throw new InvalidOperationException($"Date {d} is before OpenedAt {OpenedAt}.");

        if (ClosedAt.HasValue && d > ClosedAt.Value)
            throw new InvalidOperationException($"Date {d} is after ClosedAt {ClosedAt.Value}.");
    }

    //======================================================================
    // CRUD entries (events)
    //======================================================================

    /// <summary>
    /// Додати подію в епізод табеля (anchor-event з <paramref name="effectiveAt"/>).
    /// <para>
    /// Встановлення <see cref="TimesheetEntry.To"/> виконується нормалізацією:
    /// для кожної події <c>To = Next.From</c>, для останньої <c>To = null</c>.
    /// </para>
    /// </summary>
    public void AddTimesheetEntry(
        Guid nextCodeId,
        DateOnly effectiveAt,
        string? reference,
        string author,
        DateTime nowUtc)
    {
        EnsureNotClosed();
        EnsureInBounds(effectiveAt);

        Entries.Add(new TimesheetEntry
        {
            Id = Guid.NewGuid(),
            TimesheetId = Id,
            PersonId = PersonId,
            TimesheetCodeDefinitionId = nextCodeId,
            From = effectiveAt,
            To = null,
            Reference = reference ?? string.Empty,
            Note = null,
            CreatedBy = author,
            CreatedAtUtc = nowUtc
        });

        NormalizeEntries();
    }

    /// <summary>
    /// Видалити подію з епізоду табеля.
    /// <para>
    /// Дозволяємо видалення і в закритому епізоді (як корекцію історичних даних),
    /// але завжди перевіряємо межі епізоду.
    /// </para>
    /// </summary>
    /// <param name="entyId">Ідентифікатор запису.</param>
    /// </param>
    public void RemoveTimesheetEntry(Guid entyId)
    {
        var entry = Entries.FirstOrDefault(x => x.Id == entyId)
            ?? throw new InvalidOperationException($"Entry with Id {entyId} not found.");

        EnsureInBounds(entry.From);

        Entries.Remove(entry);

        NormalizeEntries();
    }

    /// <summary>
    /// Корекція події епізоду табеля.
    /// <para>
    /// Поки забороняємо міняти події у закритих епізодах (правило можна послабити пізніше).
    /// </para>
    /// </summary>
    public void CorrectionTimesheetEntry(
        Guid entyId,
        Guid nextCodeId,
        DateOnly fromEffectiveAt,
        string? reference,
        string author,
        DateTime nowUtc)
    {
        EnsureNotClosed();
        EnsureInBounds(fromEffectiveAt);

        var entry = Entries.FirstOrDefault(x => x.Id == entyId)
            ?? throw new InvalidOperationException($"Entry with Id {entyId} not found.");

        entry.TimesheetCodeDefinitionId = nextCodeId;
        entry.From = fromEffectiveAt;
        entry.Reference = reference ?? string.Empty;
        entry.UpdatedBy = author;
        entry.UpdatedAtUtc = nowUtc;

        NormalizeEntries();
    }

    //======================================================================
    // Timeline normalization (invariants)
    //======================================================================

    /// <summary>
    /// Нормалізує часову шкалу після будь-яких змін:
    /// <list type="bullet">
    /// <item><description>сортує події за <see cref="TimesheetEntry.From"/>;</description></item>
    /// <item><description>забороняє два різні коди в одну й ту саму дату;</description></item>
    /// <item><description>обʼєднує дублікати (одна дата + один код) — склеює <see cref="TimesheetEntry.Reference"/>;</description></item>
    /// <item><description>проставляє <see cref="TimesheetEntry.To"/> як <c>Next.From</c> (last = null);</description></item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// Це доменна логіка: репозиторій не має “підчищати хвости” за агрегатом.
    /// </remarks>
    private void NormalizeEntries()
    {
        if (Entries.Count == 0)
            return;

        var ordered = Entries
            .Where(e => !e.IsDeleted)
            .OrderBy(e => e.From)
            .ThenBy(e => e.CreatedAtUtc)
            .ToList();

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

            Entries.Remove(cur);
        }

        for (var i = 0; i < normalized.Count; i++)
        {
            normalized[i].To = (i < normalized.Count - 1) ? normalized[i + 1].From : null;
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
