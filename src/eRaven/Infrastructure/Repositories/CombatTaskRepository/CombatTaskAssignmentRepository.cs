//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CombatTaskAssignmentRepository
//-----------------------------------------------------------------------------

using eRaven.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Infrastructure.Repositories.CombatTaskRepository;

public sealed class CombatTaskAssignmentRepository(IDbContextFactory<AppDbContext> dbFactory)
    : ICombatTaskAssignmentRepository
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory = dbFactory;

    // не підключений
    public async Task<CombatTaskAssignment?> GetOpenAssignmentForPersonAsync(
        Guid personId,
        CancellationToken ct = default)
    {
        if (personId == Guid.Empty) throw new ArgumentException("PersonId is required.", nameof(personId));

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        return await db.Set<CombatTaskAssignment>()
            .AsNoTracking()
            .Where(x => x.PersonId == personId && x.EndedAt == null)
            .OrderByDescending(x => x.StartedAt)
            .ThenByDescending(x => x.Id)
            .FirstOrDefaultAsync(ct);
    }

    // не підключений
    public async Task CreateAssignmentAsync(
        CombatTaskAssignment assignment,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(assignment);

        if (assignment.Id == Guid.Empty) throw new ArgumentException("Assignment.Id is required.", nameof(assignment));
        if (assignment.PersonId == Guid.Empty) throw new ArgumentException("PersonId is required.", nameof(assignment));
        if (string.IsNullOrWhiteSpace(assignment.RNOKPP)) throw new ArgumentException("RNOKPP is required.", nameof(assignment));
        if (string.IsNullOrWhiteSpace(assignment.FullName)) throw new ArgumentException("FullName is required.", nameof(assignment));
        if (string.IsNullOrWhiteSpace(assignment.PlanningDocTitle)) throw new ArgumentException("PlanningDocTitle is required.", nameof(assignment));
        if (string.IsNullOrWhiteSpace(assignment.PositionalArea)) throw new ArgumentException("PositionalArea is required.", nameof(assignment));
        if (string.IsNullOrWhiteSpace(assignment.GroupName)) throw new ArgumentException("GroupName is required.", nameof(assignment));
        if (string.IsNullOrWhiteSpace(assignment.Goal)) throw new ArgumentException("Goal is required.", nameof(assignment));
        if (assignment.StartDocumentId == Guid.Empty) throw new ArgumentException("StartDocumentId is required.", nameof(assignment));
        if (string.IsNullOrWhiteSpace(assignment.CreatedBy)) assignment.CreatedBy = "system";

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        db.Set<CombatTaskAssignment>().Add(assignment);

        // IMPORTANT:
        // уникальний partial index (PersonId where EndedAt is null) дасть гарантію 1 активного.
        // якщо хтось спробує створити 2 відкритих — впаде DbUpdateException.
        await db.SaveChangesAsync(ct);
    }

    // не підключений
    public async Task CloseAssignmentAsync(
        Guid assignmentId,
        DateOnly endedAt,
        Guid endDocumentId,
        string author,
        DateTime nowUtc,
        CancellationToken ct = default)
    {
        if (assignmentId == Guid.Empty) throw new ArgumentException("AssignmentId is required.", nameof(assignmentId));
        if (endDocumentId == Guid.Empty) throw new ArgumentException("EndDocumentId is required.", nameof(endDocumentId));
        if (string.IsNullOrWhiteSpace(author)) author = "system";

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var a = await db.Set<CombatTaskAssignment>()
            .FirstOrDefaultAsync(x => x.Id == assignmentId, ct)
            ?? throw new InvalidOperationException("Активне завдання не знайдено.");

        if (a.EndedAt is not null)
            throw new InvalidOperationException("Завдання вже закрите.");

        if (endedAt < a.StartedAt)
            throw new InvalidOperationException("Дата закриття не може бути раніше дати початку.");

        a.EndedAt = endedAt;
        a.EndDocumentId = endDocumentId;
        a.UpdatedBy = author.Trim();
        a.UpdatedAtUtc = nowUtc;

        await db.SaveChangesAsync(ct);
    }
}