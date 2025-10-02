/*// -----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
// -----------------------------------------------------------------------------
// Reports → PlanningOnDatePage (code-behind)
// Логіка:
//  • обираємо дату → “Побудувати” → тягнемо активні Dispatch на дату
//  • на екрані: групування Location → Group → Crew, блоковий вивід
//  • експорт: лінійний — рядок шапки блоку (Location/Group/Crew) + під ним рядки людей
// -----------------------------------------------------------------------------

using Blazored.Toast.Services;
using Microsoft.AspNetCore.Components;

namespace eRaven.Components.Pages.Reports;

public partial class PlanningOnDate : ComponentBase, IDisposable
{
    // ============================ DI ============================
    [Inject] private IPlanActionService PlanActionService { get; set; } = default!;
    [Inject] private IToastService Toast { get; set; } = default!;

    private readonly CancellationTokenSource _cts = new();

    // =========================== State ==========================
    protected bool Busy { get; private set; }

    /// <summary>Дата у локалі; інтерпретуємо як 00:00 UTC цього календарного дня.</summary>
    protected DateTime DateLocal { get; set; } = DateTime.Today;

    // Дані для відображення (блокова структура)
    protected List<LocationGroupViewModel> Groups { get; } = [];

    // Дані для експорту (лінійно: шапка блоку → рядки людей)
    protected List<PlanningExportLine> ExportLines { get; } = [];

    // ========================= Lifecycle ========================
    protected override Task OnInitializedAsync()
    {
        // Будуємо лише по натисканню “Побудувати”
        return Task.CompletedTask;
    }

    // ============================ Load ===========================
    protected async Task ReloadAsync()
    {
        try
        {
            SetBusy(true);
            Groups.Clear();
            ExportLines.Clear();

            var (fromUtc, toUtc) = DayUtcRange(DateLocal);

            // Беремо лише Dispatch (Відрядження), стани PlanAction/ApprovedOrder,
            // та лише тих, хто ще не повернувся до цієї дати (логіка — у сервісі).
            var actions = await PlanActionService.GetActiveDispatchOnDateAsync(
                fromUtc, _cts.Token);

            if (actions.Count == 0) return;

            // ===== ГРУПУВАННЯ ДЛЯ ВІДОБРАЖЕННЯ =====
            var byLocation = actions
                .GroupBy(a => a.Location?.Trim() ?? string.Empty,
                         StringComparer.OrdinalIgnoreCase)
                .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase);

            foreach (var loc in byLocation)
            {
                var locVm = new LocationGroupViewModel { Location = loc.Key };

                var byGroup = loc
                    .GroupBy(a => a.GroupName?.Trim() ?? string.Empty,
                             StringComparer.OrdinalIgnoreCase)
                    .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase);

                foreach (var grp in byGroup)
                {
                    var grpVm = new GroupViewModel { GroupName = grp.Key };

                    var byCrew = grp
                        .GroupBy(a => a.CrewName?.Trim() ?? string.Empty,
                                 StringComparer.OrdinalIgnoreCase)
                        .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase);

                    foreach (var crew in byCrew)
                    {
                        var crewVm = new CrewGroupViewModel { CrewName = crew.Key };

                        foreach (var a in crew.OrderBy(x => x.FullName, StringComparer.OrdinalIgnoreCase))
                        {
                            crewVm.Rows.Add(new PlanniingOnDateRowViewModel
                            {
                                RankName = NullIfEmpty(a.RankName),
                                FullName = NullIfEmpty(a.FullName),
                                Callsign = NullIfEmpty(a.Callsign),
                                PlanActionName = NullIfEmpty(a.PlanActionName),
                                Order = NullIfEmpty(a.Order),
                                EffectiveAtUtc = a.EffectiveAtUtc,
                                Note = NullIfEmpty(a.Note)
                            });
                        }

                        grpVm.Crews.Add(crewVm);
                    }

                    locVm.Groups.Add(grpVm);
                }

                Groups.Add(locVm);
            }

            // ===== ЛІНІЙНИЙ ЕКСПОРТ (шапка блоку → рядки людей) =====
            foreach (var loc in Groups)
            {
                foreach (var grp in loc.Groups)
                {
                    foreach (var crew in grp.Crews)
                    {
                        // Рядок-шапка блоку: тільки перші три колонки
                        ExportLines.Add(new PlanningExportLine
                        {
                            Location = loc.Location,
                            GroupName = grp.GroupName,
                            CrewName = crew.CrewName
                        });

                        // Рядки людей
                        foreach (var r in crew.Rows)
                        {
                            ExportLines.Add(new PlanningExportLine
                            {
                                // перші три пусті — для “драбинки” в Excel
                                Location = null,
                                GroupName = null,
                                CrewName = null,

                                RankName = r.RankName,
                                FullName = r.FullName,
                                Callsign = r.Callsign,
                                PlanActionName = r.PlanActionName,
                                Order = r.Order,
                                EffectiveAtUtc = r.EffectiveAtUtc,
                                Note = r.Note
                            });
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Toast.ShowError($"Не вдалося побудувати звіт: {ex.Message}");
        }
        finally
        {
            SetBusy(false);
        }
    }

    // =========================== Utils ===========================
    private static (DateTime fromUtc, DateTime toUtc) DayUtcRange(DateTime localDate)
    {
        var d = localDate.Date;
        var from = new DateTime(d.Year, d.Month, d.Day, 0, 0, 0, DateTimeKind.Utc);
        var to = from.AddDays(1);
        return (from, to);
    }

    private static string? NullIfEmpty(string? s)
        => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    private void OnExportBusyChanged(bool exporting) => SetBusy(exporting || Busy);

    private void SetBusy(bool v)
    {
        Busy = v;
        StateHasChanged();
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
        GC.SuppressFinalize(this);
    }
}*/