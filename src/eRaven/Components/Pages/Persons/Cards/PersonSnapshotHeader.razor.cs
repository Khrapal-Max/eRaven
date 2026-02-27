//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonHeader
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.Enums;
using eRaven.Application.DTOs.Person;
using Microsoft.AspNetCore.Components;

namespace eRaven.Components.Pages.Persons.Cards;

public partial class PersonSnapshotHeader : ComponentBase
{
    [Parameter, EditorRequired] public PersonDetailsDto Person { get; set; } = default!;
    [Parameter] public Guid PersonId { get; set; }

    [Inject] public NavigationManager NavigationManager { get; set; } = default!;

    private void Back() => NavigationManager.NavigateTo("/persons");

    private static RenderFragment LifecycleBadge(PersonLifecycleDto lc) => builder =>
    {
        var (cls, text) = lc switch
        {
            PersonLifecycleDto.Reserved => ("badge bg-primary", "Резерв"),
            PersonLifecycleDto.Enrolled => ("badge bg-success", "В табелі"),
            _ => ("badge bg-light text-dark", lc.ToString())
        };

        builder.OpenElement(0, "span");
        builder.AddAttribute(1, "class", cls);
        builder.AddContent(2, text);
        builder.CloseElement();
    };
}
