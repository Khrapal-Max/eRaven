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
using eRaven.Infrastructure.Repositories.TimesheetPolicyRepository;
using eRaven.Presentation.Toasts;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace eRaven.Components.Pages.Timesheet.Drawers;

public partial class TimesheetTransitionDrawer : ComponentBase
{
    //======================================================================
    // DI
    //======================================================================

    [Inject] public ITimesheetPolicyRepository PolicyRepo { get; set; } = default!;
    [Inject] public ICommandHandler<TransitionTimesheetStateCommand, Guid> Transition { get; set; } = default!;
    [Inject] public ToastService Toasts { get; set; } = default!;

    //======================================================================
    // Params
    //======================================================================

    [Parameter] public bool IsOpen { get; set; }
    [Parameter] public EventCallback<bool> IsOpenChanged { get; set; }

    [Parameter] public Guid PersonId { get; set; }
    [Parameter] public DateOnly AnchorDate { get; set; }

    /// <summary>Код, який показаний як поточний на AnchorDate (для UX). Факт перевіряє handler.</summary>
    [Parameter] public string CurrentCode { get; set; } = string.Empty;

    /// <summary>Текст про людину (ПІБ/звання/посада) — для звірки.</summary>
    [Parameter] public string? PersonLabel { get; set; }

    [Parameter] public EventCallback OnApplied { get; set; }

    //======================================================================
    // State
    //======================================================================

    private bool _loading;
    private bool _busy;

    private string _currentTitle = string.Empty;

    private List<TimesheetTransitionOptionDto> _options = [];
    private TimesheetTransitionOptionDto? _selectedOption;

    private TransitionModel _model = new();
    private EditContext _editContext = default!;

    private (Guid PersonId, DateOnly AnchorDate, string Cur)? _loadedKey;

    //======================================================================
    // Lifecycle
    //======================================================================

    protected override async Task OnParametersSetAsync()
    {
        if (!IsOpen) return;

        var key = (PersonId, AnchorDate, (CurrentCode ?? string.Empty).Trim().ToUpperInvariant());
        if (_loadedKey == key) return;

        _loadedKey = key;
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        _loading = true;
        _busy = false;

        _options = [];
        _selectedOption = null;
        _currentTitle = string.Empty;

        try
        {
            if (PersonId == Guid.Empty)
            {
                Toasts.Error("Не визначено особу.");
                return;
            }

            var cur = (CurrentCode ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(cur))
            {
                Toasts.Error("Не визначено поточний код.");
                return;
            }

            // 1) Беремо активні коди (репо вже відсікає закриті/системні)
            var codes = await PolicyRepo.GetCodesAsync(); // includeInactive=false
            var curDef = codes.FirstOrDefault(x => string.Equals(x.Code?.Trim(), cur, StringComparison.OrdinalIgnoreCase));

            if (curDef is null)
            {
                Toasts.Error($"Код “{cur}” не знайдено у політиці.");
                return;
            }

            _currentTitle = curDef.Title;

            // 2) Беремо дозволені переходи для поточного коду
            var rules = await PolicyRepo.GetAllowedTransitionsAsync(curDef.Id);

            // 3) Будуємо опції для селекта (тільки дозволені To + їх shift)
            //    Припускаємо, що репо повертає вже валідні To-коди (активні) і join не "розсиплеться".
            var codeById = codes.ToDictionary(x => x.Id);

            _options = [.. rules
                .Where(r => codeById.ContainsKey(r.ToCodeId))
                .Select(r =>
                {
                    var to = codeById[r.ToCodeId];
                    return new TimesheetTransitionOptionDto(
                        Code: to.Code,
                        Title: to.Title,
                        StartShiftDays: r.StartShiftDays);
                })];

            // 4) Дефолт моделі
            var first = _options.FirstOrDefault();

            _model = new TransitionModel
            {
                PersonId = PersonId,
                InputDate = AnchorDate,
                Code = first?.Code ?? string.Empty,
                Reference = null,
                Note = null
            };

            _editContext = new EditContext(_model);

            SyncSelectedOption();
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

    private void SyncSelectedOption()
    {
        var code = (_model.Code ?? string.Empty).Trim();
        _selectedOption = _options.FirstOrDefault(x =>
            string.Equals(x.Code?.Trim(), code, StringComparison.OrdinalIgnoreCase));
    }

    private void OnNextCodeChanged(ChangeEventArgs _)
        => SyncSelectedOption();

    //======================================================================
    // Actions
    //======================================================================

    private async Task SubmitAsync()
    {
        if (_busy) return;

        if (_editContext is not null && !_editContext.Validate())
            return;

        _busy = true;

        try
        {
            var cmd = new TransitionTimesheetStateCommand(
                PersonId: PersonId,
                AnchorDate: AnchorDate,
                InputDate: _model.InputDate,
                NextCode: (_model.Code ?? string.Empty).Trim(),
                Reference: string.IsNullOrWhiteSpace(_model.Reference) ? null : _model.Reference.Trim(),
                Note: string.IsNullOrWhiteSpace(_model.Note) ? null : _model.Note.Trim(),
                Author: "ui", // TODO auth
                NowUtc: DateTime.UtcNow
            );

            await Transition.HandleAsync(cmd);

            Toasts.Success("Застосовано", $"Подію встановлено: {_model.Code}.");

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

    private Task CloseAsync()
        => IsOpenChanged.InvokeAsync(false);
}
