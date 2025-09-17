// -----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
// -----------------------------------------------------------------------------
// CreatePlanActionModal — code-behind
// -----------------------------------------------------------------------------

using eRaven.Application.Services.PersonService;
using eRaven.Application.Services.PlanActionService;
using eRaven.Application.ViewModels.PersonViewModels;
using eRaven.Application.ViewModels.PlanViewModels;
using eRaven.Domain.Enums;
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

    private bool _open;
    private bool _busy;

    private CreatePlanActionViewModel _model = new();
    private DateTime _eventLocal = DateTime.UtcNow; // інтерпретуємо як UTC-значення для зручності
    private string _personQuery = string.Empty;

    private readonly List<PersonSearchViewModel> _personResults = [];
    private PersonSearchViewModel? _selectedPerson;

    // --------- бінди верхніх полів ---------
    private DateTime EventLocalBound
    {
        get => _eventLocal;
        set
        {
            _eventLocal = value;                                               // step=900 обмежує 00/15/30/45
            _model.EventAtUtc = DateTime.SpecifyKind(value, DateTimeKind.Utc); // для валідатора (Kind=Utc)
        }
    }

    public void Open()
    {
        if (ReadOnly) return;

        // починаємо з «квартального» часу, щоб інпут не був «між» значеннями
        _eventLocal = SnapToQuarter(DateTime.UtcNow);
        _model = new()
        {
            PlanId = PlanId,
            ActionType = PlanActionType.Dispatch,
            EventAtUtc = DateTime.SpecifyKind(_eventLocal, DateTimeKind.Utc)
        };

        _personQuery = string.Empty;
        _personResults.Clear();
        _selectedPerson = null;

        _open = true;
        StateHasChanged();
    }

    private async Task CreateAsync()
    {
        if (_busy) return;
        _busy = true;
        try
        {
            // EventAtUtc вже в Kind=Utc з сеттера EventLocalBound
            var created = await PlanActionService.CreateAsync(_model);
            await OnCreated.InvokeAsync(created);
            Close();
        }
        finally { _busy = false; }
    }

    private void Close() => _open = false;

    // --------- пошук осіб і вибір ---------
    private async Task SearchPersons()
    {
        _personResults.Clear();
        _selectedPerson = null;

        var q = _personQuery?.Trim();
        if (string.IsNullOrWhiteSpace(q))
            return;

        Expression<Func<Person, bool>> predicate = q.All(char.IsDigit)
            ? p => p.Rnokpp.Contains(q)
            : p => p.Rnokpp.Contains(q)
                || p.LastName.Contains(q)
                || p.FirstName.Contains(q)
                || (p.MiddleName != null && p.MiddleName.Contains(q))
                || (p.Callsign != null && p.Callsign.Contains(q));

        var people = await PersonService.SearchAsync(predicate);

        _personResults.AddRange(
            people
                .OrderBy(p => p.LastName).ThenBy(p => p.FirstName).ThenBy(p => p.MiddleName)
                .Take(50)
                .Select(p => new PersonSearchViewModel(
                    p.Id,
                    p.FullName,
                    p.Rnokpp,
                    p.Rank,
                    p.PositionUnit?.FullName ?? string.Empty,
                    p.BZVP,
                    p.Weapon,
                    p.Callsign
                ))
        );
    }

    private Task OnSelectedPersonChanged(PersonSearchViewModel? p)
    {
        // 2-way binding від TableBaseComponent (актуалізує підсумок ліворуч)
        _selectedPerson = p;
        _model.PersonId = p?.Id ?? Guid.Empty;
        return Task.CompletedTask;
    }

    private void OnPersonRowClick(PersonSearchViewModel p)
    {
        // клік по рядку теж обирає персону (ідеально для мобільних)
        _selectedPerson = p;
        _model.PersonId = p.Id;
    }

    // --------- утиліти ---------
    private static DateTime SnapToQuarter(DateTime dtUtc)
    {
        var m = (dtUtc.Minute / 15) * 15;
        return new DateTime(dtUtc.Year, dtUtc.Month, dtUtc.Day, dtUtc.Hour, m, 0, DateTimeKind.Utc);
    }
}
