/*//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// StatusSetModal — code-behind (partial class)
// Групи:
//   1) Параметри (вхідні дані з батьківського компонента)
//   2) Локальна модель (SetPersonStatusViewModel) + EditContext + стан
//   3) Життєвий цикл / ініціалізація
//   4) Обробники (Submit/Close) та допоміжні методи
// Замітки:
//   - EditContext + OnFieldChanged гарантує коректне ввімкнення кнопки через CanSubmit.
//   - DateLocal (DateTime?) — проксі для _vm.Moment (00:00 локального дня, Kind=Unspecified).
//-----------------------------------------------------------------------------

using eRaven.Domain.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace eRaven.Components.Pages.Statuses.Modals;

public partial class StatusSetModal : ComponentBase
{
    // =========================
    // 1) Параметри (вхідні)
    // =========================
    [Parameter] public bool Open { get; set; }
    [Parameter] public bool Busy { get; set; }
    [Parameter] public Person? PersonCard { get; set; }
    [Parameter] public PersonStatus? CurrentStatus { get; set; }
    [Parameter] public IReadOnlyList<StatusKind>? AllowedNextStatuses { get; set; }
    [Parameter] public EventCallback OnClose { get; set; }
    [Parameter] public EventCallback<SetPersonStatusViewModel> OnSubmit { get; set; }

    // =========================
    // 2) Локальна модель + EditContext + стан
    // =========================
    private SetPersonStatusViewModel _vm = new();
    private EditContext? _editCtx;

    // Кнопка дозволена, коли обрано статус і дату (Moment != default)
    protected bool CanSubmit => !Busy && _vm.StatusId > 0 && _vm.Moment != default;

    // Проксі-властивість для прив’язки <InputDate TValue="DateTime?">
    private DateTime? DateLocal
    {
        get => _vm.Moment == default ? null : _vm.Moment.Date;
        set
        {
            if (value is null)
            {
                _vm.Moment = default;
            }
            else
            {
                var d = value.Value.Date;
                _vm.Moment = DateTime.SpecifyKind(new DateTime(d.Year, d.Month, d.Day, 0, 0, 0), DateTimeKind.Unspecified);
            }

            _editCtx?.NotifyFieldChanged(FieldIdentifier.Create(() => _vm.Moment));
            StateHasChanged();
        }
    }

    // =========================
    // 3) Життєвий цикл / ініціалізація
    // =========================
    protected override void OnParametersSet()
    {
        if (Open)
        {
            if (_editCtx is null || _editCtx.Model != _vm)
            {
                _editCtx = new EditContext(_vm);
                _editCtx.OnFieldChanged += (_, __) => InvokeAsync(StateHasChanged);
            }

            if (PersonCard is not null)
                _vm.PersonId = PersonCard.Id;
        }
        else
        {
            // Повний reset при закритті
            _vm = new SetPersonStatusViewModel();
            _editCtx = null;
        }
    }

    // =========================
    // 4) Обробники та допоміжні
    // =========================
    private void OnStatusChanged(int value)
    {
        _vm.StatusId = value;
        _editCtx?.NotifyFieldChanged(FieldIdentifier.Create(() => _vm.StatusId));
        StateHasChanged();
    }

    private async Task HandleValidSubmitAsync()
    {
        if (PersonCard is null) return;
        if (!CanSubmit) return;

        _vm.PersonId = PersonCard.Id; // підстраховка
        await OnSubmit.InvokeAsync(_vm);
    }

    private async Task OnCancelClick()
    {
        await OnClose.InvokeAsync();
    }

    private static string? GetStatusLabel(StatusKind status)
    {
        return string.IsNullOrWhiteSpace(status.Name) ? null : status.Name.Trim();
    }
}
*/