//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonCard
//-----------------------------------------------------------------------------

using eRaven.Application.Commands;
using eRaven.Application.Commands.PersonInfo;
using eRaven.Application.DTOs;
using eRaven.Application.Queries;
using Microsoft.AspNetCore.Components;

namespace eRaven.Components.Pages.Persons.Card;

public partial class Card
{
    [Parameter] public Guid PersonId { get; set; }

    [Inject] public IQueryHandler<GetPersonDetailsQuery, PersonDetailsDto?> GetPersonCard { get; set; } = default!;

    [Inject] public ICommandHandler<ChangeRankCommand, Guid> ChangeRankCommandHandler { get; set; } = default!;

    private bool _loading;
    private PersonDetailsDto? _person;

    private bool _rankOpen;

    protected override async Task OnParametersSetAsync()
    {
        await LoadAsync();
    }

    private Task OpenRank(PersonDetailsDto dto)
    {
        _rankOpen = true;
        return Task.CompletedTask;
    }

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

    private async Task HandleRankSubmitAsync(ChangeRankDto dto)
    {
        var cmd = new ChangeRankCommand(
           PersonId: dto.PersonId,
           EffectiveDate: dto.EffectiveDate,
           Rank: dto.Rank,
           Note: dto.Note,
           Author: "system", // TODO: auth user
           NowUtc: DateTime.UtcNow
        );

        await ChangeRankCommandHandler.HandleAsync(cmd);
        await LoadAsync();
    }

    private enum CardTab { Current, Career, TimeSheet }

    private CardTab Tab { get; set; } = CardTab.Current;

    private static readonly (CardTab Key, string Label)[] Tabs =
    [
        (CardTab.Current, "Поточний стан"),
        (CardTab.Career,  "Кар’єра"),
        (CardTab.TimeSheet,   "Табель"),
    ];

    private void SetTab(CardTab tab) => Tab = tab;

    // ✅ сірі “зливаються” → даємо контрастний фон + hover
    private string TabClass(CardTab tab) =>
        "nav-link rounded-0 " +
        (Tab == tab
            ? "active fw-semibold text-success bg-white border-primary-subtle"
            : "text-body bg-body-tertiary border-0 border-bottom border-primary-subtle");
}