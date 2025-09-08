//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonStatusImportView
//-----------------------------------------------------------------------------

using System.ComponentModel.DataAnnotations;

namespace eRaven.Application.ViewModels.PersonStatusViewModel;

public class PersonStatusImportView
{
    [Display(Name = "RNOKPP")]
    public string? Rnokpp { get; set; }

    [Display(Name = "StatusKindId")]
    public int? StatusKindId { get; set; }

    [Display(Name = "StatusCode")]
    public string? StatusCode { get; set; } // опціонально, якщо зручно по коду

    [Display(Name = "FromDateLocal")]
    public DateTime FromDateLocal { get; set; } // локальна дата (без часу)

    [Display(Name = "Note")]
    public string? Note { get; set; }
}
