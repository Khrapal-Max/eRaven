//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// AddPlanElementsModal
//-----------------------------------------------------------------------------


using eRaven.Application.ViewModels.PlanViewModels;
using eRaven.Domain.Enums;
using eRaven.Domain.Models;
using Microsoft.AspNetCore.Components;

namespace eRaven.Components.Pages.Plans.Modals;

public partial class AddPlanElementsModal : ComponentBase
{
    // -------- API --------
    [Parameter] public bool Open { get; set; }
    [Parameter] public Guid PlanId { get; set; }

    /// <summary>Повний список людей (із поточними статусами/посадами/PLANNING!).</summary>
    /// ВАЖЛИВО: при завантаженні People зробіть Include(p => p.Planning)
    [Parameter] public IReadOnlyList<Person> People { get; set; } = [];

    /// <summary>Елементи цього плану — лише для анти-дубля на той самий момент.</summary>
    [Parameter] public IReadOnlyList<PlanElement> ExistingElements { get; set; } = [];

    /// <summary>Повертаємо до батька заявки (по 1 на кожну обрану особу).</summary>
    [Parameter] public EventCallback<IReadOnlyList<CreatePlanElementViewModel>> OnAdd { get; set; }

    [Parameter] public EventCallback OnClose { get; set; }

    // -------- Local state --------
    private enum Step { Attributes = 1, People = 2 }
    private Step _step = Step.Attributes;

    // Єдиний стан форми (спрощено)
    private CreatePlanElementViewModel _form = new()
    {
        Type = PlanType.Dispatch,
        PersonId = Guid.Empty // не використовується у формі батч-додавання
    };

    // Пікери часу (локальний; 00/15/30/45)
    private DateOnly _date = DateOnly.FromDateTime(DateTime.Now);
    private int _hour = DateTime.Now.Hour;
    private int _minute = RoundToQuarter(DateTime.Now.Minute);

    // Крок 2
    private readonly HashSet<Guid> _selected = [];
    private List<Person> _candidates = [];
    private string? _query;

    private readonly List<string> _errors = [];

    // -------- Lifecycle --------
    protected override void OnParametersSet()
    {
        if (!Open) return;
        if (_step == Step.People) ApplyPeopleFilter(); // якщо People/ExistingElements оновились
    }

    // -------- UI helpers --------
    private static int RoundToQuarter(int m) => m < 15 ? 0 : m < 30 ? 15 : m < 45 ? 30 : 45;

    private static bool IsArea(Person p)
    {
        var code = p.StatusKind?.Code;
        var name = p.StatusKind?.Name;
        return string.Equals(code, "30", StringComparison.OrdinalIgnoreCase)
            || string.Equals(name, "В районі", StringComparison.OrdinalIgnoreCase);
    }

    private bool IsReturn => _form.Type == PlanType.Return;

    private DateTime BuildEventUtc()
    {
        var local = new DateTime(_date.Year, _date.Month, _date.Day, _hour, _minute, 0, DateTimeKind.Local);
        return local.ToUniversalTime();
    }

    // -------- Validations (мінімум — бек робить остаточні гварди) --------
    private bool Validate(bool collect)
    {
        if (collect) _errors.Clear();
        bool ok = true;

        if (_minute is not (0 or 15 or 30 or 45)) { if (collect) _errors.Add("Хвилини мають бути 00/15/30/45."); ok = false; }
        if (_form.Type == PlanType.Dispatch && string.IsNullOrWhiteSpace(_form.Location))
        { if (collect) _errors.Add("Вкажіть локацію для «Відрядити»."); ok = false; }

        return ok;
    }

    // -------- Stepper --------
    private void Next()
    {
        if (!Validate(true)) return;
        _step = Step.People;
        ApplyPeopleFilter();
    }

    private void Back()
    {
        _step = Step.Attributes;
        _selected.Clear();
        _errors.Clear();
    }

    private async Task CloseAndReset()
    {
        await OnClose.InvokeAsync();
        ResetAll();
    }

    private void ResetAll()
    {
        _step = Step.Attributes;
        _form = new() { Type = PlanType.Dispatch };
        var now = DateTime.Now;
        _date = DateOnly.FromDateTime(now);
        _hour = now.Hour;
        _minute = RoundToQuarter(now.Minute);
        _selected.Clear();
        _candidates.Clear();
        _query = null;
        _errors.Clear();
    }

    // -------- Events --------
    // Валідна сигнатура для @onchange
    private void OnTypeChanged(ChangeEventArgs e)
    {
        if (e?.Value is null) return;
        if (int.TryParse(e.Value.ToString(), out var v) && Enum.IsDefined(typeof(PlanType), v))
        {
            _form.Type = (PlanType)v;

            // Для Return — контекст не редагуємо в UI (тягне сервіс)
            if (_form.Type == PlanType.Return) _form.Location = _form.GroupName = _form.ToolType = null;

            if (_step == Step.People) ApplyPeopleFilter();
        }
    }

    // -------- Filtering (PLANNING-aware) --------
    private void ApplyPeopleFilter()
    {
        var tUtc = BuildEventUtc();
        IEnumerable<Person> src = People ?? [];

        var s = (_query ?? string.Empty).Trim();
        if (s.Length > 0)
        {
            static bool Has(string? v, string q) => !string.IsNullOrWhiteSpace(v) && v.Contains(q, StringComparison.OrdinalIgnoreCase);
            src = src.Where(p =>
                Has(p.FullName ?? $"{p.LastName} {p.FirstName} {p.MiddleName}", s) ||
                Has(p.Rnokpp, s) || Has(p.Rank, s) ||
                Has(p.PositionUnit?.FullName ?? p.PositionUnit?.ShortName, s));
        }

        // Ключова частина: дозволені за глобальною останньою дією (Person.Planning)
        src = src.Where(p => IsAllowedByPlanning(p, tUtc)
                             && !IsDuplicateAtThisTime(p.Id, tUtc)); // анти-дубль у межах цього плану

        _candidates = [.. src
            .OrderBy(p => p.LastName)
            .ThenBy(p => p.FirstName)
            .ThenBy(p => p.MiddleName)];
    }

    private bool IsAllowedByPlanning(Person p, DateTime tUtc)
    {
        var lastType = p.PersonPlanning?.LastActionType;
        var lastAt = p.PersonPlanning?.LastActionAtUtc;

        if (_form.Type == PlanType.Dispatch)
        {
            // лише «В районі»
            if (!IsArea(p)) return false;

            // не можна два Dispatch поспіль
            if (lastType == PlanType.Dispatch) return false;

            // час строго після останньої дії (якщо була)
            if (lastAt.HasValue && tUtc <= lastAt.Value) return false;

            return true;
        }
        else // Return
        {
            // повертати можна лише якщо останнє глобально — Dispatch
            if (lastType != PlanType.Dispatch) return false;

            // час строго після того Dispatch
            if (!lastAt.HasValue || tUtc <= lastAt.Value) return false;

            // контекст повернення сервіс підтягне з останнього Dispatch
            return true;
        }
    }

    private bool IsDuplicateAtThisTime(Guid personId, DateTime tUtc)
        => ExistingElements.Any(e => e.PersonId == personId && e.Type == _form.Type && e.EventAtUtc == tUtc);

    private void AddPick(Guid id) => _selected.Add(id);
    private void RemovePick(Guid id) => _selected.Remove(id);

    // -------- Submit --------
    private async Task Submit()
    {
        if (_selected.Count == 0) return;

        var evUtc = BuildEventUtc();

        var payload = _selected.Select(id => new CreatePlanElementViewModel
        {
            Type = _form.Type,
            EventAtUtc = evUtc,
            // Для Dispatch беремо введений контекст, для Return — лишаємо null (сервіс підтягне)
            Location = _form.Type == PlanType.Dispatch ? T(_form.Location) : null,
            GroupName = _form.Type == PlanType.Dispatch ? T(_form.GroupName) : null,
            ToolType = _form.Type == PlanType.Dispatch ? T(_form.ToolType) : null,
            Note = T(_form.Note),
            PersonId = id
        }).ToList().AsReadOnly();

        await OnAdd.InvokeAsync(payload);
        await CloseAndReset();
    }

    // -------- Utils --------
    private static string? T(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
