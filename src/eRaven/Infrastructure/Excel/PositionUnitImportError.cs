//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PositionUnitImportError
//-----------------------------------------------------------------------------

namespace eRaven.Infrastructure.Excel;

public sealed record PositionUnitImportError(int RowNumber, string Field, string Message);