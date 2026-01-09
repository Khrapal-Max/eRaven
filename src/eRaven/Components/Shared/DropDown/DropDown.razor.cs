//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// DropDown
//-----------------------------------------------------------------------------

using Microsoft.AspNetCore.Components;

namespace eRaven.Components.Shared.DropDown;

public partial class DropDown<TEnum> : ComponentBase where TEnum : struct, Enum
{
    [Parameter]
    public TEnum? Value { get; set; }

    [Parameter]
    public string? Class { get; set; } = string.Empty;

    [Parameter]
    public EventCallback<TEnum> ValueChanged { get; set; }

    [Parameter, EditorRequired]
    public required IReadOnlyList<TEnum> EnumNames { get; set; }

    private async Task HandleChange(ChangeEventArgs e)
    {
        if (Enum.TryParse<TEnum>(e.Value?.ToString(), out var parsed))
        {
            await SetValue(parsed);
        }
    }

    private async Task SetValue(TEnum value)
    {
        Value = value;
        await ValueChanged.InvokeAsync(value);
    }
}
