//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// IPersonRepository
//-----------------------------------------------------------------------------

using eRaven.Application.Commands.PersonInfo;
using eRaven.Application.Commands.PersonMove;
using eRaven.Application.DTOs;
using eRaven.Application.DTOs.Excel;
using eRaven.Application.Queries.Personal;

namespace eRaven.Infrastructure.Repositories.PersonRepository;

public interface IPersonRepository
{
    // Read-side
    Task<PagedResult<PersonListItemDto>> GetPageAsync(GetPersonsPageQuery query, CancellationToken ct = default);
    Task<PersonDetailsDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<PersonEventDto>> GetHistoryAsync(Guid id, CancellationToken ct = default);

    // Commands aggregate (load aggregate -> execute -> append events -> project)
    Task<Guid> CreateReservedAsync(CreateReservedCommand cmd, CancellationToken ct = default);
    Task EnrollAsync(EnrollCommand cmd, CancellationToken ct = default);
    Task ExcludeAsync(ExcludeCommand cmd, CancellationToken ct = default);

    // Person info changes
    Task UpdatePersonalInfoAsync(UpdatePersonalInfoCommand cmd, CancellationToken ct = default);
    Task ChangeRankAsync(ChangeRankCommand cmd, CancellationToken ct = default);
    Task ChangePositionAsync(ChangePositionCommand cmd, CancellationToken ct = default);
    Task ChangeBzvpAsync(ChangeBzvpCommand cmd, CancellationToken ct = default);
    Task ChangeWeaponAsync(ChangeWeaponCommand cmd, CancellationToken ct = default);
    Task ChangeCallsignAsync(ChangeCallsignCommand cmd, CancellationToken ct = default);

    // Void event person
    Task VoidEventAsync(VoidPersonEventCommand cmd, CancellationToken ct = default);

    // Excel import helpers
    Task<IReadOnlySet<string>> GetExistingRnokppsAsync(
     IReadOnlyCollection<string> rnokpps,
     CancellationToken ct = default);

    Task<Guid> BootstrapCreateAndEnrollAsync(
        PersonBootstrapRowDto row,
        string author,
        DateTime nowUtc,
        CancellationToken ct = default);
}
