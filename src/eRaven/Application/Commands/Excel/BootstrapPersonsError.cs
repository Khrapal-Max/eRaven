//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// BootstrapPersonsError
//-----------------------------------------------------------------------------

namespace eRaven.Application.Commands.Excel;

public sealed record BootstrapPersonsError(
    int RowNumber,
    string Rnokpp,
    string Message
);