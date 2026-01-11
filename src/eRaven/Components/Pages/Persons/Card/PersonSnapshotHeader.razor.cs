//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonHeader
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs;
using eRaven.Domain.Enums;
using Microsoft.AspNetCore.Components;

namespace eRaven.Components.Pages.Persons.Card;

public partial class PersonSnapshotHeader
{
    [Parameter, EditorRequired] public PersonDto Person { get; set; } = default!;
    [Parameter] public Guid PersonId { get; set; }

    [Inject] public NavigationManager NavigationManager { get; set; } = default!;

    private void Back() => NavigationManager.NavigateTo("/persons");

    private static RenderFragment LifecycleBadge(PersonLifecycle lc) => builder =>
    {
        var (cls, text) = lc switch
        {
            PersonLifecycle.Candidate => ("badge bg-primary", "Рекрут"),
            PersonLifecycle.Enrolled => ("badge bg-success", "В СПИСКАХ"),
            PersonLifecycle.Excluded => ("badge bg-secondary", "ВИКЛ"),
            _ => ("badge bg-light text-dark", lc.ToString())
        };

        builder.OpenElement(0, "span");
        builder.AddAttribute(1, "class", cls);
        builder.AddContent(2, text);
        builder.CloseElement();
    };
}
