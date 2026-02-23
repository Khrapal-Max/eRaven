//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CombatTaskDocumentRepository
//-----------------------------------------------------------------------------

using eRaven.Application.Abstractions.CombatTaskRepository;
using eRaven.Domain.Entities;
using eRaven.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Infrastructure.Repositories.CombatTaskRepository;

/// <summary>
/// EF Core repository for <see cref="CombatTaskDocument"/>.
/// </summary>
public sealed class CombatTaskDocumentRepository(IDbContextFactory<AppDbContext> dbFactory)
    : ICombatTaskDocumentRepository
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory = dbFactory;

    /// <summary>Lower bound for valid year filter.</summary>
    private const int MinYear = 1900;

    /// <summary>Upper bound for valid year filter.</summary>
    private const int MaxYear = 2100;

    /// <inheritdoc />
    public async Task<IReadOnlyList<CombatTaskDocument>> GetDocumentsAsync(
        int? year,
        int? month,
        DocumentStatus? status,
        string? search,
        CancellationToken ct = default)
    {
        if (year.HasValue && (year.Value < MinYear || year.Value > MaxYear))
            throw new ArgumentOutOfRangeException(nameof(year), $"year must be between {MinYear} and {MaxYear}.");

        if (month.HasValue && (month.Value < 1 || month.Value > 12))
            throw new ArgumentOutOfRangeException(nameof(month), "month must be between 1 and 12.");

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var q = db.CombatTaskDocuments
            .AsNoTracking();

        if (year.HasValue)
            q = q.Where(x => x.RecordedAt.Year == year.Value);

        if (month.HasValue)
            q = q.Where(x => x.RecordedAt.Month == month.Value);

        if (status.HasValue)
            q = q.Where(x => x.Status == status.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();

            // NOTE:
            // - PostgreSQL: use ILIKE (case-insensitive).
            // - Others (e.g., SQLite for tests): fallback to LIKE.
            var pattern = $"%{s}%";

            q = db.Database.IsNpgsql()
                ? q.Where(x =>
                    EF.Functions.ILike(x.OrderTitle, pattern)
                    || (x.Description != null && EF.Functions.ILike(x.Description, pattern)))
                : q.Where(x =>
                    EF.Functions.Like(x.OrderTitle, pattern)
                    || (x.Description != null && EF.Functions.Like(x.Description, pattern)));
        }

        return await q
            .OrderByDescending(x => x.RecordedAt)
            .ThenByDescending(x => x.CreatedAtUtc)
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task<Guid> CreateAsync(
        string orderTitle,
        DateOnly recordedAt,
        string? description,
        string author,
        DateTime nowUtc,
        CancellationToken ct = default)
    {
        var by = EnsureAuthor(author);

        if (string.IsNullOrWhiteSpace(orderTitle))
            throw new ArgumentException("orderTitle is required.", nameof(orderTitle));

        if (recordedAt == default)
            throw new ArgumentException("recordedAt is required.", nameof(recordedAt));

        if (nowUtc == default)
            throw new ArgumentException("nowUtc is required.", nameof(nowUtc));

        EnsureUtc(nowUtc, nameof(nowUtc));

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var doc = new CombatTaskDocument
        {
            Id = Guid.NewGuid(),
            Status = DocumentStatus.Active,
            OrderTitle = orderTitle.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            RecordedAt = recordedAt,
            CreatedBy = by,
            CreatedAtUtc = nowUtc,
            UpdatedBy = by,
            UpdatedAtUtc = nowUtc
        };

        db.CombatTaskDocuments.Add(doc);
        await db.SaveChangesAsync(ct);
        return doc.Id;
    }

    /// <inheritdoc />
    public async Task CancelAsync(
        Guid documentId,
        string? reason,
        string author,
        DateTime nowUtc,
        CancellationToken ct = default)
    {
        if (documentId == default)
            throw new ArgumentException("documentId must be set.", nameof(documentId));

        var by = EnsureAuthor(author);

        if (nowUtc == default)
            throw new ArgumentException("nowUtc is required.", nameof(nowUtc));

        EnsureUtc(nowUtc, nameof(nowUtc));

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var doc = await db.CombatTaskDocuments
            .SingleOrDefaultAsync(x => x.Id == documentId, ct)
            ?? throw new InvalidOperationException("CombatTaskDocument not found.");

        if (doc.Status == DocumentStatus.Canceled)
            return; // idempotent

        doc.Status = DocumentStatus.Canceled;
        doc.CanceledBy = by;
        doc.CanceledAtUtc = nowUtc;
        doc.CanceledReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();

        doc.UpdatedBy = by;
        doc.UpdatedAtUtc = nowUtc;

        await db.SaveChangesAsync(ct);
    }

    private static string EnsureAuthor(string author)
    {
        if (string.IsNullOrWhiteSpace(author))
            throw new ArgumentException("author is required.", nameof(author));

        return author.Trim();
    }

    private static void EnsureUtc(DateTime value, string paramName)
    {
        if (value.Kind != DateTimeKind.Utc)
            throw new ArgumentException($"{paramName} must be UTC (DateTimeKind.Utc).", paramName);
    }
}
