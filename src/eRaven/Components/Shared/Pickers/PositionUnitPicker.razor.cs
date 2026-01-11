//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CreateCandidateModal
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs;
using Microsoft.AspNetCore.Components;
using System.Linq.Expressions;

namespace eRaven.Components.Shared.Pickers;

public partial class PositionUnitPicker
{
    [Parameter] public string Label { get; set; } = "Вакантна посада (опц.)";

    [Parameter] public IReadOnlyList<PositionUnitOptionDto> Items { get; set; } = [];

    // двосторонній bind по Id (щоб інтегруватися з твоєю Model.PlannedPositionUnitId)
    [Parameter] public Guid? SelectedId { get; set; }
    [Parameter] public EventCallback<Guid?> SelectedIdChanged { get; set; }

    // повертаємо повний об’єкт (щоб ти міг поставити PlannedPosition = FullName)
    [Parameter] public EventCallback<PositionUnitOptionDto?> OnSelected { get; set; }

    // для ValidationMessage
    [Parameter] public Expression<Func<object>>? For { get; set; }

    private string _search = string.Empty;

    private List<PositionUnitOptionDto> _filtered = [];
    private PositionUnitOptionDto? _selected;

    private bool HasSelection => SelectedId is not null;

    protected override void OnParametersSet()
    {
        // синхронізуємо _selected якщо SelectedId поставили зовні (або Items оновили)
        _selected = SelectedId is Guid id ? Items.FirstOrDefault(x => x.Id == id) : null;

        ApplyFilter();
    }

    private void ApplyFilter()
    {
        if (Items.Count == 0)
        {
            _filtered = [];
            return;
        }

        var s = _search?.Trim();
        if (string.IsNullOrWhiteSpace(s))
        {
            _filtered = []; // нічого не показуємо без пошуку
            return;
        }
        if (s.Length < 2)
        {
            _filtered = []; // починаємо з 2 символів
            return;
        }

        s = s.ToLowerInvariant();

        _filtered = [.. Items
            .Where(x =>
                (x.Code ?? "").Contains(s, StringComparison.InvariantCultureIgnoreCase) ||
                (x.ShortName ?? "").Contains(s, StringComparison.InvariantCultureIgnoreCase) ||
                (x.FullName ?? "").Contains(s, StringComparison.InvariantCultureIgnoreCase) ||
                (x.Rank ?? "").Contains(s, StringComparison.InvariantCultureIgnoreCase) ||
                (x.Tarif ?? "").Contains(s, StringComparison.InvariantCultureIgnoreCase))
            .Take(200)];
    }

    private void OnSearchInput(ChangeEventArgs e)
    {
        _search = e.Value?.ToString() ?? string.Empty;
        ApplyFilter();
    }

    private string RowClass(PositionUnitOptionDto p)
        => SelectedId is Guid id && p.Id == id ? "table-active" : "";

    private async Task Select(PositionUnitOptionDto p)
    {
        SelectedId = p.Id;
        _selected = p;

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

        ApplyFilter();

        if (SelectedIdChanged.HasDelegate)
            await SelectedIdChanged.InvokeAsync(null);

        if (OnSelected.HasDelegate)
            await OnSelected.InvokeAsync(null);
    }
}