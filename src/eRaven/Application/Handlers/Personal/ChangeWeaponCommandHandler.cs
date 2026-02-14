//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ChangeWeaponCommandHandler
//-----------------------------------------------------------------------------

using eRaven.Application.Commands;
using eRaven.Application.Commands.PersonInfo;
using eRaven.Infrastructure.Repositories.PersonRepository;

namespace eRaven.Application.Handlers.Personal;

public class ChangeWeaponCommandHandler(IPersonRepository repo)
    : ICommandHandler<ChangeWeaponCommand>
{
    private readonly IPersonRepository _repo = repo;

    public async Task HandleAsync(ChangeWeaponCommand command, CancellationToken ct = default)
        => await _repo.ChangeWeaponAsync(command.PersonId,
            command.EffectiveDate,
            command.Weapon,
            command.Author,
            command.NowUtc,
            ct: ct);
}
