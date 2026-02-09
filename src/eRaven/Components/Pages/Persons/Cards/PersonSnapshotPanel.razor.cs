//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonSnapshotPanel
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.Person;
using eRaven.Domain.Enums;
using Microsoft.AspNetCore.Components;

namespace eRaven.Components.Pages.Persons.Cards;

public partial class PersonSnapshotPanel : ComponentBase
{
    [Parameter, EditorRequired] public PersonDetailsDto Person { get; set; } = default!;

    private static RenderFragment LifecycleText(PersonLifecycle lc) => builder =>
    {
        var text = lc switch
        {
            PersonLifecycle.Reserved => "резерв",
            PersonLifecycle.Enrolled => "В табелі",
            _ => lc.ToString()
        };

        builder.AddContent(0, text);
    };
}