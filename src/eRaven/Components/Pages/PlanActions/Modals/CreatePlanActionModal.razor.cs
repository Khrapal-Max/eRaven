//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// CreatePlanActionModal
//-----------------------------------------------------------------------------

using Blazored.FluentValidation;
using Blazored.Toast.Services;
using eRaven.Domain.Enums;
using eRaven.Domain.Models;
using Microsoft.AspNetCore.Components;

namespace eRaven.Components.Pages.PlanActions.Modals;

public partial class CreatePlanActionModal : ComponentBase
{
    // UI state
    private bool _show;
    private bool _edit;             // true — редагуємо всі поля; false — тільки дата/хвилини
    private string? _status;

    // time pickers (UTC, хвилини 00/30)
    private DateOnly _dateLocal;
    private string _dateLocalString => _dateLocal == default ? "" : _dateLocal.ToString("yyyy-MM-dd");
    private int _hourLocal;
    private string _hourLocalString => _hourLocal.ToString("00");
    private int _minuteLocal; // 0 or 30
    private string _minuteLocalString => _minuteLocal.ToString("00");

    // validator
    private FluentValidationValidator? _validator;

    // data
    private PlanAction? CreatePlanAction { get; set; } = new();

    [Parameter] public Person? Person { get; set; }
    [Parameter] public PlanAction? LastPlanAction { get; set; }
    [Parameter] public List<StatusKind> StatusKinds { get; set; } = [];

    [Parameter] public EventCallback<PlanAction> OnSaved { get; set; }

    [Inject] public IToastService ToastService { get; set; } = default!;

    private bool IsReadonly => !_edit;

    // ВІДКРИТТЯ модалки — єдиний публічний вхід
    public void Open()
    {
        // 1) підготовка моделі
        CreatePlanAction = new PlanAction { Id = Guid.NewGuid() };
        FillSnapshotFromPerson();
        ApplyPresetFromLastAction();

        // 2) час → нормалізація
        var nowUtc = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);
        InitTimePickersUtc(nowUtc);

        // 3) показ
        _show = true;
        StateHasChanged();
    }

    private void Close()
    {
        _show = false;
        StateHasChanged();
    }

    // ----------------- helpers -----------------

    private void FillSnapshotFromPerson()
    {
        // наповнюємо лише снапшотні поля та базу
        CreatePlanAction!.PersonId = Person!.Id;
        CreatePlanAction.Person = Person!;

        CreatePlanAction.Rnokpp = Person?.Rnokpp ?? string.Empty;
        CreatePlanAction.FullName = Person?.FullName ?? string.Empty;
        CreatePlanAction.RankName = Person?.Rank ?? string.Empty;
        CreatePlanAction.PositionName = Person?.PositionUnit?.FullName ?? string.Empty;

        CreatePlanAction.BZVP = Person?.BZVP ?? string.Empty;
        CreatePlanAction.Weapon = Person?.Weapon ?? string.Empty;
        CreatePlanAction.Callsign = Person?.Callsign ?? string.Empty;

        CreatePlanAction.StatusKindOnDate = Person?.StatusKind?.Modified.ToString("dd.MM.yyyy HH:mm") ?? string.Empty;

        _status = StatusKinds.FirstOrDefault(x => x.Id == Person?.StatusKindId)?.Name ?? string.Empty;
    }

    private void ApplyPresetFromLastAction()
    {
        // якщо остання дія була Dispatch → зараз створюємо Return з тими ж полями
        var wasDispatch = LastPlanAction?.MoveType == MoveType.Dispatch;

        _edit = !wasDispatch; // якщо Dispatch — то тільки дата/хвилини

        CreatePlanAction!.MoveType = wasDispatch ? MoveType.Return : MoveType.Dispatch;
        CreatePlanAction.Location = wasDispatch ? LastPlanAction!.Location : string.Empty;
        CreatePlanAction.GroupName = wasDispatch ? LastPlanAction!.GroupName : string.Empty;
        CreatePlanAction.CrewName = wasDispatch ? LastPlanAction!.CrewName : string.Empty;
    }

    private void InitTimePickersUtc(DateTime utc)
    {
        if (utc.Kind != DateTimeKind.Utc)
            utc = DateTime.SpecifyKind(utc, DateTimeKind.Utc);

        // хвилини лише 00 або 30
        var minutes = utc.Minute >= 30 ? 30 : 0;
        var normalized = new DateTime(utc.Year, utc.Month, utc.Day, utc.Hour, minutes, 0, DateTimeKind.Utc);

        _dateLocal = DateOnly.FromDateTime(normalized);
        _hourLocal = normalized.Hour;
        _minuteLocal = minutes;

        OnDateTimeChanged(normalized);
    }

    private void RecomputeUtc()
    {
        if (_dateLocal == default) return;

        var dt = new DateTime(_dateLocal.Year, _dateLocal.Month, _dateLocal.Day,
                              _hourLocal, _minuteLocal, 0, DateTimeKind.Utc);

        OnDateTimeChanged(dt);
        StateHasChanged();
    }

    // ----------------- UI change handlers -----------------

    private void OnDateChanged(ChangeEventArgs e)
    {
        if (DateOnly.TryParse(Convert.ToString(e.Value), out var d))
        {
            _dateLocal = d;
            RecomputeUtc();
        }
    }

    private void OnHourChanged(ChangeEventArgs e)
    {
        if (int.TryParse(Convert.ToString(e.Value), out var h) && h is >= 0 and <= 23)
        {
            _hourLocal = h;
            RecomputeUtc();
        }
    }

    private void OnMinuteChanged(ChangeEventArgs e)
    {
        _minuteLocal = Convert.ToString(e.Value) == "30" ? 30 : 0;
        RecomputeUtc();
    }

    private void OnDateTimeChanged(DateTime dateUtc)
    {
        CreatePlanAction!.EffectiveAtUtc = dateUtc;
    }

    // ----------------- submit -----------------

    private async Task CreateAction()
    {
        try
        {
            var ok = _validator is null || await _validator.ValidateAsync();

            if (LastPlanAction?.EffectiveAtUtc >= CreatePlanAction!.EffectiveAtUtc)
            {
                ToastService.ShowError("Дата і час планової дії не можуть бути раніше за останню планову дію.");
                return;
            }

            if (ok && CreatePlanAction is not null)
            {
                await OnSaved.InvokeAsync(CreatePlanAction);
            }
        }
        catch (Exception ex)
        {
            ToastService.ShowError(ex.Message);
        }
        finally
        {
            _show = false;
            await InvokeAsync(StateHasChanged);
        }
    }
}
