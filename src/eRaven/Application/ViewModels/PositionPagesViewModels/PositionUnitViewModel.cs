//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PositionUnitViewModel
//-----------------------------------------------------------------------------

namespace eRaven.Application.ViewModels.PositionPagesViewModels;

public class PositionUnitViewModel
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string ShortName { get; set; } = string.Empty;
    public string SpecialNumber { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? CurrentPersonFullName { get; set; }
    public bool IsActived { get; set; }
}
