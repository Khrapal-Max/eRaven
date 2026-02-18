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
    //========================================
    // DI
    //========================================
    [Inject] public IQueryHandler<GetTimesheetTransitionContextQuery, TimesheetTransitionContextDto> GetContext { get; set; } = default!;
    [Inject] public ICommandHandler<TransitionTimesheetStateCommand, Guid> Transition { get; set; } = default!;
    [Inject] public ToastService Toasts { get; set; } = default!;

    //========================================
    // Parameters
    //========================================
    [Parameter] public bool IsOpen { get; set; }
    [Parameter] public EventCallback<bool> IsOpenChanged { get; set; }

    [Parameter] public Guid PersonId { get; set; }

    /// <summary>
    /// Стартова дата для форми (за замовчуванням — операційна дата/курсор).
    /// Drawer може змінювати цю дату в UI і сам перезавантажує контекст.
    /// </summary>
    [Parameter] public DateOnly InitialDate { get; set; }

    /// <summary>
    /// Операційна дата (курсор/сьогодні): події створюємо не раніше цієї дати.
    /// Корекції — в персональному табелі.
    /// </summary>
    [Parameter] public DateOnly OperatorDate { get; set; }

    [Parameter] public string? PersonLabel { get; set; }
    [Parameter] public EventCallback OnApplied { get; set; }

    //========================================
    // UI state
    //========================================
    private bool _loading;
    private bool _busy;

    private int SelectSize => Math.Clamp(_options.Count, 2, 20);

    private TransitionModel _model = new();
    private EditContext _editContext = default!;
    private IReadOnlyList<TimesheetTransitionOptionDto> _options = [];

    private Guid _currentCodeId;
    private string _currentCode = TimesheetSystemCodes.NotInTimesheet;

    private (Guid PersonId, DateOnly InitialDate, DateOnly OperatorDate)? _loadedKey;

    //========================================
    // Lifecycle
    //========================================
    protected override async Task OnParametersSetAsync()
    {
        if (!IsOpen)
        {
            if (_loadedKey is not null || _options.Count > 0 || _busy || _loading)
                ResetState();
            return;
        }

        var key = (PersonId, InitialDate, OperatorDate);
        if (_loadedKey.HasValue && _loadedKey.Value.Equals(key))
            return;

        _loadedKey = key;

        _model = new TransitionModel
        {
            InputDate = InitialDate == default ? OperatorDate : InitialDate,
            CodeId = null
        };
        _editContext = new EditContext(_model);

        await LoadContextAsync(_model.InputDate);
    }

    private async Task OnInputDateChangedAsync()
        => await LoadContextAsync(_model.InputDate);

    private async Task LoadContextAsync(DateOnly onDate)
    {
        _loading = true;
        _busy = false;

        _options = [];
        _currentCodeId = Guid.Empty;
        _currentCode = TimesheetSystemCodes.NotInTimesheet;

        try
        {
            if (PersonId == Guid.Empty)
            {
                Toasts.Error("PersonId не задано.");
                return;
            }

            if (onDate == default)
            {
                Toasts.Error("Дата події некоректна.");
                return;
            }

            var ctx = await GetContext.HandleAsync(new GetTimesheetTransitionContextQuery(PersonId, onDate));

            _currentCodeId = ctx.CurrentCodeId;
            _currentCode = (ctx.CurrentCode ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(_currentCode))
                _currentCode = TimesheetSystemCodes.NotInTimesheet;

            if (string.Equals(_currentCode, TimesheetSystemCodes.NotInTimesheet, StringComparison.OrdinalIgnoreCase))
            {
                // derived або помилковий entry — переходи не пропонуємо
                _options = [];
                _model.CodeId = null;
                return;
            }

            _options = ctx.Options ?? [];

            if (_options.Count == 0)
            {
                _model.CodeId = null;
                return;
            }

            // Зберігаємо вибір, якщо він ще валідний; інакше — перша опція.
            if (!_model.CodeId.HasValue || !_options.Any(x => x.TransitionCodeId == _model.CodeId.Value))
                _model.CodeId = _options[0].TransitionCodeId;
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

    private static string ShiftLabel(int shift)
        => shift switch
        {
            0 => "з дати події",
            1 => "ще поточний",
            _ => $"shift={shift}"
        };

    private TimesheetTransitionOptionDto? SelectedOption()
    {
        if (!_model.CodeId.HasValue || _model.CodeId.Value == Guid.Empty)
            return null;

        return _options.FirstOrDefault(x => x.TransitionCodeId == _model.CodeId.Value);
    }

    //========================================
    // Commands
    //========================================
    private async Task SubmitAsync()
    {
        if (_busy) return;

        if (_editContext is not null && !_editContext.Validate())
            return;

        if (_model.InputDate < OperatorDate)
        {
            Toasts.Warning("Корекції виконуються у персональному табелі.");
            return;
        }

        if (!_model.CodeId.HasValue || _model.CodeId.Value == Guid.Empty)
        {
            Toasts.Error("Оберіть наступний код.");
            return;
        }

        var selected = _options.FirstOrDefault(x => x.TransitionCodeId == _model.CodeId.Value);
        if (selected is null)
        {
            Toasts.Error("Обраний код не знайдено у списку опцій.");
            return;
        }

        _busy = true;

        try
        {
            var cmd = new TransitionTimesheetStateCommand(
                PersonId: PersonId,
                AnchorDate: OperatorDate,
                InputDate: _model.InputDate,
                NextCode: selected.TransitionCodeId,
                Reference: string.IsNullOrWhiteSpace(_model.Reference) ? null : _model.Reference.Trim(),
                Note: string.IsNullOrWhiteSpace(_model.Note) ? null : _model.Note.Trim(),
                IsCorrection: false,
                Author: "ui", // TODO auth
                NowUtc: DateTime.UtcNow
            );

            await Transition.HandleAsync(cmd);

            Toasts.Success("Застосовано", $"Подію встановлено: {selected.Code}.");

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
        _currentCodeId = Guid.Empty;
        _currentCode = TimesheetSystemCodes.NotInTimesheet;

        _model = new TransitionModel();
        _editContext = new EditContext(_model);

        _loadedKey = null;
    }

    private Task CloseAsync()
    {
        ResetState();
        return IsOpenChanged.InvokeAsync(false);
    }
}
