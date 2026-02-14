//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonCard (Shell)
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.Person;
using eRaven.Application.Queries;
using eRaven.Application.Queries.Personal;
using Microsoft.AspNetCore.Components;

namespace eRaven.Components.Pages.Persons.Cards;

public partial class Card : ComponentBase
{
    // =========================
    // Parameters
    // =========================
    [Parameter] public Guid PersonId { get; set; }

    // =========================
    // DI
    // =========================
    [Inject] public IQueryHandler<GetPersonDetailsQuery, PersonDetailsDto?> GetPersonCard { get; set; } = default!;

    // =========================
    // UI State
    // =========================
    private bool _loading;
    private PersonDetailsDto? _person;

    private enum CardTab { Current, Career, TimeSheet }
    private CardTab Tab { get; set; } = CardTab.Current;

    // =========================
    // Tabs (metadata)
    // =========================
    private readonly record struct TabItem(CardTab Key, string Label);

    private static readonly TabItem[] Tabs =
    [
        new(CardTab.Current,  "Поточний стан"),
        new(CardTab.Career,   "Кар’єра"),
        new(CardTab.TimeSheet,"Табель"),
    ];

    // =========================
    // Lifecycle
    // =========================
    protected override async Task OnParametersSetAsync()
        => await LoadAsync();

    // =========================
    // Actions
    // =========================
    private void SetTab(CardTab tab)
    {
        if (Tab == tab) return;
        Tab = tab;
    }

    private string TabClass(CardTab tab) =>
        "nav-link rounded-0 " +
        (Tab == tab
            ? "active fw-semibold text-success bg-white border-primary-subtle"
            : "text-body bg-body-tertiary border-0 border-bottom border-primary-subtle");

    // =========================
    // Internals
    // =========================
    private async Task LoadAsync()
    {
        _loading = true;
        try
        {
            _person = await GetPersonCard.HandleAsync(new GetPersonDetailsQuery(PersonId));
        }
        finally
        {
            _loading = false;
        }
    }
}
