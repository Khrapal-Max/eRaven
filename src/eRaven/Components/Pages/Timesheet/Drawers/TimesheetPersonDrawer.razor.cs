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
    private Drawer? _drawer;

    [Parameter] public bool IsOpen { get; set; }
    [Parameter] public EventCallback<bool> IsOpenChanged { get; set; }

    [Parameter] public TimesheetMonthPerPersonDto? Person { get; set; }

    /// <summary>Щоб батько міг обнулити Person після закриття.</summary>
    [Parameter] public EventCallback OnClosed { get; set; }

    private async Task CloseAsync()
    {
        // закриваємо “правильно” через Drawer, щоб спрацювали esc/backdrop/X однаково
        if (_drawer is not null)
            await _drawer.CloseAsync();
        else
            await IsOpenChanged.InvokeAsync(false);
    }

    private async Task HandleClosed()
        => await OnClosed.InvokeAsync();

    private static string GetSign(EnrollmentKind? kind)
        => kind switch
        {
            EnrollmentKind.Unit => "Штат",
            EnrollmentKind.AttachedByList => "Приданий по наказу",
            EnrollmentKind.AttachedByOrder => "Приданий по БР",
            _ => "ВКЛ (Резерв)"
        };
}
