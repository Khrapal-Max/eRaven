//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CreatePositionUnitViewModel
//-----------------------------------------------------------------------------

using System.ComponentModel.DataAnnotations;

namespace eRaven.Application.ViewModels.PositionPagesViewModels;

public class CreatePositionUnitViewModel
{
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Коротка назва обовʼязкова")]
    [MaxLength(128)]
    public string ShortName { get; set; } = string.Empty;

    [MaxLength(15)]
    public string SpecialNumber { get; set; } = string.Empty;

    [MaxLength(512)]
    public string OrgPath { get; set; } = string.Empty;
}
