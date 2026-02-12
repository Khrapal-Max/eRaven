//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetPolicyRepository
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.Timesheet;
using eRaven.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Infrastructure.Repositories.TimesheetPolicyRepository;

public sealed class TimesheetPolicyRepository(IDbContextFactory<AppDbContext> dbFactory)
    : ITimesheetPolicyRepository
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory = dbFactory;

    //======================================================================
    // Codes
    //======================================================================

    /// <inheritdoc />
    public async Task<IReadOnlyList<TimesheetCodeDefinition>> GetCodesAsync(
    bool includeInactive = false,
    CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var q = db.TimesheetCodes.AsNoTracking();

        if (!includeInactive)
            q = q.Where(x => x.IsActive);

        return await q
            .Where(x => !x.Code.Equals(TimesheetSystemCodes.NotInTimesheet))
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Priority)
            .ThenBy(x => x.Code)
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task<TimesheetCodeDefinition?> GetCodeByIdAsync(Guid codeId, CancellationToken ct = default)
    {
        if (codeId == Guid.Empty)
            throw new ArgumentException("codeId is required.", nameof(codeId));

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        return await db.TimesheetCodes
            .AsNoTracking()
            .Where(x => x.IsActive)
            .FirstOrDefaultAsync(x => x.Id == codeId, ct);
    }

    /// <inheritdoc />
    public async Task<Guid> AddCodeAsync(
        string code,
        string title,
        string? description,
        int sortOrder,
        int priority,
        bool isTerminal,
        string author,
        DateTime nowUtc,
        CancellationToken ct = default)
    {
        code = (code ?? string.Empty).Trim();
        title = (title ?? string.Empty).Trim();
        description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

        if (string.IsNullOrWhiteSpace(code))
            throw new InvalidOperationException("Код не може бути порожнім.");
        if (string.IsNullOrWhiteSpace(title))
            throw new InvalidOperationException("Назва коду не може бути порожньою.");
        if (sortOrder < 0)
            throw new InvalidOperationException("SortOrder не може бути < 0.");
        if (priority < 0)
            throw new InvalidOperationException("Priority не може бути < 0.");
        if (string.IsNullOrWhiteSpace(author))
            throw new InvalidOperationException("Author is required.");

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
            throw new ArgumentException("codeId is required.", nameof(codeId));
        if (string.IsNullOrWhiteSpace(author))
            throw new InvalidOperationException("Author is required.");

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var code = await db.TimesheetCodes
            .FirstOrDefaultAsync(x => x.Id == codeId, ct)
            ?? throw new InvalidOperationException("Код не знайдено.");

        if (!code.IsActive)
            return;

        code.IsActive = false;
        code.UpdatedBy = author;
        code.UpdatedAtUtc = nowUtc;

        await db.SaveChangesAsync(ct);
    }

    //======================================================================
    // Transitions (Rules)
    //======================================================================

    /// <inheritdoc />
    public async Task<IReadOnlyList<TimesheetCodeTransition>> GetAllowedTransitionsAsync(
    Guid fromCodeId,
    CancellationToken ct = default)
    {
        if (fromCodeId == Guid.Empty)
            throw new ArgumentException("fromCodeId is required.", nameof(fromCodeId));

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        return await db.TimesheetCodeTransitions
            .AsNoTracking()
            .Where(x => x.FromCodeId == fromCodeId)
            .Include(x => x.ToCode)
            // UI/handler не повинні бачити переходи у закриті або системні коди
            .Where(x => x.ToCode.IsActive)
            .Where(x => !x.ToCode.Code.Equals(TimesheetSystemCodes.NotInTimesheet))
            .OrderBy(x => x.ToCode.SortOrder)
            .ThenBy(x => x.ToCode.Priority)
            .ThenBy(x => x.ToCode.Code)
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TimesheetTransitionOptionDto>> GetAllowedTransitionOptionsAsync(
        string code,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("fromCodeId is required.", nameof(code));

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var codeId = await db.TimesheetCodes
            .AsNoTracking()
            .Where(x => x.IsActive)
            .Where(x => x.Code == code)
            .Select(x => x.Id)
            .FirstOrDefaultAsync(ct);

        if (codeId == Guid.Empty)
            throw new ArgumentException($"Code '{code}' not found.", nameof(code));

        return await db.TimesheetCodeTransitions
            .AsNoTracking()
            .Where(x => x.FromCodeId == codeId)
            .Where(x => x.ToCode.IsActive)
            .Where(x => !x.ToCode.Code.Equals(TimesheetSystemCodes.NotInTimesheet))
            .OrderBy(x => x.ToCode.SortOrder)
            .ThenBy(x => x.ToCode.Priority)
            .ThenBy(x => x.ToCode.Code)
            .Select(x => new TimesheetTransitionOptionDto(
                x.ToCode.Code,
                x.ToCode.Title,
                x.StartShiftDays))
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task SavePolicyAsync(
        Guid codeId,
        string title,
        string? description,
        int sortOrder,
        int priority,
        bool isTerminal,
        IReadOnlyCollection<TimesheetTransitionSpecDto> allowedTransitions,
        string author,
        DateTime nowUtc,
        CancellationToken ct = default)
    {
        if (codeId == Guid.Empty)
            throw new ArgumentException("codeId is required.", nameof(codeId));

        title = (title ?? string.Empty).Trim();
        description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

        if (string.IsNullOrWhiteSpace(title))
            throw new InvalidOperationException("Назва коду не може бути порожньою.");
        if (sortOrder < 0)
            throw new InvalidOperationException("SortOrder не може бути < 0.");
        if (priority < 0)
            throw new InvalidOperationException("Priority не може бути < 0.");
        if (string.IsNullOrWhiteSpace(author))
            throw new InvalidOperationException("Author is required.");

        allowedTransitions ??= [];

        // normalize transitions
        var normalized = allowedTransitions
            .Where(x => x.ToCodeId != Guid.Empty)
            .Where(x => x.ToCodeId != codeId)
            .Select(x => new TimesheetTransitionSpecDto(x.ToCodeId, x.StartShiftDays))
            .GroupBy(x => x.ToCodeId)
            .Select(g => g.First())
            .ToList();

        foreach (var t in normalized)
        {
            // для вашої карти — 0 або 1, але залишимо маленький запас
            if (t.StartShiftDays is < 0 or > 7)
                throw new InvalidOperationException("StartShiftDays має бути в межах 0..7 (для карти зазвичай 0 або 1).");
        }

        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var code = await db.TimesheetCodes
            .FirstOrDefaultAsync(x => x.Id == codeId, ct)
            ?? throw new InvalidOperationException("Код не знайдено.");

        // Оновлюємо тільки редаговані поля — не чіпаємо Code/Created*
        code.Title = title;
        code.Description = description;
        code.SortOrder = sortOrder;
        code.Priority = priority;
        code.IsTerminal = isTerminal;
        code.UpdatedBy = author;
        code.UpdatedAtUtc = nowUtc;

        // Перевіряємо що всі ToCode існують та активні (щоб UI не зберіг "мертві" переходи)
        var toIds = normalized.Select(x => x.ToCodeId).ToList();
        if (toIds.Count > 0)
        {
            var activeTo = await db.TimesheetCodes
                .AsNoTracking()
                .Where(x => toIds.Contains(x.Id))
                .Where(x => x.IsActive)
                .Select(x => x.Id)
                .ToListAsync(ct);

            var missing = toIds.Except(activeTo).ToList();
            if (missing.Count > 0)
                throw new InvalidOperationException("Серед переходів є коди, які не існують або вже закриті (неактивні).");
        }

        // ---- DIFF update transitions (не delete-all) ----
        var existing = await db.TimesheetCodeTransitions
            .Where(x => x.FromCodeId == codeId)
            .ToListAsync(ct);

        var existingByTo = existing.ToDictionary(x => x.ToCodeId, x => x);

        // update or add
        foreach (var desired in normalized)
        {
            if (existingByTo.TryGetValue(desired.ToCodeId, out var tr))
            {
                // update only if changed
                if (tr.StartShiftDays != desired.StartShiftDays)
                    tr.StartShiftDays = desired.StartShiftDays;

                // CreatedBy/CreatedAtUtc зберігаємо (щоб не "губились дані")
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

        // remove missing
        var desiredSet = normalized.Select(x => x.ToCodeId).ToHashSet();
        foreach (var old in existing)
        {
            if (!desiredSet.Contains(old.ToCodeId))
                db.TimesheetCodeTransitions.Remove(old);
        }

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }
}