//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonSnapshotPanel
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.Enums;
using eRaven.Application.DTOs.Person;
using Microsoft.AspNetCore.Components;

namespace eRaven.Components.Pages.Persons.Cards;

public partial class PersonSnapshotPanel : ComponentBase
{
    [Parameter, EditorRequired] public PersonDetailsDto Person { get; set; } = default!;

    private static RenderFragment LifecycleText(PersonLifecycleDto lc) => builder =>
    {
        var text = lc switch
        {
            PersonLifecycleDto.Reserved => "резерв",
            PersonLifecycleDto.Enrolled => "В табелі",
            _ => lc.ToString()
        };

        builder.AddContent(0, text);
    };
}
