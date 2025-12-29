//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TableComponent
//-----------------------------------------------------------------------------

using Microsoft.AspNetCore.Components;

namespace eRaven.Components.Shared.TableComponent;

public partial class TableComponent<TItem> : ComponentBase
{
    [Parameter] public string Class { get; set; } = string.Empty;

    [Parameter, EditorRequired] public RenderFragment TableHeader { get; set; } = default!;
    [Parameter, EditorRequired] public RenderFragment<TItem> RowTemplate { get; set; } = default!;
    [Parameter] public IReadOnlyCollection<TItem> Items { get; set; } = [];

    // Selection (supports @bind-SelectedItem)
    [Parameter] public TItem? SelectedItem { get; set; }
    [Parameter] public EventCallback<TItem?> SelectedItemChanged { get; set; }

    [Parameter] public EventCallback<TItem> OnClick { get; set; }
    [Parameter] public Func<TItem, object?>? KeySelector { get; set; }

    private string GetRowClass(TItem item)
        => IsSelected(item) ? "table-row table-active" : "table-row";

    private bool IsSelected(TItem item)
    {
        if (SelectedItem is null) return false;

        if (KeySelector is not null)
            return Equals(KeySelector(item), KeySelector(SelectedItem));

        return EqualityComparer<TItem>.Default.Equals(item, SelectedItem);
    }

    private async Task OnRowClicked(TItem item)
    {
        if (SelectedItemChanged.HasDelegate)
            await SelectedItemChanged.InvokeAsync(item);
        else
            SelectedItem = item;

        if (OnClick.HasDelegate)
            await OnClick.InvokeAsync(item);
    }
}