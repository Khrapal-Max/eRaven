//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// SetPersonStatusViewModel
//-----------------------------------------------------------------------------

using System.ComponentModel.DataAnnotations;

namespace eRaven.Application.ViewModels.PersonStatusViewModels;

public class SetPersonStatusViewModel
{
    [Required]
    public Guid PersonId { get; set; }

    [Required]
    public int StatusId { get; set; }

    /// <summary>
    /// Момент локальний із UI; бек нормалізує в UTC.
    /// Якщо вже маєш у UTC — проставляй з Kind=Utc.
    /// </summary>
    [Required]
    public DateTime Moment { get; set; }

    [MaxLength(512)]
    public string? Note { get; set; }

    [MaxLength(64)]
    public string? Author { get; set; }
}
