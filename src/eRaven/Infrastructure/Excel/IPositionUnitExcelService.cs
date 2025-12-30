//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// IPositionUnitExcelService
//-----------------------------------------------------------------------------

using eRaven.Domain.Entities;

namespace eRaven.Infrastructure.Excel;

public interface IPositionUnitExcelService
{
    byte[] Export(IEnumerable<PositionUnit> items);
    Task<PositionUnitImportResult> ParseAsync(Stream stream, CancellationToken ct);
}
