//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonsRegistry
//-----------------------------------------------------------------------------

using eRaven.Application.Commands;
using eRaven.Application.DTOs;
using Microsoft.AspNetCore.Components;

namespace eRaven.Components.Pages.Persons.Registry;

public partial class PersonsRegistry
{
    [Inject] public ICommandHandler<CreatePersonCandidateCommand, Guid> CreateCandidateHandler { get; set; } = default!;

    private bool _createOpen;

    private Task OpenCreateCandidate()
    {
        _createOpen = true;
        return Task.CompletedTask;
    }

    private async Task CreateCandidate(CreateCandidateDto dto)
    {
        var command = new CreatePersonCandidateCommand(
            dto.Rnokpp,
            dto.LastName,
            dto.FirstName,
            dto.MiddleName,
            dto.PlannedPosition);

        await CreateCandidateHandler.HandleAsync(command);
    }
}