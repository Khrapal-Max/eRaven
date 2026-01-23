//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetPolicyConfigurator (code-behind)
//-----------------------------------------------------------------------------

using eRaven.Domain.Entities;
using eRaven.Domain.Enums;
using eRaven.Infrastructure.Repositories.TimesheetPolicyRepository;
using eRaven.Presentation.Toasts;
using Microsoft.AspNetCore.Components;

namespace eRaven.Components.Pages.Timesheet;

public partial class TimesheetPolicyConfigurator
{
    [Inject] public ITimesheetPolicyRepository PolicyRepo { get; set; } = default!;
    [Inject] public ToastService Toasts { get; set; } = default!;

    private List<TimesheetCodeDefinition> _mainCodes = [];
    private List<TimesheetCodeDefinition> _taskCodes = [];

    private TimesheetCodeDefinition? _selected;
    private TimesheetLane _selectedLane = TimesheetLane.Main;

    private TimesheetEndDateMeaning _endDateMeaning = TimesheetEndDateMeaning.LastDayOfThisCode;
    private string? _nextCodeOnEnd;

    private HashSet<Guid> _allowedTo = [];
    private bool _canSave;

    protected override async Task OnInitializedAsync()
    {
        _mainCodes = [.. (await PolicyRepo.GetCodesAsync(TimesheetLane.Main))];
        _taskCodes = [.. (await PolicyRepo.GetCodesAsync(TimesheetLane.Task))];

        // Auto-select first available (Main preferred)
        if (_mainCodes.Count > 0)
            await SelectAsync(TimesheetLane.Main, _mainCodes[0]);
        else if (_taskCodes.Count > 0)
            await SelectAsync(TimesheetLane.Task, _taskCodes[0]);
    }

    private async Task SelectAsync(TimesheetLane lane, TimesheetCodeDefinition def)
    {
        _selectedLane = lane;
        _selected = def;

        _endDateMeaning = def.EndDateMeaning;
        _nextCodeOnEnd = def.NextCodeOnEnd;

        _allowedTo = [.. (await PolicyRepo.GetAllowedNextAsync(def.Id))];
        _canSave = false;
    }

    private IEnumerable<TimesheetCodeDefinition> RightPanelTargets()
    {
        if (_selected is null) return [];

        var list = _selectedLane == TimesheetLane.Main ? _mainCodes : _taskCodes;
        return list.Where(x => x.Id != _selected.Id);
    }

    private void Toggle(Guid toId, bool value)
    {
        if (_selected is null) return;

        if (value) _allowedTo.Add(toId);
        else _allowedTo.Remove(toId);

        _canSave = true;
    }

    private void SelectAll()
    {
        if (_selected is null) return;

        _allowedTo = [.. RightPanelTargets().Select(x => x.Id)];
        _canSave = true;
    }

    private void ClearAll()
    {
        _allowedTo.Clear();
        _canSave = true;
    }

    private Task OnMeaningChanged(ChangeEventArgs e)
    {
        var raw = e.Value?.ToString();

        if (!Enum.TryParse<TimesheetEndDateMeaning>(raw, ignoreCase: true, out var meaning))
            return Task.CompletedTask;

        _endDateMeaning = meaning;

        // Якщо meaning не FirstDayOfNextCode — NextCodeOnEnd не має сенсу
        if (_endDateMeaning != TimesheetEndDateMeaning.FirstDayOfNextCode)
            _nextCodeOnEnd = null;

        // Для Task теж зазвичай не потрібен NextCodeOnEnd
        if (_selectedLane == TimesheetLane.Task)
            _nextCodeOnEnd = null;

        _canSave = true;
        return Task.CompletedTask;
    }

    private Task OnNextCodeChanged(ChangeEventArgs e)
    {
        // Показується тільки для Main + FirstDayOfNextCode, але все одно безпечно обробимо
        var v = e.Value?.ToString();
        _nextCodeOnEnd = string.IsNullOrWhiteSpace(v) ? null : v.Trim();

        _canSave = true;
        return Task.CompletedTask;
    }

    private async Task SaveAsync()
    {
        if (_selected is null) return;

        // Normalize: only meaningful for Main + FirstDayOfNextCode
        var next = _endDateMeaning == TimesheetEndDateMeaning.FirstDayOfNextCode && _selectedLane == TimesheetLane.Main
            ? (string.IsNullOrWhiteSpace(_nextCodeOnEnd) ? "30" : _nextCodeOnEnd.Trim())
            : null;

        await PolicyRepo.SavePolicyAsync(
            lane: _selectedLane,
            fromCodeId: _selected.Id,
            endDateMeaning: _endDateMeaning,
            nextCodeOnEnd: next,
            allowedToCodeIds: [.. _allowedTo],
            author: "ui",
            nowUtc: DateTime.UtcNow);

        _canSave = false;

        Toasts.Success("Збережено", $"Політика для {_selectedLane}/{_selected.Code} оновлена.");
    }
}
