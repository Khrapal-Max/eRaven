//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonsRegistry
//-----------------------------------------------------------------------------

using eRaven.Application.Commands;
using eRaven.Application.Commands.PersonMove;
using eRaven.Application.DTOs;
using Microsoft.AspNetCore.Components;

namespace eRaven.Components.Pages.Persons.Registry;

public partial class PersonsRegistry
{
    [Inject] public ICommandHandler<CreateReservedCommand, Guid> CreateReservedHandler { get; set; } = default!;

    private bool _createReservedOpen;

    private Task OpenCreateReserved()
    {
        _createReservedOpen = true;
        return Task.CompletedTask;
    }

    private async Task HandleCreateReservedAsync(CreateReservedDto dto)
    {
        var cmd = new CreateReservedCommand(
             PersonId: Guid.NewGuid(),
             Rnokpp: dto.Rnokpp,
             LastName: dto.LastName,
             FirstName: dto.FirstName,
             MiddleName: dto.MiddleName,
             Rank: dto.Rank,
             Position: dto.Position,
             Bzvp: dto.Bzvp,
             Weapon: dto.Weapon,
             Callsign: dto.Callsign,
             Author: "system",          // TODO: замінити на User.Identity.Name
             NowUtc: DateTime.UtcNow);

        await CreateReservedHandler.HandleAsync(cmd);
    }
}