//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// PersonAssignmentExportRow
//-----------------------------------------------------------------------------

namespace eRaven.Application.ViewModels.PositionAssignmentViewModels;

/// <summary>
/// Плоский рядок для експорту «Призначення на посаду».
/// — тільки скалярні поля, щоб IExcelService коректно серіалізував.
/// </summary>
public class PersonAssignmentExportRow
{
    public string? Rank { get; set; }
    public string? Rnokpp { get; set; }
    public string? FullName { get; set; }

    public string? Code { get; set; }
    public string? SpecialNumber { get; set; }

    public string? Position { get; set; }
}
