//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonsTable
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs;
using eRaven.Domain.Enums;
using Microsoft.AspNetCore.Components;

namespace eRaven.Components.Pages.Persons.Registry;

public partial class PersonsTable
{
    [Parameter] public IReadOnlyList<PersonRowDto> Items { get; set; } = [];
    [Parameter] public PersonRowDto? Selected { get; set; }
    [Parameter] public EventCallback<PersonRowDto?> SelectedChanged { get; set; }

    [Parameter] public EventCallback<PersonRowDto> OnRowClick { get; set; }

    // Чернетка: окремо “відкрити картку”
    [Parameter] public EventCallback<PersonRowDto> OnOpenCard { get; set; }

    private Task OpenCard(PersonRowDto row)
        => OnOpenCard.HasDelegate ? OnOpenCard.InvokeAsync(row)
                                  : (OnRowClick.HasDelegate ? OnRowClick.InvokeAsync(row) : Task.CompletedTask);

    private static RenderFragment LifecycleBadge(PersonLifecycle lc) => builder =>
    {
        var (cls, text) = lc switch
        {
            PersonLifecycle.Candidate => ("badge bg-primary", "Рекрут"),
            PersonLifecycle.Enrolled => ("badge bg-success", "ОС"),
            PersonLifecycle.Excluded => ("badge bg-secondary", "ВИКЛ"),
            _ => ("badge bg-light text-dark", lc.ToString())
        };

        builder.OpenElement(0, "span");
        builder.AddAttribute(1, "class", cls);
        builder.AddContent(2, text);
        builder.CloseElement();
    };
}