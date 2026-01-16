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
using eRaven.Application.Queries.Personal;
using Microsoft.AspNetCore.Components;

namespace eRaven.Components.Pages.Persons.Card;

public partial class Card
{
    [Parameter] public Guid PersonId { get; set; }

    [Inject] public IQueryHandler<GetPersonDetailsQuery, PersonDetailsDto?> GetPersonCard { get; set; } = default!;

    [Inject] public ICommandHandler<UpdatePersonalInfoCommand> UpdatePersonalInfoCommandHandler { get; set; } = default!;
    [Inject] public ICommandHandler<ChangeRankCommand> ChangeRankCommandHandler { get; set; } = default!;
    [Inject] public ICommandHandler<ChangePositionCommand> ChangePositionCommandHandler { get; set; } = default!;
    [Inject] public ICommandHandler<ChangeBzvpCommand> ChangeBzvpCommandHandler { get; set; } = default!;
    [Inject] public ICommandHandler<ChangeWeaponCommand> ChangeWeaponCommandHandler { get; set; } = default!;

    private bool _loading;
    private PersonDetailsDto? _person;

    private bool _personalOpen;
    private bool _rankOpen;
    private bool _positionOpen;
    private bool _bzvpOpen;
    private bool _weaponOpen;

    protected override async Task OnParametersSetAsync()
    {
        await LoadAsync();
    }

    private Task OpenPersonalInfo()
    {
        _personalOpen = true;
        return Task.CompletedTask;
    }

    private Task OpenRank()
    {
        _rankOpen = true;
        return Task.CompletedTask;
    }

    private Task OpenPosition()
    {
        _positionOpen = true;
        return Task.CompletedTask;
    }

    private Task OpenBzvp()
    {
        _bzvpOpen = true;
        return Task.CompletedTask;
    }

    private Task OpenWeapon()
    {
        _weaponOpen = true;
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

    private async Task HandlePersonalInfoSubmitAsync(UpdatePersonalInfoDto dto)
    {
        var cmd = new UpdatePersonalInfoCommand(
           PersonId: dto.PersonId,
           Rnokpp: dto.Rnokpp,
           LastName: dto.LastName,
           FirstName: dto.FirstName,
           MiddleName: dto.MiddleName,
           Note: dto.Note,
           Author: "system", // TODO: auth user
           NowUtc: DateTime.UtcNow
        );

        await UpdatePersonalInfoCommandHandler.HandleAsync(cmd);
        await LoadAsync();
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

    private async Task HandlePositionSubmitAsync(ChangePositionDto dto)
    {
        var cmd = new ChangePositionCommand(
           PersonId: dto.PersonId,
           EffectiveDate: dto.EffectiveDate,
           PositionSort: dto.PositionSort,
           Position: dto.Position,
           Note: dto.Note,
           Author: "system", // TODO: auth user
           NowUtc: DateTime.UtcNow
        );

        await ChangePositionCommandHandler.HandleAsync(cmd);
        await LoadAsync();
    }

    private async Task HandleBzvpSubmitAsync(ChangeBzvpDto dto)
    {
        var cmd = new ChangeBzvpCommand(
           PersonId: dto.PersonId,
           EffectiveDate: dto.EffectiveDate,
           Bzvp: dto.Bzvp,
           Note: dto.Note,
           Author: "system", // TODO: auth user
           NowUtc: DateTime.UtcNow
        );

        await ChangeBzvpCommandHandler.HandleAsync(cmd);
        await LoadAsync();
    }

    private async Task HandleWeaponSubmitAsync(ChangeWeaponDto dto)
    {
        var cmd = new ChangeWeaponCommand(
           PersonId: dto.PersonId,
           EffectiveDate: dto.EffectiveDate,
           Weapon: dto.Weapon,
           Author: "system", // TODO: auth user
           NowUtc: DateTime.UtcNow
        );

        await ChangeWeaponCommandHandler.HandleAsync(cmd);
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