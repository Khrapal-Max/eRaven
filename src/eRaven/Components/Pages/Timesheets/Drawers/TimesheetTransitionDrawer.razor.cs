//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// TimesheetTransitionDrawer
//-----------------------------------------------------------------------------

using eRaven.Application.Commands;
using eRaven.Application.Commands.Timesheets;
using eRaven.Application.DTOs.Enums;
using eRaven.Application.DTOs.Timesheets.Models;
using eRaven.Application.DTOs.Timesheets.Policy;
using eRaven.Application.Queries;
using eRaven.Application.Queries.Timesheets;
using eRaven.Presentation.Toasts;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace eRaven.Components.Pages.Timesheets.Drawers;

public partial class TimesheetTransitionDrawer : ComponentBase
{
    //========================================
    // DI
    //========================================
    [Inject] public IQueryHandler<GetTimesheetTransitionContextQuery, TimesheetTransitionContextDto> GetTimesheetTransitionContextQueryHandler { get; set; } = default!;
    [Inject] public ICommandHandler<TransitionTimesheetStateCommand, Guid> TransitionTimesheetStateCommandHandler { get; set; } = default!;
    [Inject] public ToastService ToastService { get; set; } = default!;

    //========================================
    // Parameters
    //========================================
    [Parameter] public bool IsOpen { get; set; }
    [Parameter] public EventCallback<bool> IsOpenChanged { get; set; }
    [Parameter] public Guid PersonId { get; set; }
    [Parameter] public DateOnly InitialDate { get; set; }
    [Parameter] public DateOnly OperatorDate { get; set; }
    [Parameter] public string? PersonLabel { get; set; }
    [Parameter] public EventCallback OnApplied { get; set; }

    //========================================
    // UI state
    //========================================
    private bool _loading;
    private bool _busy;

    private AddTransitionTimesheetCodeModel _model = new();
    private EditContext _editContext = default!;

    private IReadOnlyList<TimesheetTransitionOptionDto> _transitionOptions = [];
    private IReadOnlyList<TimesheetTransitionOptionDto> _emergencyOptions = [];

    private Guid? _currentCodeId;
    private string _currentCode = string.Empty;
    private bool _isDerived;
    private RoleCodeDto? _currentRole;

    private bool _isSystemCurrent;

    private (Guid PersonId, DateOnly InitialDate, DateOnly OperatorDate)? _loadedKey;

    private static int SelectSize(IReadOnlyList<TimesheetTransitionOptionDto> options)
        => Math.Clamp(options.Count, 2, 14);

    //========================================
    // Lifecycle
    //========================================
    protected override async Task OnParametersSetAsync()
    {
        if (!IsOpen)
        {
            if (_loadedKey is not null || _busy || _loading)
                ResetState();
            return;
        }

        var key = (PersonId, InitialDate, OperatorDate);
        if (_loadedKey.HasValue && _loadedKey.Value.Equals(key))
            return;

        _loadedKey = key;

        _model = new AddTransitionTimesheetCodeModel
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

        _transitionOptions = [];
        _emergencyOptions = [];
        _currentCodeId = null;
        _currentCode = string.Empty;
        _isDerived = true;
        _currentRole = null;
        _isSystemCurrent = false;

        try
        {
            if (PersonId == Guid.Empty)
            {
                ToastService.Error("PersonId не задано.");
                return;
            }
            if (onDate == default)
            {
                ToastService.Error("Дата події некоректна.");
                return;
            }

            var ctx = await GetTimesheetTransitionContextQueryHandler.HandleAsync(new GetTimesheetTransitionContextQuery(PersonId, onDate));

            _currentCodeId = ctx.CurrentCodeId;
            _currentCode = (ctx.CurrentCode ?? string.Empty).Trim();
            _isDerived = ctx.IsDerived;
            _currentRole = ctx.CurrentRole;
            _isSystemCurrent = (_currentRole == RoleCodeDto.SystemCode);

            _transitionOptions = ctx.TransitionOptions ?? [];
            _emergencyOptions = ctx.EmergencyOptions ?? [];

            // auto-pick: якщо є transition options — перша; інакше якщо є emergency — перша
            var preferred = _transitionOptions.Count > 0 ? _transitionOptions[0]
                           : _emergencyOptions.Count > 0 ? _emergencyOptions[0]
                           : null;

            if (preferred is null)
            {
                _model.CodeId = null;
                return;
            }

            // зберігаємо вибір якщо він ще валідний
            if (_model.CodeId.HasValue)
            {
                var id = _model.CodeId.Value;
                var exists = _transitionOptions.Any(x => x.TransitionCodeId == id)
                             || _emergencyOptions.Any(x => x.TransitionCodeId == id);
                if (!exists)
                    _model.CodeId = preferred.TransitionCodeId;
            }
            else
            {
                _model.CodeId = preferred.TransitionCodeId;
            }
        }
        catch (Exception ex)
        {
            ToastService.Error(ex.Message);
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

        var id = _model.CodeId.Value;
        return _transitionOptions.FirstOrDefault(x => x.TransitionCodeId == id)
            ?? _emergencyOptions.FirstOrDefault(x => x.TransitionCodeId == id);
    }

    private bool IsEmergencySelected()
    {
        if (!_model.CodeId.HasValue) return false;
        var id = _model.CodeId.Value;
        return _emergencyOptions.Any(x => x.TransitionCodeId == id);
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
            ToastService.Warning("Корекції виконуються у персональному табелі.");
            return;
        }

        if (_isDerived)
        {
            var label = string.IsNullOrWhiteSpace(_currentCode) ? "derived" : _currentCode;
            ToastService.Warning($"Стан “{label}” є derived. Події створюються лише для активних станів.");
            return;
        }

        if (_isSystemCurrent)
        {
            ToastService.Warning("Під активним system-кодом ручні події заблоковані.");
            return;
        }

        if (!_model.CodeId.HasValue || _model.CodeId.Value == Guid.Empty)
        {
            ToastService.Error("Оберіть наступний код.");
            return;
        }

        var selected = SelectedOption();
        if (selected is null)
        {
            ToastService.Error("Обраний код не знайдено у списку опцій.");
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
                NowUtc: DateTime.UtcNow);

            await TransitionTimesheetStateCommandHandler.HandleAsync(cmd);

            ToastService.Success("Застосовано", $"Подію встановлено: {selected.Code}.");

            if (OnApplied.HasDelegate)
                await OnApplied.InvokeAsync();
        }
        catch (Exception ex)
        {
            ToastService.Error(ex.Message);
        }
        finally
        {
            _busy = false;
            await CloseAsync();
        }
    }

    private void ResetState()
    {
        _loading = false;
        _busy = false;

        _transitionOptions = [];
        _emergencyOptions = [];
        _currentCodeId = null;
        _currentCode = string.Empty;
        _isDerived = true;
        _currentRole = null;
        _isSystemCurrent = false;

        _model = new AddTransitionTimesheetCodeModel();
        _editContext = new EditContext(_model);
        _loadedKey = null;
    }

    private Task CloseAsync()
    {
        ResetState();
        return IsOpenChanged.InvokeAsync(false);
    }
}
