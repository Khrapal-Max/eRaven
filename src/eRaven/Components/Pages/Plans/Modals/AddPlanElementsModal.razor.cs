// -----------------------------------------------------------------------------
// AddPlanElementsModal (code-behind; проста й передбачувана логіка)
// - Критерії:
//   * Dispatch → у список потрапляють тільки особи зі статусом "В районі" (code=30 або name),
//                і які не порушують чергування/дубль по часу в МЕЖАХ цього плану.
//   * Return   → у список потрапляють тільки особи, які вже мають попередній Dispatch у плані,
//                і вставка не ламає чергування (лінійність по часу для цієї особи).
// - Контекст для Return заповнить сервіс (із останнього Dispatch).
// -----------------------------------------------------------------------------

using eRaven.Application.ViewModels.PlanViewModels;
using eRaven.Domain.Enums;
using eRaven.Domain.Models;
using Microsoft.AspNetCore.Components;

namespace eRaven.Components.Pages.Plans.Modals;

public partial class AddPlanElementsModal : ComponentBase
{
    // -------- Props --------
    [Parameter] public bool Open { get; set; }
    [Parameter] public Guid PlanId { get; set; }
    [Parameter] public IReadOnlyList<PlanRosterViewModel> People { get; set; } = [];
    [Parameter] public IReadOnlyList<PlanElement> ExistingElements { get; set; } = [];
    [Parameter] public EventCallback<IReadOnlyList<CreatePlanElementViewModel>> OnAdd { get; set; }
    [Parameter] public EventCallback OnClose { get; set; }

    // -------- State (step 1) --------
    private WizardStep Step { get; set; } = WizardStep.Attributes;
    private PlanType _type = PlanType.Dispatch;
    private DateOnly _date = DateOnly.FromDateTime(DateTime.Now);
    private int _hour = DateTime.Now.Hour;
    private int _minute = RoundToQuarter(DateTime.Now.Minute);
    private string? _location, _group, _tool, _note;
    private readonly List<string> _attrErrors = [];

    // -------- State (step 2) --------
    private string? _query;
    private readonly HashSet<Guid> _selected = [];
    private List<PlanRosterViewModel> _candidates = [];

    protected override void OnParametersSet()
    {
        if (!Open) return;
        if (Step == WizardStep.People) ApplyPeopleFilter(); // якщо під’їхали нові ExistingElements → одразу оновити
    }

    // ---------- Stepper / attrs ----------
    private static int RoundToQuarter(int m) => m < 15 ? 0 : m < 30 ? 15 : m < 45 ? 30 : 45;

    private bool CanGoNext() => IsAttributesValid(collectErrors: false);

    private bool IsAttributesValid(bool collectErrors)
    {
        if (collectErrors) _attrErrors.Clear();
        bool ok = true;

        if (_date == default) { if (collectErrors) _attrErrors.Add("Оберіть дату."); ok = false; }
        if (_hour is < 0 or > 23) { if (collectErrors) _attrErrors.Add("Невірна година."); ok = false; }
        if (_minute is not (0 or 15 or 30 or 45)) { if (collectErrors) _attrErrors.Add("Хвилини: 00/15/30/45."); ok = false; }
        if (_type == PlanType.Dispatch && string.IsNullOrWhiteSpace(_location)) { if (collectErrors) _attrErrors.Add("Вкажіть локацію для відрядження."); ok = false; }

        return ok;
    }

    private void OnTypeChanged()
    {
        _selected.Clear();
        _query = null;
        _candidates.Clear();
        StateHasChanged();
    }

    private void GoNext()
    {
        if (!IsAttributesValid(true)) return;
        Step = WizardStep.People;
        ApplyPeopleFilter();
    }

    private void GoBack()
    {
        Step = WizardStep.Attributes;
        _selected.Clear();
        _attrErrors.Clear();
    }

    private async Task CloseAndReset()
    {
        await OnClose.InvokeAsync();
        ResetAll();
    }

    private void ResetAll()
    {
        Step = WizardStep.Attributes;
        _type = PlanType.Dispatch;

        var now = DateTime.Now;
        _date = DateOnly.FromDateTime(now);
        _hour = now.Hour;
        _minute = RoundToQuarter(now.Minute);

        _location = _group = _tool = _note = null;
        _query = null; _selected.Clear(); _candidates.Clear();
        _attrErrors.Clear();
    }

    // ---------- Candidates & rules ----------
    private void OnSearchClick() => ApplyPeopleFilter();

    private void ApplyPeopleFilter()
    {
        var whenUtc = BuildEventUtc();
        var s = (_query ?? string.Empty).Trim();

        IEnumerable<PlanRosterViewModel> src = People;

        if (_type == PlanType.Dispatch)
        {
            // Тільки «В районі»
            src = src.Where(IsInArea);
        }
        else // Return
        {
            // Тільки ті, хто вже має попередній Dispatch у плані до whenUtc
            var byPerson = ExistingElements
                .Where(e => e.EventAtUtc < whenUtc && e.Type == PlanType.Dispatch)
                .GroupBy(e => e.PlanParticipantSnapshot.PersonId)
                .ToDictionary(g => g.Key, g => g.OrderBy(x => x.EventAtUtc).ToList());

            src = src.Where(p => byPerson.ContainsKey(p.PersonId));
        }

        if (s.Length > 0)
        {
            static bool Has(string? v, string find) => !string.IsNullOrWhiteSpace(v) && v.Contains(find, StringComparison.OrdinalIgnoreCase);
            src = src.Where(p => Has(p.FullName, s) || Has(p.Rnokpp, s) || Has(p.Rank, s) || Has(p.Position, s));
        }

        // Anti-dup та чергування в межах ПЛАНУ, відносно whenUtc
        _candidates = [.. src
            .Where(p => IsInsertAllowed(p.PersonId, whenUtc, _type))
            .OrderBy(p => p.FullName)];
    }

    private bool IsInArea(PlanRosterViewModel p)
        => string.Equals(p.StatusKindCode, "30", StringComparison.OrdinalIgnoreCase)
           || string.Equals(p.StatusKindName, "В районі", StringComparison.OrdinalIgnoreCase);

    private bool IsInsertAllowed(Guid personId, DateTime whenUtc, PlanType newType)
    {
        var timeline = ExistingElements
            .Where(e => e.PlanParticipantSnapshot.PersonId == personId)
            .OrderBy(e => e.EventAtUtc)
            .ToList();

        // Дублікат моменту?
        if (timeline.Any(e => e.EventAtUtc == whenUtc)) return false;

        var prev = timeline.LastOrDefault(e => e.EventAtUtc < whenUtc);
        var next = timeline.FirstOrDefault(e => e.EventAtUtc > whenUtc);

        if (prev is not null && prev.Type == newType) return false; // не можна два підряд однакових
        if (next is not null && next.Type == newType) return false; // вставка не має ламати чергування

        if (newType == PlanType.Return)
        {
            // Має існувати попередній Dispatch
            if (prev is null || prev.Type != PlanType.Dispatch) return false;
        }

        return true;
    }

    private bool IsDuplicateAtSameMoment(Guid personId)
        => ExistingElements.Any(e => e.PlanParticipantSnapshot.PersonId == personId && e.EventAtUtc == BuildEventUtc());

    private DateTime BuildEventUtc()
    {
        var local = new DateTime(_date.Year, _date.Month, _date.Day, _hour, _minute, 0, DateTimeKind.Local);
        return local.ToUniversalTime();
    }

    private void AddPick(Guid personId) => _selected.Add(personId);
    private void RemovePick(Guid personId) => _selected.Remove(personId);

    // ---------- Submit ----------
    private async Task Submit()
    {
        if (_selected.Count == 0) return;

        var evUtc = BuildEventUtc();
        var items = new List<CreatePlanElementViewModel>(_selected.Count);

        foreach (var id in _selected)
        {
            items.Add(new CreatePlanElementViewModel
            {
                Type = _type,
                EventAtUtc = evUtc,
                Location = _type == PlanType.Dispatch ? T(_location) : null,
                GroupName = _type == PlanType.Dispatch ? T(_group) : null,
                ToolType = _type == PlanType.Dispatch ? T(_tool) : null,
                Note = T(_note),
                PersonId = id
            });
        }

        await OnAdd.InvokeAsync(items.AsReadOnly());
        await CloseAndReset();
    }

    private static string? T(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    private enum WizardStep { Attributes = 1, People = 2 }
}
