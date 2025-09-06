//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TableBaseComponent
//-----------------------------------------------------------------------------

using Microsoft.AspNetCore.Components;

namespace eRaven.Components.Shared.TableBaseComponent;

public partial class TableBaseComponent<TItem> : ComponentBase
{
    [Parameter] public string Class { get; set; } = string.Empty;

    /// <summary>Рендер заголовків колонок. Рендерьте <th scope="col">...</th>.</summary>
    [Parameter, EditorRequired] public RenderFragment TableHeader { get; set; } = default!;

    /// <summary>Шаблон рядка: рендерьте <td>...</td>.</summary>
    [Parameter, EditorRequired] public RenderFragment<TItem> RowTemplate { get; set; } = default!;

    [Parameter, EditorRequired] public IReadOnlyCollection<TItem> Items { get; set; } = Array.Empty<TItem>();

    /// <summary>Поточний вибраний елемент (2-way binding).</summary>
    [Parameter] public TItem? SelectedItem { get; set; }
    [Parameter] public EventCallback<TItem?> SelectedItemChanged { get; set; }

    /// <summary>Подія кліку по рядку (після оновлення SelectedItem).</summary>
    [Parameter] public EventCallback<TItem> OnClick { get; set; }

    /// <summary>Макс. висота таблиці (для внутрішнього скролу).</summary>
    [Parameter] public string MaxHeight { get; set; } = "73vh";

    /// <summary>Мін. висота контейнера.</summary>
    [Parameter] public string MinHeight { get; set; } = "200px";

    private async Task Click(TItem item)
    {
        if (!EqualityComparer<TItem>.Default.Equals(SelectedItem, item))
            await SelectedItemChanged.InvokeAsync(item);

        await OnClick.InvokeAsync(item);
    }
}