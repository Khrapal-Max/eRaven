//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetTransitionDrawer
//-----------------------------------------------------------------------------

using eRaven.Application.Commands;
using eRaven.Application.Commands.Timesheet;
using eRaven.Application.DTOs.Timesheet;
using eRaven.Application.DTOs.Timesheet.Models;
using eRaven.Application.Queries;
using eRaven.Application.Queries.Timesheet;
using eRaven.Infrastructure;
using eRaven.Presentation.Toasts;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace eRaven.Components.Pages.Timesheet.Drawers;

public partial class TimesheetTransitionDrawer : ComponentBase
{
    //======================================================================
    // DI
    //======================================================================

    [Inject] public IQueryHandler<GetTimesheetPolicyForCodeByCodeQuery, IReadOnlyList<TimesheetTransitionOptionDto>> GetOptions { get; set; } = default!;
    [Inject] public ICommandHandler<TransitionTimesheetStateCommand, Guid> Transition { get; set; } = default!;
    [Inject] public ToastService Toasts { get; set; } = default!;

    //======================================================================
    // Params
    //======================================================================

    [Parameter] public bool IsOpen { get; set; }
    [Parameter] public EventCallback<bool> IsOpenChanged { get; set; }

    [Parameter] public Guid PersonId { get; set; }
    [Parameter] public DateOnly AnchorDate { get; set; }                 // дата кліку/рядка
    [Parameter] public string CurrentCode { get; set; } = string.Empty;  // код табеля на цю дату

    [Parameter] public string? PersonLabel { get; set; }
    [Parameter] public EventCallback OnApplied { get; set; }             // ReloadAsync у батька

    //======================================================================
    // State
    //======================================================================

    private bool _loading;
    private bool _busy;
    private int SelectSize => Math.Clamp(_options.Count, 2, 20);

    private TransitionModel _model = new();
    private EditContext _editContext = default!;

    private IReadOnlyList<TimesheetTransitionOptionDto> _options = [];

    // щоб не перевантажувати щоразу при кожному ререндері
    private (Guid PersonId, DateOnly AnchorDate, string Code)? _loadedKey;

    //======================================================================
    // Lifecycle
    //======================================================================

    protected override async Task OnParametersSetAsync()
    {
        // ✅ При закритті — скинути стан (включно з _loadedKey)
        if (!IsOpen)
        {
            if (_loadedKey is not null || _options.Count > 0 || _busy || _loading)
                ResetState();

            return;
        }

        // якщо той самий контекст — не вантажимо повторно
        var key = (PersonId, AnchorDate, (CurrentCode ?? string.Empty).Trim());
        if (_loadedKey.HasValue && _loadedKey.Value.Equals(key))
            return;

        _loadedKey = key;

        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        _loading = true;
        _busy = false;

        // reset форми
        _model = new TransitionModel
        {
            InputDate = AnchorDate
        };
        _editContext = new EditContext(_model);

        _options = [];

        try
        {
            var code = (CurrentCode ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(code))
            {
                Toasts.Error("Поточний код порожній.");
                return;
            }

            if (string.Equals(code, TimesheetSystemCodes.NotInTimesheet, StringComparison.OrdinalIgnoreCase))
            {
                Toasts.Warning("Стан 'НБ' не застосовується як подія.");
                return;
            }

            _options = await GetOptions.HandleAsync(new GetTimesheetPolicyForCodeByCodeQuery(code));

            if (_options.Count == 0)
                return;

            // дефолтний вибір
            _model.Code = _options[0].Code;
        }
        catch (Exception ex)
        {
            Toasts.Error(ex.Message);
        }
        finally
        {
            _loading = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    //======================================================================
    // UI helpers
    //======================================================================

    private static string ShiftLabel(int shift)
        => shift switch
        {
            0 => "з дати події",
            1 => "ще поточний",
            _ => $"shift={shift}"
        };

    private TimesheetTransitionOptionDto? SelectedOption()
    {
        var code = (_model.Code ?? string.Empty).Trim();
        return _options.FirstOrDefault(x =>
            string.Equals(x.Code?.Trim(), code, StringComparison.OrdinalIgnoreCase));
    }

    //======================================================================
    // Actions
    //======================================================================

    private async Task SubmitAsync()
    {
        if (_busy) return;

        if (_editContext is not null && !_editContext.Validate())
            return;

        var nextCode = (_model.Code ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(nextCode))
        {
            Toasts.Error("Оберіть наступний код.");
            return;
        }

        _busy = true;

        try
        {
            var cmd = new TransitionTimesheetStateCommand(
                PersonId: PersonId,
                AnchorDate: AnchorDate,
                InputDate: _model.InputDate,
                NextCode: nextCode,
                Reference: string.IsNullOrWhiteSpace(_model.Reference) ? null : _model.Reference.Trim(),
                Note: string.IsNullOrWhiteSpace(_model.Note) ? null : _model.Note.Trim(),
                IsCorrection: false,
                Author: "ui", // TODO auth
                NowUtc: DateTime.UtcNow
            );

            await Transition.HandleAsync(cmd);

            Toasts.Success("Застосовано", $"Подію встановлено: {nextCode}.");

            if (OnApplied.HasDelegate)
                await OnApplied.InvokeAsync();

            await CloseAsync();
        }
        catch (Exception ex)
        {
            Toasts.Error(ex.Message);
        }
        finally
        {
            _busy = false;
        }
    }

    private void ResetState()
    {
        _loading = false;
        _busy = false;

        _options = [];

        // чистимо модель, щоб не лишались Reference/Note/Code
        _model = new TransitionModel();
        _editContext = new EditContext(_model);

        // дозволяємо перезавантаження при повторному відкритті
        _loadedKey = null;
    }

    private Task CloseAsync()
    {
        ResetState();
        return IsOpenChanged.InvokeAsync(false);
    }
}
