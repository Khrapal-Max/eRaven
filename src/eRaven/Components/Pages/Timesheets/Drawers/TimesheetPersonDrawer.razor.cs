//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetPersonDrawer
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.Enums;
using eRaven.Application.DTOs.Timesheets;
using eRaven.Components.Shared.Drawer;
using Microsoft.AspNetCore.Components;

namespace eRaven.Components.Pages.Timesheets.Drawers;

/// <summary>
/// Drawer “Картка в табелі” (короткий профіль):
/// показує snapshot особи (ПІБ/РНОКПП/звання/посада/дати)
/// і дає перехід на “Особовий табель” цієї особи.
/// </summary>
public partial class TimesheetPersonDrawer : ComponentBase
{
    //======================================================================
    // DI
    //======================================================================

    /// <summary>Навігація на сторінку особового табелю.</summary>
    [Inject] public NavigationManager NavigationManager { get; set; } = default!;

    //======================================================================
    // Parameters
    //======================================================================

    /// <summary>Ознака відкриття drawer.</summary>
    [Parameter] public bool IsOpen { get; set; }

    /// <summary>Callback для керування відкриттям drawer.</summary>
    [Parameter] public EventCallback<bool> IsOpenChanged { get; set; }

    /// <summary>Дані особи (snapshot для відображення).</summary>
    [Parameter] public TimesheetPersonInfoDto? Person { get; set; }

    /// <summary>Callback, який викликається після закриття drawer.</summary>
    [Parameter] public EventCallback OnClosed { get; set; }

    //======================================================================
    // State
    //======================================================================

    private Drawer? _drawer;

    //======================================================================
    // UI actions
    //======================================================================

    /// <summary>
    /// Закриває drawer та повідомляє батьківський компонент.
    /// </summary>
    private async Task CloseAsync()
    {
        await IsOpenChanged.InvokeAsync(false);
        await OnClosed.InvokeAsync();
    }

    /// <summary>
    /// Обробник закриття (натискання “X”, клік поза drawer тощо).
    /// Тримай це як single source of truth для “OnClosed”.
    /// </summary>
    private Task HandleClosed()
        => OnClosed.InvokeAsync();

    /// <summary>
    /// Переходить на особовий табель (місячна сторінка) для поточної особи.
    /// </summary>
    private void OpenPersonTimesheet()
    {
        if (Person is null) return;
        NavigationManager.NavigateTo($"/timesheet/person/{Person.PersonId}");
    }

    //======================================================================
    // Helpers
    //======================================================================

    /// <summary>
    /// Людський текст для типу обліку (EnrollmentKind).
    /// </summary>
    private static string GetSign(EnrollmentKindDto kind)
        => kind switch
        {
            EnrollmentKindDto.Unit => "Штат",
            EnrollmentKindDto.AttachedByList => "Приданий по наказу (котел)",
            EnrollmentKindDto.AttachedByOrder => "Приданий по БР",
            _ => "ВКЛ"
        };
}