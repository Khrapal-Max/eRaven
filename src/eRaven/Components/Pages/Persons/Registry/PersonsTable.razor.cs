//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonsTable
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.Person;
using eRaven.Domain.Enums;
using Microsoft.AspNetCore.Components;

namespace eRaven.Components.Pages.Persons.Registry;

public partial class PersonsTable
{
    // =========================
    // Parameters
    // =========================

    [Parameter] public IReadOnlyList<PersonListItemDto> Items { get; set; } = [];

    [Parameter] public PersonListItemDto? Selected { get; set; }
    [Parameter] public EventCallback<PersonListItemDto?> SelectedChanged { get; set; }

    [Parameter] public EventCallback<PersonListItemDto> OnRowClick { get; set; }
    [Parameter] public EventCallback<PersonListItemDto> OnOpenCard { get; set; }
    [Parameter] public EventCallback<PersonListItemDto> OnEnroll { get; set; }
    [Parameter] public EventCallback<PersonListItemDto> OnExclude { get; set; }

    // =========================
    // Row actions
    // =========================

    private Task OpenCard(PersonListItemDto row)
        => OnOpenCard.HasDelegate
            ? OnOpenCard.InvokeAsync(row)
            : InvokeOrCompleted(OnRowClick, row);

    private Task Enroll(PersonListItemDto row)
        => InvokeOrCompleted(OnEnroll, row);

    private Task Exclude(PersonListItemDto row)
        => InvokeOrCompleted(OnExclude, row);

    private static Task InvokeOrCompleted(EventCallback<PersonListItemDto> cb, PersonListItemDto arg)
        => cb.HasDelegate ? cb.InvokeAsync(arg) : Task.CompletedTask;

    // =========================
    // Badges
    // =========================

    private static RenderFragment LifecycleBadge(PersonLifecycle lifecycle, DateOnly? excludedAt) => builder =>
    {
        var (cls, text) = lifecycle switch
        {
            PersonLifecycle.Enrolled => ("badge bg-success", "В ТАБЕЛІ"),

            PersonLifecycle.Reserved when excludedAt is null
                => ("badge bg-primary", "РЕЗЕРВ"),

            PersonLifecycle.Reserved
                => ("badge bg-secondary", "РЕЗЕРВ (ВИКЛ)"),

            _ => ("badge text-bg-light", lifecycle.ToString())
        };

        builder.OpenElement(0, "span");
        builder.AddAttribute(1, "class", cls);
        builder.AddContent(2, text);
        builder.CloseElement();
    };

    private static RenderFragment EnrollmentKindBadge(EnrollmentKind? kind) => builder =>
    {
        if (kind is null)
        {
            builder.OpenElement(0, "span");
            builder.AddAttribute(1, "class", "text-muted");
            builder.AddContent(2, "—");
            builder.CloseElement();
            return;
        }

        var (text, cls) = kind.Value switch
        {
            EnrollmentKind.Unit => ("Штат", "badge bg-success"),
            EnrollmentKind.AttachedByOrder => ("БР", "badge bg-warning text-dark"),
            EnrollmentKind.AttachedByList => ("Наказ", "badge text-bg-info"),
            _ => (kind.Value.ToString(), "badge text-bg-secondary")
        };

        builder.OpenElement(0, "span");
        builder.AddAttribute(1, "class", cls);
        builder.AddContent(2, text);
        builder.CloseElement();
    };
}
