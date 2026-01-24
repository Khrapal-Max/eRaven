//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetPersonDrawer
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.Timesheet;
using eRaven.Components.Shared.Drawer;
using eRaven.Domain.Enums;
using Microsoft.AspNetCore.Components;

namespace eRaven.Components.Pages.Timesheet.Drawers;

public partial class TimesheetPersonDrawer
{
    [Inject] public NavigationManager Nav { get; set; } = default!;

    [Parameter] public bool IsOpen { get; set; }
    [Parameter] public EventCallback<bool> IsOpenChanged { get; set; }
    [Parameter] public TimesheetPersonMonthRowDto? Person { get; set; }
    [Parameter] public EventCallback OnClosed { get; set; }

    private Drawer? _drawer;

    private async Task CloseAsync()
    {
        await IsOpenChanged.InvokeAsync(false);
        await OnClosed.InvokeAsync();
    }

    private async Task HandleClosed()
        => await OnClosed.InvokeAsync();

    private static string GetSign(EnrollmentKind? kind)
        => kind switch
        {
            EnrollmentKind.Unit => "Штат",
            EnrollmentKind.AttachedByList => "Приданий по наказу (котел)",
            EnrollmentKind.AttachedByOrder => "Приданий по БР",
            _ => "ВКЛ"
        };
}