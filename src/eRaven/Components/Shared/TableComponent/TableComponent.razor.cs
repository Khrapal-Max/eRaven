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

    // Selection (optional, supports @bind-SelectedItem)
    [Parameter] public TItem? SelectedItem { get; set; }

    // Click event (always item)
    [Parameter] public EventCallback<TItem> OnClick { get; set; }

    // Optional: if Equals isn't stable -> compare by key
    [Parameter] public Func<TItem, object?>? KeySelector { get; set; }

    private string GetRowClass(TItem item)
    {
        var isSelected = IsSelected(item);
        return isSelected ? "table-row table-active" : "table-row";
    }

    private bool IsSelected(TItem item)
    {
        if (SelectedItem is null) return false;

        if (KeySelector is not null)
            return Equals(KeySelector(item), KeySelector(SelectedItem));

        return EqualityComparer<TItem>.Default.Equals(item, SelectedItem);
    }

    private async Task OnRowClicked(TItem item)
    {
        SelectedItem = item;

        await OnClick.InvokeAsync(item);
    }
}
