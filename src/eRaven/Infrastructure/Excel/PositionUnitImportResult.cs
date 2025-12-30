//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PositionUnitImportResult
//-----------------------------------------------------------------------------

using eRaven.Domain.Entities;

namespace eRaven.Infrastructure.Excel;

public sealed class PositionUnitImportResult
{
    public List<PositionUnit> ValidItems { get; } = [];
    public List<PositionUnitImportError> Errors { get; } = [];
}