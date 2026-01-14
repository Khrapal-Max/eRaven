//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// BootstrapPersonsResult
//-----------------------------------------------------------------------------

namespace eRaven.Application.Commands.Excel;

public sealed record BootstrapPersonsResult(
    int TotalRows,
    int CreatedCount,
    int SkippedCount,
    IReadOnlyList<BootstrapPersonsError> Errors
);