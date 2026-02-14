//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// IPersonRepository
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.Excel;
using eRaven.Application.DTOs.Person;
using eRaven.Domain.Enums;

namespace eRaven.Infrastructure.Repositories.PersonRepository;

public interface IPersonRepository
{
    // Read-side
    Task<PagedResult<PersonListItemDto>> GetPageAsync(int page,
        int pageSize,
        string? search = null,
        DateOnly? asOfDate = null,
        PersonLifecycle? lifecycle = null,
        EnrollmentKind? enrollmentKind = null,
        CancellationToken ct = default);

    Task<IReadOnlyList<CombatTaskPersonLookupDto>> GetPersonsSearchAsync(string search,
        int takePersons,
        CancellationToken ct = default);

    Task<PersonDetailsDto?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<PersonEventDto>> GetHistoryAsync(Guid id, CancellationToken ct = default);

    // Commands aggregate (load aggregate -> execute -> append events -> project)
    Task<Guid> CreateReservedAsync(string rnokpp,
        string lastName,
        string firstName,
        string? middleName,
        string? rank,
        string? position,
        string author,
        DateTime nowUtc,
        CancellationToken ct = default);

    Task EnrollAsync(Guid personId,
        EnrollmentKind kind,
        string? reference,
        string reason,
        DateOnly enrollDate,
        string rank,
        int positionSort,
        string position,
        string author,
        DateTime nowUtc,
        CancellationToken ct = default);

    Task ExcludeAsync(Guid personId,
        string reason,
        DateOnly effectiveDate,
        string author,
        DateTime nowUtc,
        CancellationToken ct = default);

    // Person info changes
    Task UpdatePersonalInfoAsync(Guid personId,
        string rnokpp,
        string lastName,
        string firstName,
        string? middleName,
        string? Note,
        string author,
        DateTime nowUtc,
        CancellationToken ct = default);

    Task ChangeRankAsync(Guid personId,
        DateOnly effectiveDate,
        string rank,
        string? note,
        string author,
        DateTime nowUtc,
        CancellationToken ct = default);

    Task ChangePositionAsync(Guid personId,
        DateOnly effectiveDate,
        int? positionSort,
        string? position,
        string? note,
        string author,
        DateTime nowUtc,
        CancellationToken ct = default);

    Task ChangeBzvpAsync(Guid personId,
        DateOnly effectiveDate,
        string bzvp,
        string? note,
        string author,
        DateTime nowUtc,
        CancellationToken ct = default);

    Task ChangeWeaponAsync(Guid personId,
        DateOnly effectiveDate,
        string? weapon,
        string author,
        DateTime nowUtc,
        CancellationToken ct = default);

    Task ChangeCallsignAsync(Guid personId,
        DateOnly effectiveDate,
        string? callsign,
        string author,
        DateTime nowUtc,
        CancellationToken ct = default);

    // Void event person
    Task VoidEventAsync(Guid personId,
        Guid targetEventId,
        string reason,
        string author,
        DateTime nowUtc,
        CancellationToken ct = default);

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
