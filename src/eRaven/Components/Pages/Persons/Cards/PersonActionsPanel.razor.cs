//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonSnapshotPanel
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.Person;
using Microsoft.AspNetCore.Components;

namespace eRaven.Components.Pages.Persons.Cards;

public partial class PersonActionsPanel : ComponentBase
{
    [Parameter, EditorRequired] public PersonDetailsDto Person { get; set; } = default!;
    [Parameter] public EventCallback OnOpenPersonal { get; set; }
    [Parameter] public EventCallback OnOpenRank { get; set; }
    [Parameter] public EventCallback OnOpenPosition { get; set; }
    [Parameter] public EventCallback OnOpenBZVP { get; set; }
    [Parameter] public EventCallback OnOpenWeapon { get; set; }
    [Parameter] public EventCallback OnOpenCallsing { get; set; }

    private Task OpenPersonal()
       => OnOpenPersonal.HasDelegate ? OnOpenPersonal.InvokeAsync() : Task.CompletedTask;

    private Task OpenRank()
        => OnOpenRank.HasDelegate ? OnOpenRank.InvokeAsync() : Task.CompletedTask;

    private Task OpenPosition()
       => OnOpenPosition.HasDelegate ? OnOpenPosition.InvokeAsync() : Task.CompletedTask;

    private Task OpenBZVP()
       => OnOpenBZVP.HasDelegate ? OnOpenBZVP.InvokeAsync() : Task.CompletedTask;

    private Task OpenWeapon()
       => OnOpenWeapon.HasDelegate ? OnOpenWeapon.InvokeAsync() : Task.CompletedTask;

    private Task OpenCallsing()
       => OnOpenCallsing.HasDelegate ? OnOpenCallsing.InvokeAsync() : Task.CompletedTask;
}
