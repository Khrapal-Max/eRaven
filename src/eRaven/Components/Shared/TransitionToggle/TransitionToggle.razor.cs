//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TransitionToggle
//-----------------------------------------------------------------------------

using Microsoft.AspNetCore.Components;

namespace eRaven.Components.Shared.TransitionToggle;

public partial class TransitionToggle<TiD>
{
    // Публічні параметри
    [Parameter] public TiD FromId { get; set; } = default!;
    [Parameter] public TiD ToId { get; set; } = default!;
    [Parameter] public string ToName { get; set; } = string.Empty;

    // Поточний стан (керується батьком)
    [Parameter] public bool Checked { get; set; }
    [Parameter] public EventCallback<bool> CheckedChanged { get; set; }

    // Глобальні вимикачі
    [Parameter] public bool Disabled { get; set; }

    // Додатково: підказка
    [Parameter] public string? Title { get; set; }

    // Делегат підтвердження: (toId, turnOn) => true/false
    [Parameter] public Func<TiD, bool, Task<bool>>? ConfirmAsync { get; set; }

    // Делегат збереження: (toId, turnOn) => Task
    // Очікується, що всередині ви зробите всю потрібну логіку:
    // зберете новий HashSet дозволів та викличете TransitionService.SaveAllowedAsync(...)
    [Parameter] public Func<TiD, bool, Task>? SaveAsync { get; set; }

    private async Task OnClickAsync()
    {
        if (Disabled) return;

        var turnOn = !Checked;

        // 1) Підтвердження (опціонально)
        if (ConfirmAsync is not null)
        {
            var ok = await ConfirmAsync.Invoke(ToId, turnOn);
            if (!ok)
            {
                // Нічого не міняємо. Батько залишає Checked як є.
                // Ми керовані, тож просто перемалюємось у поточному стані.
                StateHasChanged();
                return;
            }
        }

        // 2) Збереження (обов’язково дайте делегат)
        if (SaveAsync is not null)
        {
            await SaveAsync.Invoke(ToId, turnOn);
        }

        // 3) Повідомляємо батька оновити «джерело істини»
        await CheckedChanged.InvokeAsync(turnOn);
    }
}
