//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// Table
//-----------------------------------------------------------------------------

using Microsoft.AspNetCore.Components;

namespace eRaven.Components.Shared.Table;

/// <summary>
/// Узагальнений компонент таблиці.
/// Підтримує:
/// - рендер заголовка та рядків через RenderFragment
/// - опційний клік по рядку (OnClick)
/// - опційний selection (через @bind-SelectedItem)
///
/// Важливо:
/// - Якщо selection не використовується (SelectedItemChanged не заданий) —
///   компонент НЕ буде встановлювати SelectedItem самостійно (щоб уникнути
///   неочікуваного підсвічування рядків).
/// </summary>
public partial class Table<TItem> : ComponentBase
{
    //======================================================================
    // Parameters: layout
    //======================================================================

    /// <summary>Додаткові CSS класи для &lt;table&gt;.</summary>
    [Parameter] public string Class { get; set; } = string.Empty;

    /// <summary>
    /// Дозволити обробку кліку по рядку.
    /// Якщо false — кліки ігноруються (OnClick/Selection не виконуються).
    /// </summary>
    [Parameter] public bool IsClickable { get; set; } = true;

    /// <summary>Заголовок таблиці (&lt;th&gt;...)</summary>
    [Parameter, EditorRequired] public RenderFragment TableHeader { get; set; } = default!;

    /// <summary>Шаблон рядка таблиці.</summary>
    [Parameter, EditorRequired] public RenderFragment<TItem> RowTemplate { get; set; } = default!;

    /// <summary>Джерело рядків.</summary>
    [Parameter] public IReadOnlyCollection<TItem> Items { get; set; } = [];

    //======================================================================
    // Parameters: selection
    //======================================================================

    /// <summary>
    /// Обраний елемент (для підсвічування).
    /// Зазвичай використовується через @bind-SelectedItem.
    /// </summary>
    [Parameter] public TItem? SelectedItem { get; set; }

    /// <summary>
    /// Callback для @bind-SelectedItem.
    /// Якщо не заданий — selection вважається вимкненим.
    /// </summary>
    [Parameter] public EventCallback<TItem?> SelectedItemChanged { get; set; }

    /// <summary>
    /// Опційний ключ-селектор для коректного порівняння SelectedItem.
    /// Корисно, коли TItem — record/DTO і потрібно порівнювати за Id.
    /// </summary>
    [Parameter] public Func<TItem, object?>? KeySelector { get; set; }

    //======================================================================
    // Parameters: click action
    //======================================================================

    /// <summary>
    /// Callback при кліку по рядку.
    /// Не залежить від selection.
    /// </summary>
    [Parameter] public EventCallback<TItem> OnClick { get; set; }

    //======================================================================
    // Computed flags
    //======================================================================

    /// <summary>Чи увімкнений selection (тільки якщо є bind).</summary>
    private bool IsSelectionEnabled => SelectedItemChanged.HasDelegate;

    /// <summary>Чи має таблиця реагувати на клік.</summary>
    private bool IsRowClickable => IsClickable && (IsSelectionEnabled || OnClick.HasDelegate);

    //======================================================================
    // Rendering helpers
    //======================================================================

    /// <summary>CSS клас рядка з урахуванням selection.</summary>
    private string GetRowClass(TItem item)
        => IsSelectionEnabled && IsSelected(item)
            ? "table-row table-active"
            : "table-row";

    /// <summary>Перевіряє, чи item є SelectedItem.</summary>
    private bool IsSelected(TItem item)
    {
        if (SelectedItem is null) return false;

        if (KeySelector is not null)
            return Equals(KeySelector(item), KeySelector(SelectedItem));

        return EqualityComparer<TItem>.Default.Equals(item, SelectedItem);
    }

    //======================================================================
    // Events
    //======================================================================

    /// <summary>
    /// Обробляє клік по рядку:
    /// - якщо увімкнений selection (є @bind-SelectedItem) — викликає SelectedItemChanged
    /// - якщо заданий OnClick — викликає OnClick
    ///
    /// Не встановлює SelectedItem внутрішньо, щоб уникнути неочікуваного підсвічування.
    /// </summary>
    private async Task OnRowClicked(TItem item)
    {
        if (!IsRowClickable)
            return;

        // 1) selection (лише якщо використовується bind)
        if (IsSelectionEnabled)
            await SelectedItemChanged.InvokeAsync(item);

        // 2) click action
        if (OnClick.HasDelegate)
            await OnClick.InvokeAsync(item);
    }
}
