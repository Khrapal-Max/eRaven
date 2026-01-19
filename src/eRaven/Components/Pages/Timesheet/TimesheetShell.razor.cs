//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetShell
//-----------------------------------------------------------------------------

using eRaven.Application.Catalogs.Timesheet;
using eRaven.Domain.Enums;
using Microsoft.AspNetCore.Components;

namespace eRaven.Components.Pages.Timesheet;

public partial class TimesheetShell
{
    [Inject] public ITimesheetStatusCatalog Catalog { get; set; } = default!;

    private TimesheetLane _lane = TimesheetLane.Main;
    private IReadOnlyList<TimesheetStatusOption> _options = [];

    protected override void OnInitialized()
        => _options = Catalog.GetByLane(_lane);

    private void SetLane(TimesheetLane lane)
    {
        _lane = lane;
        _options = Catalog.GetByLane(_lane);
    }

    private void OnPickStatus(TimesheetStatusOption option)
    {
        // MVP: поки без дровера. Наступним кроком тут відкриємо CreateEntryDrawer
        // і якщо option.IsCodeTemplate == true — попросимо ввести “номер/код”.
    }
}
