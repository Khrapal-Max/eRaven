//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// SearchBox
//-----------------------------------------------------------------------------

using Microsoft.AspNetCore.Components;

namespace eRaven.Components.Shared.SearchBox;

public partial class SearchBox : ComponentBase, IDisposable
{
    private CancellationTokenSource? _cts;
    private string? _currentValue;

    [Parameter] public int Delay { get; set; } = 300;
    [Parameter] public string? Value { get; set; }
    [Parameter] public string Placeholder { get; set; } = "Пошук";
    [Parameter] public string Icon { get; set; } = "bi bi-search";
    [Parameter] public string MaxWidth { get; set; } = "680px";
    [Parameter] public bool Disabled { get; set; }

    [Parameter] public EventCallback OnSearch { get; set; }
    [Parameter] public EventCallback<string?> ValueChanged { get; set; }

    // двосторонній байндинг всередині компонента
    private string? CurrentValue
    {
        get => _currentValue;
        set
        {
            if (!Equals(_currentValue, value))
            {
                _currentValue = value;
                // піднімаємо ValueChanged для батьківського двостороннього зв'язування
                if (ValueChanged.HasDelegate)
                    _ = ValueChanged.InvokeAsync(value);
            }
        }
    }

    protected override void OnParametersSet()
    {
        if (!Equals(_currentValue, Value))
            _currentValue = Value;
    }

    private async Task OnInputAfter()
    {
        _cts?.Cancel();
        _cts?.Dispose();             // ← додати
        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        try
        {
            await Task.Delay(Delay, token);
            if (!token.IsCancellationRequested)
                await OnSearch.InvokeAsync();
        }
        catch (TaskCanceledException) { }
    }

    private Task Reload()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
        return OnSearch.InvokeAsync();
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();

        GC.SuppressFinalize(this);
    }
}
