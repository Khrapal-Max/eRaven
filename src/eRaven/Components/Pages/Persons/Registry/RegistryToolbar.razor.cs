//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// RegistryToolbar
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs;
using eRaven.Domain.Enums;
using Microsoft.AspNetCore.Components;

namespace eRaven.Components.Pages.Persons.Registry;

public partial class RegistryToolbar
{
    // =========================
    // Parameters
    // =========================

    [Parameter] public PersonsRegistryFilters Value { get; set; } = new();
    [Parameter] public EventCallback<PersonsRegistryFilters> ValueChanged { get; set; }
    [Parameter] public EventCallback OnCreateReserved { get; set; }

    // =========================
    // UI state
    // =========================

    private bool HasAnyFilter => Value.Lifecycle is not null || Value.EnrollmentKind is not null;

    private static string BtnClass(bool active)
        => active
            ? "btn btn-sm btn-primary rounded-0"
            : "btn btn-sm btn-outline-secondary rounded-0";

    private bool IsLifecycle(PersonLifecycle? lc) => Value.Lifecycle == lc;
    private bool IsKind(EnrollmentKind? k) => Value.EnrollmentKind == k;

    // =========================
    // Actions
    // =========================

    private Task HandleCreateClick()
        => OnCreateReserved.HasDelegate ? OnCreateReserved.InvokeAsync() : Task.CompletedTask;

    private Task SetLifecycle(PersonLifecycle? lc)
        => SetFilters(Value with { Lifecycle = lc });

    private Task SetKind(EnrollmentKind? k)
        => SetFilters(Value with { EnrollmentKind = k });

    private Task Reset()
        => SetFilters(new PersonsRegistryFilters());

    private Task SetFilters(PersonsRegistryFilters next)
        => ValueChanged.HasDelegate ? ValueChanged.InvokeAsync(next) : Task.CompletedTask;
}
