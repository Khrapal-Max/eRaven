//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// UpsertReport for ExselImport component
//-----------------------------------------------------------------------------

namespace eRaven.Application.ViewModels;

/// <summary>
/// Звіт після імпорту ексель файла
/// </summary>
public sealed record ImportReportViewModel(int Added, int Updated, List<string> Errors);