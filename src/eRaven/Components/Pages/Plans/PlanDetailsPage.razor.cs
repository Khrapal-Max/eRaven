// -----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
// -----------------------------------------------------------------------------
// PlanDetailsPage (code-behind; точкові операції через сервіс)
// -----------------------------------------------------------------------------

using Blazored.Toast.Services;
using eRaven.Application.Services.PlanService;
using eRaven.Application.ViewModels.PlanViewModels;
using eRaven.Components.Shared.ConfirmModal;
using eRaven.Domain.Enums;
using eRaven.Domain.Models;
using Microsoft.AspNetCore.Components;
using static eRaven.Components.Pages.Plans.Modals.AddPlanElementsModal;

namespace eRaven.Components.Pages.Plans;

public sealed partial class PlanDetailsPage : ComponentBase
{
    [Parameter] public Guid Id { get; set; }

    [Inject] private IPlanService PlanService { get; set; } = default!;
    [Inject] private IToastService Toast { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;

    private Plan? _plan;
    private readonly List<PlanRow> _rows = [];

    private bool _busy;
    private bool Editable => _plan?.State == PlanState.Open;

    // Modal data
    private bool _modalOpen;
    private bool _pickerBusy;
    private string? _pickerError;
    private string? _pickerStatus;
    private PlanRosterResponse _roster = new([]);
    private IReadOnlyList<PersonPlanInfo> _filtered = [];
    private string? _query;
    private const int MaxItems = 200;

    private ConfirmModal _confirm = default!;

    // ---------------- Lifecycle ----------------

    protected override async Task OnParametersSetAsync() => await ReloadAsync();

    private async Task ReloadAsync()
    {
        try
        {
            _busy = true;

            _plan = await PlanService.GetByIdAsync(Id);
            if (_plan is null)
            {
                Toast.ShowError("План не знайдено.");
                Nav.NavigateTo("/plans");
                return;
            }

            BuildRows(_plan);
        }
        catch (Exception ex)
        {
            Toast.ShowError("Не вдалося завантажити план. " + ex.Message);
        }
        finally
        {
            _busy = false;
            StateHasChanged();
        }
    }

    private void BuildRows(Plan p)
    {
        _rows.Clear();
        var ordered = (p.PlanElements ?? []).OrderBy(e => e.EventAtUtc);
        foreach (var e in ordered)
            foreach (var s in e.Participants)
                _rows.Add(new PlanRow(e, s));
    }

    private void GoBack() => Nav.NavigateTo("/plans");

    // ---------------- Modal open/close & roster ----------------

    private async Task OpenModalAsync()
    {
        _pickerError = null;
        _pickerStatus = null;
        _modalOpen = true;

        try
        {
            _pickerBusy = true;
            _roster = await PlanService.GetPlanRosterAsync(Id);
            ApplyFilter(); // стартовий зріз
            _pickerStatus = $"Завантажено осіб: {_roster.People.Count}.";
        }
        catch (Exception ex)
        {
            _pickerError = ex.Message;
        }
        finally
        {
            _pickerBusy = false;
            StateHasChanged();
        }
    }

    private Task CloseModalAsync() { _modalOpen = false; return Task.CompletedTask; }

    private void OnModalFilterChanged(string? q)
    {
        _query = q;
        ApplyFilter();
        StateHasChanged();
    }

    private void ApplyFilter()
    {
        var s = (_query ?? string.Empty).Trim();
        IEnumerable<PersonPlanInfo> src = _roster.People;

        if (string.IsNullOrEmpty(s))
        {
            _filtered = [.. src.Take(MaxItems)];
            return;
        }

        bool Has(string? v) => !string.IsNullOrWhiteSpace(v) &&
                               v.Contains(s, StringComparison.OrdinalIgnoreCase);

        _filtered = [.. src.Where(p =>
                Has(p.FullName) || Has(p.Rnokpp) || Has(p.Rank) || Has(p.Position) ||
                Has(p.Weapon) || Has(p.Callsign) || Has(p.StatusKindName) || Has(p.StatusKindCode)
            )
            .Take(MaxItems)];
    }

    // ---------------- Add via service ----------------

    private async Task OnModalAddAsync(AddElementsPayload payload)
    {
        try
        {
            _pickerBusy = true;

            var req = new AddElementsRequest
            {
                PlanId = Id,
                Type = payload.Type,
                EventAtUtc = LocalToQuarterUtc(payload.LocalDate, payload.Hour, payload.Minute),
                Location = payload.Location,
                GroupName = payload.Group,
                ToolType = payload.Tool,
                Note = payload.Note,
                PersonIds = payload.PersonIds
            };

            var res = await PlanService.AddElementsAsync(req);

            // Після успіху — закриваємо модал і оновлюємо план та ростер
            _modalOpen = false;
            Toast.ShowSuccess($"Додано елемент: {res.AddedCount} учасник(ів).");

            await ReloadAsync();
        }
        catch (Exception ex)
        {
            _pickerError = ex.Message;
        }
        finally
        {
            _pickerBusy = false;
        }
    }

    // ---------------- Remove via service ----------------

    private async Task RemoveParticipantAsync(Guid elementId, Guid personId)
    {
        var ok = await _confirm.ShowConfirmAsync("Прибрати цю особу з елемента?");
        if (!ok) return;

        try
        {
            _busy = true;
            var res = await PlanService.RemoveParticipantAsync(new RemoveParticipantRequest(Id, elementId, personId));
            if (res.Removed)
            {
                Toast.ShowSuccess(res.ElementDeleted
                    ? "Особа прибрана. Елемент спорожнів та видалений."
                    : "Особа прибрана з елемента.");
                await ReloadAsync();
            }
            else
            {
                Toast.ShowWarning("Нічого не змінено.");
            }
        }
        catch (Exception ex)
        {
            Toast.ShowError("Не вдалося видалити. " + ex.Message);
        }
        finally
        {
            _busy = false;
        }
    }

    // ---------------- Helpers ----------------

    private static DateTime LocalToQuarterUtc(DateOnly d, int hour, int minute)
    {
        var mm = minute < 15 ? 0 : minute < 30 ? 15 : minute < 45 ? 30 : 45;
        var local = new DateTime(d.Year, d.Month, d.Day, hour, mm, 0, DateTimeKind.Local);
        return local.ToUniversalTime();
    }

    private record PlanRow(PlanElement E, PlanParticipantSnapshot P);
}
