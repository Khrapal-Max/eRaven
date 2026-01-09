//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CreatePersonCandidateCommand
//-----------------------------------------------------------------------------

namespace eRaven.Application.Commands;

public record CreatePersonCandidateCommand(
    string Rnokpp,
    string LastName,
    string FirstName,
    string? MiddleName,
    string? PlannedPosition
);