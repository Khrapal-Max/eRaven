//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// RegistryToolbar
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs;
using eRaven.Domain.Enums;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace eRaven.Components.Pages.Persons.Registry;

public partial class RegistryToolbar : ComponentBase, IDisposable
{
    // ==========================
    // Parameters
    // ==========================
    [Parameter] public PersonsRegistryFilters Value { get; set; } = new();

    [Parameter] public EventCallback OnOpenImportExport { get; set; }
    [Parameter] public EventCallback OnCreateReserved { get; set; }
    [Parameter] public EventCallback<PersonsRegistryFilters> ValueChanged { get; set; }

    private string _searchDraft = string.Empty;
    private CancellationTokenSource? _searchCts;

    private bool HasAnyFilter =>
        Value.Lifecycle is not null ||
        Value.EnrollmentKind is not null ||
        !string.IsNullOrWhiteSpace(Value.Search);

    private static string BtnClass(bool active)
        => active ? "btn btn-sm btn-primary rounded-0"
                  : "btn btn-sm btn-outline-secondary rounded-0";

    private bool IsLifecycle(PersonLifecycle? lc) => Value.Lifecycle == lc;
    private bool IsKind(EnrollmentKind? k) => Value.EnrollmentKind == k;

    protected override void OnInitialized()
    {
        // ініціалізація драфту з зовнішнього Value один раз
        _searchDraft = Value.Search ?? string.Empty;
    }

    public void Dispose()
    {
        _searchCts?.Cancel();
        _searchCts?.Dispose();

        GC.SuppressFinalize(this);
    }


    // ==========================
    // Handlers
    // ==========================
    private Task HandleCreateClick()
        => OnCreateReserved.HasDelegate ? OnCreateReserved.InvokeAsync() : Task.CompletedTask;

    private Task SetLifecycle(PersonLifecycle? lc)
        => SetFilters(Value with { Lifecycle = lc });

    private Task SetKind(EnrollmentKind? k)
        => SetFilters(Value with { EnrollmentKind = k });

    private Task Reset()
    {
        CancelDebounce();
        _searchDraft = string.Empty;
        return SetFilters(new PersonsRegistryFilters());
    }

    private Task SetFilters(PersonsRegistryFilters next)
        => ValueChanged.HasDelegate ? ValueChanged.InvokeAsync(next) : Task.CompletedTask;

    private async Task OnSearchKeyDown(KeyboardEventArgs e)
    {
        if (e.Key is "Enter")
        {
            CancelDebounce();
            await ApplySearchAsync();
        }
        else if (e.Key is "Escape")
        {
            await ClearSearch();
        }
    }

    private async Task ClearSearch()
    {
        CancelDebounce();
        _searchDraft = string.Empty;
        await SetFilters(Value with { Search = null });
    }

    private void CancelDebounce()
    {
        _searchCts?.Cancel();
        _searchCts?.Dispose();
        _searchCts = null;
    }

    private Task ApplySearchAsync()
    {
        var normalized = string.IsNullOrWhiteSpace(_searchDraft) ? null : _searchDraft.Trim();
        return SetFilters(Value with { Search = normalized });
    }
}
