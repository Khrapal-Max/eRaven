//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CreateCandidateModal
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs;
using Microsoft.AspNetCore.Components;
using System.Linq.Expressions;

namespace eRaven.Components.Pages.Persons.Registry.Pickers;

public partial class PositionUnitPicker
{
    [Parameter] public string Label { get; set; } = "Вакантна посада (опц.)";
    [Parameter] public IReadOnlyList<PositionUnitOptionDto> Items { get; set; } = [];

    [Parameter] public Guid? SelectedId { get; set; }
    [Parameter] public EventCallback<Guid?> SelectedIdChanged { get; set; }

    [Parameter] public EventCallback<PositionUnitOptionDto?> OnSelected { get; set; }

    // Для ValidationMessage
    [Parameter] public Expression<Func<object>>? For { get; set; }

    private string _search = string.Empty;
    private List<PositionUnitOptionDto> _filtered = [];
    private PositionUnitOptionDto? _selected;

    protected override void OnParametersSet()
    {
        _selected = SelectedId is Guid id ? Items.FirstOrDefault(x => x.Id == id) : null;

        // якщо вже є selection — таблицю не показуємо (вона в UI вже схована)
        if (_selected is null)
            ApplyFilter();
        else
            _filtered = [];
    }

    private void OnSearchInput(ChangeEventArgs e)
    {
        _search = e.Value?.ToString() ?? string.Empty;
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        _filtered = [];

        var s = _search?.Trim();
        if (string.IsNullOrWhiteSpace(s) || s.Length < 2)
            return;

        _filtered = [.. Items
            .Where(x =>
                (x.Code ?? "").Contains(s, StringComparison.InvariantCultureIgnoreCase) ||
                (x.ShortName ?? "").Contains(s, StringComparison.InvariantCultureIgnoreCase) ||
                (x.FullName ?? "").Contains(s, StringComparison.InvariantCultureIgnoreCase) ||
                (x.Rank ?? "").Contains(s, StringComparison.InvariantCultureIgnoreCase) ||
                (x.Tarif ?? "").Contains(s, StringComparison.InvariantCultureIgnoreCase))
            .Take(200)];
    }

    private Task ClearSearch()
    {
        _search = string.Empty;
        _filtered = [];
        return Task.CompletedTask;
    }

    private async Task ShowSearch()
    {
        // залишаємо SelectedId як є, але дозволяємо змінити:
        _selected = null;
        _search = string.Empty;
        _filtered = [];
        await InvokeAsync(StateHasChanged);
    }

    private async Task Select(PositionUnitOptionDto p)
    {
        SelectedId = p.Id;
        _selected = p;

        _search = string.Empty;
        _filtered = []; // ✅ ховаємо список одразу після вибору

        if (SelectedIdChanged.HasDelegate)
            await SelectedIdChanged.InvokeAsync(p.Id);

        if (OnSelected.HasDelegate)
            await OnSelected.InvokeAsync(p);
    }

    private async Task Clear()
    {
        SelectedId = null;
        _selected = null;
        _search = string.Empty;
        _filtered = [];

        if (SelectedIdChanged.HasDelegate)
            await SelectedIdChanged.InvokeAsync(null);

        if (OnSelected.HasDelegate)
            await OnSelected.InvokeAsync(null);
    }
}