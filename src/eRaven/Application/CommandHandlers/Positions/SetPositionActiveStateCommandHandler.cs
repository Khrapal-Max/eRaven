//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// SetPositionActiveStateCommandHandler
//-----------------------------------------------------------------------------

using eRaven.Application.Commands.Positions;
using eRaven.Application.Repositories;
using eRaven.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Application.CommandHandlers.Positions;

public sealed class SetPositionActiveStateCommandHandler(
    IDbContextFactory<AppDbContext> dbFactory,
    IPersonRepository personRepository)
        : ICommandHandler<SetPositionActiveStateCommand>
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory = dbFactory;
    private readonly IPersonRepository _personRepository = personRepository;

    public async Task HandleAsync(
        SetPositionActiveStateCommand cmd,
        CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var position = await db.PositionUnits
            .FirstOrDefaultAsync(p => p.Id == cmd.PositionId, ct)
            ?? throw new InvalidOperationException("Посада не знайдена");

        if (position.IsActived == cmd.IsActive)
            return;

        if (!cmd.IsActive)
        {
            // Перевірка через repository
            var occupied = _personRepository.IsPositionOccupied(cmd.PositionId);

            if (occupied)
                throw new InvalidOperationException(
                    "Неможливо деактивувати посаду, поки вона закріплена за людиною");

            position.IsActived = false;
        }
        else
        {
            // Перевірка дублю коду при активації
            if (!string.IsNullOrWhiteSpace(position.Code))
            {
                var duplicateExists = await db.PositionUnits
                    .AnyAsync(p => p.IsActived &&
                                   p.Code == position.Code &&
                                   p.Id != cmd.PositionId, ct);

                if (duplicateExists)
                    throw new InvalidOperationException(
                        "Активна посада з таким кодом вже існує");
            }

            position.IsActived = true;
        }

        await db.SaveChangesAsync(ct);
    }
}