//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// RegistryImportExport
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.Excel;

namespace eRaven.Application.Commands.Excel;

public sealed record BootstrapPersonsCommand(
    IReadOnlyList<PersonBootstrapRowDto> Rows,
    string Author,
    DateTime NowUtc
);
