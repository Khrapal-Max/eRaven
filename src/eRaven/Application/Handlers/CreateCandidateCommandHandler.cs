//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CreateCandidateCommandHandler
//-----------------------------------------------------------------------------

using eRaven.Application.Commands;
using eRaven.Domain.Aggregates;
using eRaven.Domain.Enums;
using eRaven.Domain.ValueObjects;
using eRaven.Infrastructure;
using eRaven.Infrastructure.Repositories.PersonRepository;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Application.Handlers;

public sealed class CreateCandidateCommandHandler(
    IDbContextFactory<AppDbContext> dbFactory,
    IPersonRepository personRepo)
    : ICommandHandler<CreatePersonCandidateCommand, Guid>
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory = dbFactory;
    private readonly IPersonRepository _personRepo = personRepo;

    public async Task<Guid> HandleAsync(CreatePersonCandidateCommand command, CancellationToken ct = default)
    {
        var id = Guid.NewGuid();

        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        PersonAggregate? agg = null;
        long newVersion = 0;

        try
        {
            // 1) (опционально) проверка дубля RNOKPP по read-model
            // ВАЖНО: это не 100% защита от гонок — всё равно нужен UNIQUE в БД.
            var rnokppExists = await db.PersonRead
                .AsNoTracking()
                .AnyAsync(x => x.Rnokpp == command.Rnokpp, ct);

            if (rnokppExists)
                throw new InvalidOperationException("Особа з таким РНОКПП вже існує.");

            // 2) если выбрана вакансия — резервируем
            if (command.PlannedPositionUnitId is Guid posId)
            {
                var pos = await db.PositionUnits
                    .SingleOrDefaultAsync(x => x.Id == posId, ct)
                    ?? throw new InvalidOperationException("Обрана посада не знайдена.");

                if (!pos.IsActived || pos.State != PositionUnitState.Vacant)
                    throw new InvalidOperationException("Посада вже зайнята або зарезервована.");

                pos.State = PositionUnitState.TemporarilyCandidate;

                await db.SaveChangesAsync(ct);
            }

            // 3) создаём aggregate + события
            var personal = new PersonalInfo(
                rnokpp: command.Rnokpp,
                lastName: command.LastName,
                firstName: command.FirstName,
                middleName: command.MiddleName);

            agg = PersonAggregate.CreateCandidate(
                id: id,
                personal: personal,
                plannedPosition: command.PlannedPosition,
                author: "author",
                nowUtc: DateTime.UtcNow);

            // 4) persist events через репозиторий, НО в том же db/tx
            // expectedVersion: 0 для нового агрегата
            newVersion = await _personRepo.SaveAsync(db, agg, expectedVersion: 0, ct);

            // 5) коммитим всё одним атомарным блоком (reserve + append + projection)
            await tx.CommitAsync(ct);

            // 6) после коммита фиксируем состояние агрегата в памяти
            agg.MarkChangesAsCommitted(newVersion);

            return id;
        }
        catch
        {
            // если что-то пошло не так — откат транзакции
            await tx.RollbackAsync(ct);
            throw;
        }
    }
}