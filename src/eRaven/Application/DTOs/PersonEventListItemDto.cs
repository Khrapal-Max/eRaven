//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonEventListItemDto
//-----------------------------------------------------------------------------

namespace eRaven.Application.DTOs;

public sealed record PersonEventListItemDto(
    long Version,
    Guid EventId,
    DateOnly? EffectiveDate,
    string Title,
    string? Details,
    string Author,
    DateTime OccurredAtUtc
);