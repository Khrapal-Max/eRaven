//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ChangePersonStatusCommand
//-----------------------------------------------------------------------------

namespace eRaven.Application.Commands.Persons;

public sealed record ChangePersonStatusCommand(
    Guid PersonId,
    int NewStatusKindId,
    DateTime EffectiveAtUtc,
    string? Note = null,
    string? Author = null
);