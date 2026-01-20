//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetPersonModal
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.Timesheet;
using eRaven.Domain.Enums;
using Microsoft.AspNetCore.Components;

namespace eRaven.Components.Pages.Timesheet;

public partial class TimesheetPersonModal
{
    [Parameter] public bool IsOpen { get; set; }
    [Parameter] public TimesheetMonthPerPersonDto? Person { get; set; }
    [Parameter] public EventCallback OnClose { get; set; }

    private async Task Close()
        => await OnClose.InvokeAsync();
}
