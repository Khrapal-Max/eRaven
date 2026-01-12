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
    [Parameter] public IReadOnlyList<PersonListItemDto> Items { get; set; } = [];
    [Parameter] public PersonListItemDto? Selected { get; set; }
    [Parameter] public EventCallback<PersonListItemDto?> SelectedChanged { get; set; }

    [Parameter] public EventCallback<PersonListItemDto> OnRowClick { get; set; }

    [Parameter] public EventCallback<PersonListItemDto> OnOpenCard { get; set; }

    // NEW
    [Parameter] public EventCallback<PersonListItemDto> OnEnroll { get; set; }
    [Parameter] public EventCallback<PersonListItemDto> OnExclude { get; set; }

    private Task OpenCard(PersonListItemDto row)
        => OnOpenCard.HasDelegate ? OnOpenCard.InvokeAsync(row)
                                  : (OnRowClick.HasDelegate ? OnRowClick.InvokeAsync(row) : Task.CompletedTask);
    private Task Enroll(PersonListItemDto row)
      => OnEnroll.HasDelegate ? OnEnroll.InvokeAsync(row) : Task.CompletedTask;

    private Task Exclude(PersonListItemDto row)
        => OnExclude.HasDelegate ? OnExclude.InvokeAsync(row) : Task.CompletedTask;

    private static RenderFragment LifecycleBadge(PersonLifecycle lc, DateOnly? excludedAt) => builder =>
    {
        var (cls, text) = lc switch
        {
            PersonLifecycle.Enrolled => ("badge bg-success", "В ТАБЕЛІ"),
            PersonLifecycle.Reserved => excludedAt is null
                ? ("badge bg-primary", "РЕЗЕРВ")
                : ("badge bg-secondary", "РЕЗЕРВ (ВИКЛ)"),
            _ => ("badge bg-light text-dark", lc.ToString())
        };

        builder.OpenElement(0, "span");
        builder.AddAttribute(1, "class", cls);
        builder.AddContent(2, text);
        builder.CloseElement();
    };

    private static RenderFragment EnrollmentKindBadge(EnrollmentKind? kind) => builder =>
    {
        // null => тире
        if (kind is null)
        {
            builder.OpenElement(0, "span");
            builder.AddAttribute(1, "class", "text-muted");
            builder.AddContent(2, "—");
            builder.CloseElement();
            return;
        }

        // mapping
        // Unit = "Штат"
        // AttachedByOrder = "БР"
        // AttachedByList = "Наказ"
        var (text, cls) = kind.Value switch
        {
            EnrollmentKind.Unit => ("Штат", "badge text-bg-success"),
            EnrollmentKind.AttachedByOrder => ("БР", "badge text-bg-warning"),
            EnrollmentKind.AttachedByList => ("Наказ", "badge text-bg-success"),
            _ => (kind.Value.ToString(), "badge text-bg-secondary")
        };

        builder.OpenElement(0, "span");
        builder.AddAttribute(1, "class", cls);
        builder.AddContent(2, text);
        builder.CloseElement();
    };
}
