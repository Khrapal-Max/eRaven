//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonAggregateProjector
//-----------------------------------------------------------------------------

using eRaven.Domain.Models;
using eRaven.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Application.Projector;

/// <summary>
/// Проектор, який синхронізує стан агрегата <see cref="Person"/> з реляційними таблицями.
/// </summary>
internal static class PersonAggregateProjector
{
    public static async Task ProjectAsync(AppDbContext db, Person aggregate, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(db);
        ArgumentNullException.ThrowIfNull(aggregate);

        await ProjectPersonAsync(db, aggregate, ct);
        await ProjectAssignmentsAsync(db, aggregate, ct);
        await ProjectStatusesAsync(db, aggregate, ct);
        await ProjectPlanActionsAsync(db, aggregate, ct);
    }

    private static async Task ProjectPersonAsync(AppDbContext db, Person aggregate, CancellationToken ct)
    {
        var entity = await db.Persons
            .FirstOrDefaultAsync(p => p.Id == aggregate.Id, ct);

        if (entity is null)
        {
            entity = new Person { Id = aggregate.Id };
            db.Persons.Add(entity);
        }

        entity.Rnokpp = aggregate.Rnokpp;
        entity.Rank = aggregate.Rank;
        entity.LastName = aggregate.LastName;
        entity.FirstName = aggregate.FirstName;
        entity.MiddleName = aggregate.MiddleName;
        entity.BZVP = aggregate.BZVP;
        entity.Weapon = aggregate.Weapon;
        entity.Callsign = aggregate.Callsign;
        entity.PositionUnitId = aggregate.PositionUnitId;
        entity.StatusKindId = aggregate.StatusKindId;
        entity.IsAttached = aggregate.IsAttached;
        entity.AttachedFromUnit = aggregate.AttachedFromUnit;
        entity.CreatedUtc = aggregate.CreatedUtc;
        entity.ModifiedUtc = aggregate.ModifiedUtc;
    }

    private static async Task ProjectAssignmentsAsync(AppDbContext db, Person aggregate, CancellationToken ct)
    {
        var existing = await db.PersonPositionAssignments
            .Where(a => a.PersonId == aggregate.Id)
            .ToDictionaryAsync(a => a.Id, ct);

        var present = new HashSet<Guid>();

        foreach (var assignment in aggregate.PositionAssignments)
        {
            if (!existing.TryGetValue(assignment.Id, out var entity))
            {
                entity = new PersonPositionAssignment
                {
                    Id = assignment.Id,
                    PersonId = aggregate.Id
                };
                db.PersonPositionAssignments.Add(entity);
            }

            present.Add(assignment.Id);

            entity.PositionUnitId = assignment.PositionUnitId;
            entity.OpenUtc = assignment.OpenUtc;
            entity.CloseUtc = assignment.CloseUtc;
            entity.Note = assignment.Note;
            entity.Author = assignment.Author;
            entity.ModifiedUtc = assignment.ModifiedUtc;
        }

        foreach (var (id, entity) in existing)
        {
            if (!present.Contains(id))
            {
                db.PersonPositionAssignments.Remove(entity);
            }
        }
    }

    private static async Task ProjectStatusesAsync(AppDbContext db, Person aggregate, CancellationToken ct)
    {
        var existing = await db.PersonStatuses
            .Where(s => s.PersonId == aggregate.Id)
            .ToDictionaryAsync(s => s.Id, ct);

        var present = new HashSet<Guid>();

        foreach (var status in aggregate.StatusHistory)
        {
            if (!existing.TryGetValue(status.Id, out var entity))
            {
                entity = new PersonStatus
                {
                    Id = status.Id,
                    PersonId = aggregate.Id
                };
                db.PersonStatuses.Add(entity);
            }

            present.Add(status.Id);

            entity.StatusKindId = status.StatusKindId;
            entity.OpenDate = status.OpenDate;
            entity.IsActive = status.IsActive;
            entity.Sequence = status.Sequence;
            entity.Note = status.Note;
            entity.Author = status.Author;
            entity.Modified = status.Modified;
            entity.SourceDocumentId = status.SourceDocumentId;
            entity.SourceDocumentType = status.SourceDocumentType;
        }

        foreach (var (id, entity) in existing)
        {
            if (!present.Contains(id))
            {
                db.PersonStatuses.Remove(entity);
            }
        }
    }

    private static async Task ProjectPlanActionsAsync(AppDbContext db, Person aggregate, CancellationToken ct)
    {
        var existing = await db.PlanActions
            .Where(a => a.PersonId == aggregate.Id)
            .ToDictionaryAsync(a => a.Id, ct);

        var present = new HashSet<Guid>();

        foreach (var action in aggregate.PlanActions)
        {
            if (!existing.TryGetValue(action.Id, out var entity))
            {
                entity = new PlanAction
                {
                    Id = action.Id,
                    PersonId = aggregate.Id
                };
                db.PlanActions.Add(entity);
            }

            present.Add(action.Id);

            entity.PlanActionName = action.PlanActionName;
            entity.EffectiveAtUtc = action.EffectiveAtUtc;
            entity.ToStatusKindId = action.ToStatusKindId;
            entity.Order = action.Order;
            entity.ActionState = action.ActionState;
            entity.MoveType = action.MoveType;
            entity.Location = action.Location;
            entity.GroupName = action.GroupName;
            entity.CrewName = action.CrewName;
            entity.Note = action.Note;
            entity.Rnokpp = action.Rnokpp;
            entity.FullName = action.FullName;
            entity.RankName = action.RankName;
            entity.PositionName = action.PositionName;
            entity.BZVP = action.BZVP;
            entity.Weapon = action.Weapon;
            entity.Callsign = action.Callsign;
            entity.StatusKindOnDate = action.StatusKindOnDate;
        }

        foreach (var (id, entity) in existing)
        {
            if (!present.Contains(id))
            {
                db.PlanActions.Remove(entity);
            }
        }
    }
}
