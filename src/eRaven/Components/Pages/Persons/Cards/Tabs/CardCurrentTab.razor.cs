//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CardCurrentTab
//-----------------------------------------------------------------------------

using eRaven.Application.Commands;
using eRaven.Application.Commands.PersonInfo;
using eRaven.Application.DTOs.Person;
using Microsoft.AspNetCore.Components;

namespace eRaven.Components.Pages.Persons.Cards.Tabs;

public partial class CardCurrentTab : ComponentBase
{
    // =========================
    // Parameters
    // =========================

    [Parameter, EditorRequired] public PersonDetailsDto Person { get; set; } = default!;
    [Parameter] public EventCallback OnReload { get; set; }

    // =========================
    // DI
    // =========================

    [Inject] public ICommandHandler<UpdatePersonalInfoCommand> UpdatePersonalInfoCommandHandler { get; set; } = default!;
    [Inject] public ICommandHandler<ChangeRankCommand> ChangeRankCommandHandler { get; set; } = default!;
    [Inject] public ICommandHandler<ChangePositionCommand> ChangePositionCommandHandler { get; set; } = default!;
    [Inject] public ICommandHandler<ChangeBzvpCommand> ChangeBzvpCommandHandler { get; set; } = default!;
    [Inject] public ICommandHandler<ChangeWeaponCommand> ChangeWeaponCommandHandler { get; set; } = default!;
    [Inject] public ICommandHandler<ChangeCallsignCommand> ChangeCallsignCommandHandler { get; set; } = default!;

    // =========================
    // UI state (drawers)
    // =========================

    private bool _personalOpen;
    private bool _rankOpen;
    private bool _positionOpen;
    private bool _bzvpOpen;
    private bool _weaponOpen;
    private bool _callsingOpen;

    // =========================
    // Open actions
    // =========================

    private Task OpenPersonalInfo() { _personalOpen = true; return Task.CompletedTask; }
    private Task OpenRank() { _rankOpen = true; return Task.CompletedTask; }
    private Task OpenPosition() { _positionOpen = true; return Task.CompletedTask; }
    private Task OpenBzvp() { _bzvpOpen = true; return Task.CompletedTask; }
    private Task OpenWeapon() { _weaponOpen = true; return Task.CompletedTask; }
    private Task OpenCallsing() { _callsingOpen = true; return Task.CompletedTask; }

    // =========================
    // Submit handlers
    // =========================

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
        await ReloadParentAsync();
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
        await ReloadParentAsync();
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
        await ReloadParentAsync();
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
        await ReloadParentAsync();
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
        await ReloadParentAsync();
    }

    private async Task HandleCallsingSubmitAsync(ChangeCallsingDto dto)
    {
        var cmd = new ChangeCallsignCommand(
            PersonId: dto.PersonId,
            EffectiveDate: dto.EffectiveDate,
            Callsign: dto.Callsign,
            Author: "system", // TODO: auth user
            NowUtc: DateTime.UtcNow
        );

        await ChangeCallsignCommandHandler.HandleAsync(cmd);
        await ReloadParentAsync();
    }

    private async Task ReloadParentAsync()
    {
        if (OnReload.HasDelegate)
            await OnReload.InvokeAsync();
    }
}
