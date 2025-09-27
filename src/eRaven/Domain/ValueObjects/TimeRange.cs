namespace eRaven.Domain.ValueObjects;

/// <summary>
/// Замкнутий зліва, відкритий справа часовий відрізок у UTC.
/// </summary>
public readonly record struct TimeRange
{
    public TimeRange(DateTime startUtc, DateTime endUtc)
    {
        if (startUtc.Kind != DateTimeKind.Utc)
            startUtc = DateTime.SpecifyKind(startUtc, DateTimeKind.Utc);
        if (endUtc.Kind != DateTimeKind.Utc)
            endUtc = DateTime.SpecifyKind(endUtc, DateTimeKind.Utc);

        if (endUtc <= startUtc)
            throw new ArgumentException("Кінець інтервалу має бути пізніше за початок.", nameof(endUtc));

        StartUtc = startUtc;
        EndUtc = endUtc;
    }

    public DateTime StartUtc { get; }

    public DateTime EndUtc { get; }

    public TimeSpan Duration => EndUtc - StartUtc;

    public bool Contains(DateTime momentUtc)
    {
        if (momentUtc.Kind != DateTimeKind.Utc)
            momentUtc = DateTime.SpecifyKind(momentUtc, DateTimeKind.Utc);

        return momentUtc >= StartUtc && momentUtc < EndUtc;
    }

    public bool Intersects(TimeRange other)
        => StartUtc < other.EndUtc && other.StartUtc < EndUtc;

    public TimeRange? Intersect(TimeRange other)
    {
        var start = StartUtc > other.StartUtc ? StartUtc : other.StartUtc;
        var end = EndUtc < other.EndUtc ? EndUtc : other.EndUtc;
        return end > start ? new TimeRange(start, end) : null;
    }

    public TimeRange? Clamp(DateTime minUtc, DateTime maxUtc)
    {
        if (minUtc.Kind != DateTimeKind.Utc)
            minUtc = DateTime.SpecifyKind(minUtc, DateTimeKind.Utc);
        if (maxUtc.Kind != DateTimeKind.Utc)
            maxUtc = DateTime.SpecifyKind(maxUtc, DateTimeKind.Utc);

        var start = StartUtc < minUtc ? minUtc : StartUtc;
        var end = EndUtc > maxUtc ? maxUtc : EndUtc;
        return end > start ? new TimeRange(start, end) : null;
    }

    public IEnumerable<TimeRange> SplitByDay()
    {
        var cursor = StartUtc;
        while (cursor < EndUtc)
        {
            var nextDayStart = new DateTime(cursor.Year, cursor.Month, cursor.Day, 0, 0, 0, DateTimeKind.Utc).AddDays(1);
            var segmentEnd = nextDayStart < EndUtc ? nextDayStart : EndUtc;

            yield return new TimeRange(cursor, segmentEnd);
            cursor = segmentEnd;
        }
    }
}
