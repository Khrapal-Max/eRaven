//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// IPersonService
//-----------------------------------------------------------------------------

using eRaven.Application.Services.PersonService;
using eRaven.Application.Services.PlanActionService;
using eRaven.Application.ViewModels.PersonViewModels;
using eRaven.Application.ViewModels.PlanViewModels;
using eRaven.Domain.Models;
using Microsoft.AspNetCore.Components;
using System.Linq.Expressions;

namespace eRaven.Components.Pages.Plans.Modals;

public partial class CreatePlanActionModal : ComponentBase
{
    [Inject] public IPlanActionService PlanActionService { get; set; } = default!;
    [Inject] public IPersonService PersonService { get; set; } = default!;

    [Parameter] public Guid PlanId { get; set; }
    [Parameter] public bool ReadOnly { get; set; }
    [Parameter] public EventCallback<PlanActionViewModel> OnCreated { get; set; }
    private DateTime EventLocalBound
    {
        get => _eventLocal;
        set
        {
            _eventLocal = SnapToQuarter(value); // 00/15/30/45
            Validate();                         // оновлюємо стан кнопки
            StateHasChanged();
        }
    }

    private bool _open;
    private bool _busy;
    private bool _canSubmit;

    private CreatePlanActionViewModel _model = new();
    private DateTime _eventLocal = DateTime.UtcNow; // для <input type="datetime-local">
    private string _personQuery = string.Empty;

    private readonly List<PersonSearchViewModel> _personResults = [];
    private PersonSearchViewModel? _selectedPerson;

    public void Open()
    {
        if (ReadOnly) return;

        _model = new() { PlanId = PlanId, EventAtUtc = DateTime.UtcNow };
        _eventLocal = DateTime.UtcNow;
        _personQuery = string.Empty;
        _personResults.Clear();
        _selectedPerson = null;
        _canSubmit = false;

        _open = true;
        StateHasChanged();
    }

    private async Task CreateAsync()
    {
        if (_busy || !_canSubmit) return;
        _busy = true;
        try
        {
            // гарантуємо «квартальні» хвилини і UTC
            _eventLocal = SnapToQuarter(_eventLocal);
            _model.EventAtUtc = DateTime.SpecifyKind(_eventLocal, DateTimeKind.Utc);

            var created = await PlanActionService.CreateAsync(_model);
            await OnCreated.InvokeAsync(created);
            Close();
        }
        finally
        {
            _busy = false;
        }
    }

    private void Close() => _open = false;

    private async Task SearchPersons()
    {
        _personResults.Clear();
        _selectedPerson = null;

        var q = _personQuery?.Trim();
        if (string.IsNullOrWhiteSpace(q))
        {
            Validate();
            return;
        }

        // будуємо предикат для EF
        Expression<Func<Person, bool>> predicate;
        if (q.All(char.IsDigit))
        {
            // тільки цифри → шукаємо по РНОКПП
            predicate = p => p.Rnokpp.Contains(q);
        }
        else
        {
            // простий OR по ключових полях
            predicate = p =>
                p.Rnokpp.Contains(q) ||
                p.LastName.Contains(q) ||
                p.FirstName.Contains(q) ||
                (p.MiddleName != null && p.MiddleName.Contains(q)) ||
                (p.Callsign != null && p.Callsign.Contains(q));
        }

        var people = await PersonService.SearchAsync(predicate);

        var mapped = people
            .OrderBy(p => p.LastName).ThenBy(p => p.FirstName).ThenBy(p => p.MiddleName)
            .Take(20) // трішки більше, все одно є скрол
            .Select(p => new PersonSearchViewModel(
                p.Id,
                p.FullName,
                p.Rnokpp,
                p.Rank,
                p.PositionUnit?.FullName ?? string.Empty,
                p.BZVP,
                p.Weapon,
                p.Callsign
            ));

        _personResults.AddRange(mapped);

        // якщо знайдено рівно 1 — автообрати (щоб кнопка одразу активувалась)
        if (_personResults.Count == 1)
            SelectPerson(_personResults[0], validateAfter: false);

        Validate();
        StateHasChanged();
    }

    private void SelectPerson(PersonSearchViewModel person, bool validateAfter = true)
    {
        _selectedPerson = person;
        _model.PersonId = person.Id;
        if (validateAfter) Validate();
    }

    // ===== валідація локальної форми =====
    private void Validate()
    {
        var hasPerson = _model.PersonId != Guid.Empty;
        var hasLocation = !string.IsNullOrWhiteSpace(_model.Location);
        var hasGroup = !string.IsNullOrWhiteSpace(_model.GroupName);
        var hasCrew = !string.IsNullOrWhiteSpace(_model.CrewName);
        var quarterOk = IsQuarterMinute(_eventLocal);

        _canSubmit = hasPerson && hasLocation && hasGroup && hasCrew && quarterOk && !ReadOnly;
    }

    private void OnTextChanged(ChangeEventArgs _)
    {
        // будь-яка зміна інпутів → перевалідувати
        Validate();
    }

    // ===== helper-и для «квартальних» хвилин =====
    private static bool IsQuarterMinute(DateTime dt)
        => dt.Minute % 15 == 0 && dt.Second == 0 && dt.Millisecond == 0;

    private static DateTime SnapToQuarter(DateTime dt)
    {
        var minute = dt.Minute;
        var q = (minute / 15) * 15; // округлення вниз до кварталу
        return new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, q, 0, dt.Kind);
    }
}