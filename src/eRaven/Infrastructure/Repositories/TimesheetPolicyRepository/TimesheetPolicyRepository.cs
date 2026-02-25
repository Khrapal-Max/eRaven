//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetPolicyRepository
//-----------------------------------------------------------------------------
//
// Політика табеля (Feb 2026):
// - "НБ" (TimesheetDerivedCodes.NotInTimesheet) — derived gap, НЕ є реальним кодом у довіднику.
// - SystemCode (наприклад "100") належить системі/документам: не створюється і не редагується вручну.
// //
// NOTE: Репозиторій живе в Infrastructure (EF Core) і є "persistence boundary" для налаштувань політики.
//-----------------------------------------------------------------------------

using eRaven.Application.Abstractions.TimesheetPolicyRepository;
using eRaven.Domain.Consts;
using eRaven.Domain.Entities;
using eRaven.Domain.Enums;
using eRaven.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Infrastructure.Repositories.TimesheetPolicyRepository;

/// <summary>
/// Репозиторій керування довідником табельних кодів та матрицею переходів між ними.
/// </summary>
public sealed class TimesheetPolicyRepository(IDbContextFactory<AppDbContext> dbFactory)
    : ITimesheetPolicyRepository
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory = dbFactory;

    //======================================================================
    // Reads
    //======================================================================

    /// <inheritdoc />
    public async Task<IReadOnlyList<TimesheetCodeDefinition>> GetCodesAsync(
        bool includeInactive,
        CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var q = db.TimesheetCodes.AsNoTracking();

        // Hide derived NB always (even if legacy row exists in DB).
        q = q.Where(x => x.Code != TimesheetDerivedCodes.NotInTimesheet);

        if (!includeInactive)
            q = q.Where(x => x.IsActive);

        return await q
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Priority)
            .ThenBy(x => x.Code)
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    /// <inheritdoc />
    public async Task<IReadOnlyList<TimesheetCodeTransition>> GetAllowedCodesAsync(
        Guid fromCodeId,
        CancellationToken ct = default)
    {
        if (fromCodeId == Guid.Empty)
            throw new ArgumentException("ИД коду обов'язковий.", nameof(fromCodeId));

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        // 1) Transition targets (strict matrix) for current code: active only.
        var transitions = await db.TimesheetCodeTransitions
            .AsNoTracking()
            .Where(x => x.FromCodeId == fromCodeId)
            .Include(x => x.ToCode)
                .Where(x => x.ToCode.Code != TimesheetDerivedCodes.NotInTimesheet)
                .Where(x => x.ToCode.IsActive)
                .Where(x => x.ToCode.RoleCode == RoleCode.TransitionCode)
            .ToListAsync(ct);

        // 2) Emergency codes are global options (active only) and should be visible as "allowed",
        // even if they are not present in the strict matrix table.
        var emergency = await db.TimesheetCodes
            .AsNoTracking()
            .Where(x => x.IsActive)
            .Where(x => x.Code != TimesheetDerivedCodes.NotInTimesheet)
            .Where(x => x.RoleCode == RoleCode.EmergencyCode)
            .ToListAsync(ct);

        // Merge into a single list of "allowed codes".
        // Emergency codes are returned as synthetic transitions with StartShiftDays=0.
        var existingTo = transitions.Select(x => x.ToCodeId).ToHashSet();
        foreach (var e in emergency)
        {
            if (existingTo.Contains(e.Id))
                continue;

            transitions.Add(new TimesheetCodeTransition
            {
                Id = Guid.Empty,
                FromCodeId = fromCodeId,
                ToCodeId = e.Id,
                ToCode = e,
                StartShiftDays = 0,
                CreatedBy = string.Empty,
                CreatedAtUtc = default
            });
        }

        return [.. transitions
            .OrderBy(x => x.ToCode.SortOrder)
            .ThenBy(x => x.ToCode.Priority)
            .ThenBy(x => x.ToCode.Code)];
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TimesheetCodeTransition>> GetTransitionCodesAsync(Guid fromCodeId, CancellationToken ct = default)
    {
        if (fromCodeId == Guid.Empty)
            throw new ArgumentException("fromCodeId is required.", nameof(fromCodeId));

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var from = await db.TimesheetCodes
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == fromCodeId, ct);

        if (from is null)
            return [];

        // Strict policy is supported only for TransitionCode (not System/Emergency/derived).
        if (from.Code == TimesheetDerivedCodes.NotInTimesheet || from.RoleCode != RoleCode.TransitionCode)
            return [];

        // For policy UI we return matrix rows even if ToCode is inactive (so it can be cleaned),
        // but still only to TransitionCode targets (strictness).
        return await db.TimesheetCodeTransitions
            .AsNoTracking()
            .Where(x => x.FromCodeId == fromCodeId)
            .Include(x => x.ToCode)
                .Where(x => x.ToCode.Code != TimesheetDerivedCodes.NotInTimesheet)
                .Where(x => x.ToCode.RoleCode == RoleCode.TransitionCode)
                .OrderBy(x => x.ToCode.SortOrder)
                .ThenBy(x => x.ToCode.Priority)
                .ThenBy(x => x.ToCode.Code)
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TimesheetCodeDefinition>> GetEmergencyCodesAsync(CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        return await db.TimesheetCodes
            .AsNoTracking()
            .Where(x => x.IsActive)
            .Where(x => x.Code != TimesheetDerivedCodes.NotInTimesheet)
            .Where(x => x.RoleCode == RoleCode.EmergencyCode)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Priority)
            .ThenBy(x => x.Code)
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task<TimesheetCodeDefinition?> GetCodeByIdAsync(Guid codeId, CancellationToken ct = default)
    {
        if (codeId == Guid.Empty)
            throw new ArgumentException("ИД коду обов'язковий.", nameof(codeId));

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        return await db.TimesheetCodes
            .AsNoTracking()
            .Where(x => x.IsActive)
            .Where(x => x.Code != TimesheetDerivedCodes.NotInTimesheet)
            .FirstOrDefaultAsync(x => x.Id == codeId, ct);
    }

    //====================================
    // Writes
    //====================================

    /// <inheritdoc />
    public async Task<Guid> AddCodeAsync(
        string code,
        string title,
        string? description,
        int sortOrder,
        int priority,
        bool isTerminal,
        RoleCode roleCode,
        TimesheetUiStyle uiStyle,
        string author,
        DateTime nowUtc,
        CancellationToken ct = default)
    {
        code = NormalizeCode(code);
        title = NormalizeRequired(title, "Назва коду не може бути порожньою.");
        description = NormalizeOptional(description);
        author = NormalizeRequired(author, "Автор обов'язковий.");

        if (string.IsNullOrWhiteSpace(code))
            throw new InvalidOperationException("Код не може бути порожнім.");

        if (string.Equals(code, TimesheetDerivedCodes.NotInTimesheet, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Код “НБ” є derived станом і не може бути створений як подія.");

        if (roleCode == RoleCode.SystemCode)
            throw new InvalidOperationException("Системні коди не можна створювати вручну.");

        if (uiStyle == TimesheetUiStyle.NotInTimesheet)
            throw new InvalidOperationException("UiStyle.NotInTimesheet зарезервований для derived gap (“НБ”).");

        if (sortOrder < 0)
            throw new InvalidOperationException("Порядковий номер не може бути < 0.");

        if (priority < 0)
            throw new InvalidOperationException("Пріоритет не може бути < 0.");

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var exists = await db.TimesheetCodes
            .AsNoTracking()
            .AnyAsync(x => x.Code == code, ct);

        if (exists)
            throw new InvalidOperationException($"Код '{code}' вже існує.");

        var e = new TimesheetCodeDefinition
        {
            Id = Guid.NewGuid(),
            Code = code,
            Title = title,
            Description = description,
            SortOrder = sortOrder,
            Priority = priority,
            IsTerminal = isTerminal,
            IsActive = true,
            RoleCode = roleCode,
            UiStyle = uiStyle,
            CreatedBy = author,
            CreatedAtUtc = nowUtc
        };

        db.TimesheetCodes.Add(e);
        await db.SaveChangesAsync(ct);
        return e.Id;
    }

    /// <inheritdoc />
    public async Task CloseCodeAsync(
        Guid codeId,
        string author,
        DateTime nowUtc,
        CancellationToken ct = default)
    {
        if (codeId == Guid.Empty)
            throw new ArgumentException("ИД коду обов'язковий.", nameof(codeId));

        author = NormalizeRequired(author, "Автор обоав'язковий.");

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var code = await db.TimesheetCodes
            .FirstOrDefaultAsync(x => x.Id == codeId, ct)
            ?? throw new InvalidOperationException("Код не знайдено.");

        if (code.Code == TimesheetDerivedCodes.NotInTimesheet)
            throw new InvalidOperationException("Код “НБ” є derived станом і не може бути закритий (він не має існувати в БД).");

        if (code.RoleCode == RoleCode.SystemCode)
            throw new InvalidOperationException("Системні коди не можна закривати вручну.");

        if (!code.IsActive)
            return;

        code.IsActive = false;
        code.UpdatedBy = author;
        code.UpdatedAtUtc = nowUtc;

        await db.SaveChangesAsync(ct);
    }

    /// <inheritdoc />
    public async Task SavePolicyAsync(
        Guid codeId,
        string title,
        string? description,
        int sortOrder,
        int priority,
        bool isTerminal,
        IReadOnlyCollection<TimesheetTransitionSpec> allowedTransitions,
        string author,
        DateTime nowUtc,
        CancellationToken ct = default)
    {
        if (codeId == Guid.Empty)
            throw new ArgumentException("codeId is required.", nameof(codeId));

        title = NormalizeRequired(title, "Назва коду не може бути порожньою.");
        description = NormalizeOptional(description);
        author = NormalizeRequired(author, "Author is required.");

        if (sortOrder < 0)
            throw new InvalidOperationException("SortOrder не може бути < 0.");
        if (priority < 0)
            throw new InvalidOperationException("Priority не може бути < 0.");

        allowedTransitions ??= [];

        // Normalize: remove empty/self + dedupe by ToCodeId.
        var normalized = allowedTransitions
            .Where(x => x.ToCodeId != Guid.Empty)
            .Where(x => x.ToCodeId != codeId)
            .GroupBy(x => x.ToCodeId)
            .Select(g => g.First())
            .ToList();

        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var code = await db.TimesheetCodes
            .FirstOrDefaultAsync(x => x.Id == codeId, ct)
            ?? throw new InvalidOperationException("Код не знайдено.");

        if (code.Code == TimesheetDerivedCodes.NotInTimesheet)
            throw new InvalidOperationException("Неможливо зберегти політику для derived стану “НБ”.");

        // Update code fields
        code.Title = title;
        code.Description = description;
        code.SortOrder = sortOrder;
        code.Priority = priority;
        code.IsTerminal = isTerminal;
        code.UpdatedBy = author;
        code.UpdatedAtUtc = nowUtc;

        // Verify ToCode exists + ACTIVE (policy cannot reference inactive targets).
        var toIds = normalized.Select(x => x.ToCodeId).ToList();
        if (toIds.Count > 0)
        {
            var activeTo = await db.TimesheetCodes
                .AsNoTracking()
                .Where(x => toIds.Contains(x.Id))
                .Where(x => x.IsActive)
                .Where(x => x.Code != TimesheetDerivedCodes.NotInTimesheet)
                .Select(x => x.Id)
                .ToListAsync(ct);

            var missing = toIds.Except(activeTo).ToList();
            if (missing.Count > 0)
                throw new InvalidOperationException("Серед переходів є коди, які не існують, вже закриті або не є TransitionCode.");
        }

        // Diff-update transitions (preserve Created* for existing rows).
        var existing = await db.TimesheetCodeTransitions
            .Where(x => x.FromCodeId == codeId)
            .ToListAsync(ct);

        var existingByTo = existing.ToDictionary(x => x.ToCodeId, x => x);

        foreach (var desired in normalized)
        {
            if (existingByTo.TryGetValue(desired.ToCodeId, out var tr))
            {
                if (tr.StartShiftDays != desired.StartShiftDays)
                    tr.StartShiftDays = desired.StartShiftDays;
            }
            else
            {
                db.TimesheetCodeTransitions.Add(new TimesheetCodeTransition
                {
                    Id = Guid.NewGuid(),
                    FromCodeId = codeId,
                    ToCodeId = desired.ToCodeId,
                    StartShiftDays = desired.StartShiftDays,
                    CreatedBy = author,
                    CreatedAtUtc = nowUtc
                });
            }
        }

        var desiredSet = normalized.Select(x => x.ToCodeId).ToHashSet();
        foreach (var old in existing)
            if (!desiredSet.Contains(old.ToCodeId))
                db.TimesheetCodeTransitions.Remove(old);

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    //======================================================================
    // Helpers
    //======================================================================

    private static string NormalizeCode(string? code)
        => (code ?? string.Empty).Trim().ToUpperInvariant();

    private static string NormalizeRequired(string? value, string messageIfEmpty)
    {
        var s = (value ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(s))
            throw new InvalidOperationException(messageIfEmpty);
        return s;
    }

    private static string? NormalizeOptional(string? value)
    {
        var s = (value ?? string.Empty).Trim();
        return string.IsNullOrWhiteSpace(s) ? null : s;
    }
}
