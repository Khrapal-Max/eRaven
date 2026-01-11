//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonSnapshotPanel
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs;
using eRaven.Domain.Enums;
using Microsoft.AspNetCore.Components;

namespace eRaven.Components.Pages.Persons.Card;

public partial class PersonSnapshotPanel
{
    [Parameter, EditorRequired] public PersonDto Person { get; set; } = default!;

    private static RenderFragment LifecycleText(PersonLifecycle lc) => builder =>
    {
        var text = lc switch
        {
            PersonLifecycle.Candidate => "Кандидат",
            PersonLifecycle.Enrolled => "В списках",
            PersonLifecycle.Excluded => "Виключений зі списків",
            _ => lc.ToString()
        };

        builder.AddContent(0, text);
    };
}
