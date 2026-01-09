//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonsRegistry
//-----------------------------------------------------------------------------

using eRaven.Application.Commands;
using eRaven.Application.DTOs;
using eRaven.Application.Handlers;
using Microsoft.AspNetCore.Components;

namespace eRaven.Components.Pages.Persons.Registry;

public partial class PersonsRegistry
{
    [Inject] public CreateCandidateCommandHandler CreateCandidateHandler { get; set; } = default!;

    private bool _createOpen;

    private Task OpenCreateCandidate()
    {
        _createOpen = true;
        return Task.CompletedTask;
    }

    private async Task CreateCandidate(CreateCandidateDto dto)
    {
        var command = new CreatePersonCandidateCommand(
            Rnokpp: dto.Rnokpp,
            LastName: dto.LastName,
            FirstName: dto.FirstName,
            MiddleName: dto.MiddleName,
            PlannedPosition: dto.PlannedPosition);

        await CreateCandidateHandler.HandleAsync(command);
    }
}
