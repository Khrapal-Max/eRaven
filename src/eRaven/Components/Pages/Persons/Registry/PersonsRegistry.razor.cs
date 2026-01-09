//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonsRegistry
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs;

namespace eRaven.Components.Pages.Persons.Registry;

public partial class PersonsRegistry
{
    private bool _createOpen;

    private Task OpenCreateCandidate()
    {
        _createOpen = true;
        return Task.CompletedTask;
    }

    private async Task CreateCandidate(CreateCandidateDto dto)
    {
        // TODO: тут буде command handler/service
        // await _personsService.CreateCandidateAsync(dto);

        await Task.CompletedTask;
    }
}
