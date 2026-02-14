//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonnelDashboardShell
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.Dashboard;
using eRaven.Application.Queries;
using eRaven.Application.Queries.Dashboard;
using eRaven.Domain.Enums;
using eRaven.Presentation.Toasts;
using Microsoft.AspNetCore.Components;

namespace eRaven.Components.Pages.Dashboard;

public partial class PersonnelDashboardShell : ComponentBase
{
    [Inject] public IQueryHandler<GetPersonnelDashboardQuery, PersonnelDashboardDto> DashboardQuery { get; set; } = default!;
    [Inject] public NavigationManager Nav { get; set; } = default!;
    [Inject] public ToastService ToastService { get; set; } = default!;

    private bool _loading;
    private PersonnelDashboardDto? _data;

    protected override async Task OnInitializedAsync()
    {
        await ReloadAsync();
    }

    private async Task ReloadAsync()
    {
        _loading = true;

        try
        {
            _data = await DashboardQuery.HandleAsync(new GetPersonnelDashboardQuery());
        }
        catch (Exception ex)
        {
            ToastService.Error($"Помилка завантаження даних: {ex.Message}");
        }
        finally
        {
            _loading = false;
        }
    }

    private void GoToRegistryTimesheetTotal()
        => Nav.NavigateTo($"/persons?lifecycle={PersonLifecycle.Enrolled}");

    private void GoToRegistryTimesheetKind(EnrollmentKind kind)
        => Nav.NavigateTo($"/persons?lifecycle={PersonLifecycle.Enrolled}&enrollmentKind={kind}");
}
