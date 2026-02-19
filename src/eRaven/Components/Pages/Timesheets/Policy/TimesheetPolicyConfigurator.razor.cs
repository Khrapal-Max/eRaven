//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetPolicyConfigurator (code-behind)
//-----------------------------------------------------------------------------
//
// Важливі принципи:
// 1) НБ ("НБ") — системний стан "поза табелем". Він НЕ є подією і не конфігурується.
//    Тому в UI ми його не показуємо (пропускаємо в foreach).
//
// 2) Політика задається для конкретного коду (from):
//    - Allowed transitions (to IDs)
//-----------------------------------------------------------------------------

using eRaven.Application.Commands;
using eRaven.Application.Commands.Timesheets;
using eRaven.Application.DTOs.Timesheets.Policy;
using eRaven.Application.Queries;
using eRaven.Application.Queries.Timesheets;
using eRaven.Presentation.Toasts;
using Microsoft.AspNetCore.Components;

namespace eRaven.Components.Pages.Timesheets.Policy;

/// <summary>
/// Конфігуратор політики переходів табеля (без lane).
/// 
/// Налаштовує для одного коду (from):
/// - список дозволених наступних кодів (to),
/// - семантику інтерпретації дати завершення,
/// 
/// NB (“НБ”) — системний стан “поза табелем”: не є подією і не конфігурується.
/// </summary>
public partial class TimesheetPolicyConfigurator : ComponentBase
{
    //======================================================================
    // DI
    //======================================================================

    [Inject] public IQueryHandler<GetTimesheetPolicyCodesQuery, IReadOnlyList<TimesheetCodeDto>> GetCodes { get; set; } = default!;
    [Inject] public IQueryHandler<GetTimesheetPolicyForCodeQuery, TimesheetPolicyEditorDto?> GetPolicy { get; set; } = default!;
    [Inject] public ICommandHandler<SaveTimesheetPolicyCommand> SavePolicy { get; set; } = default!;
    [Inject] public ToastService Toasts { get; set; } = default!;

    //======================================================================
    // Loaded data
    //======================================================================

    private List<TimesheetCodeDto> _codes = [];

    //======================================================================
    // Current selection + edit buffer
    //======================================================================

    private TimesheetCodeDto? _selected;

    private string _editTitle = string.Empty;
    private string? _editDescription;
    private int _editSortOrder;
    private int _editPriority;
    private bool _editIsTerminal;

    /// <summary>allowed transitions map: ToCodeId -> StartShiftDays (0/1)</summary>
    private Dictionary<Guid, int> _transitions = [];

    private bool _canSave;

    //======================================================================
    // Drawers (will be replaced with real components)
    //======================================================================

    private bool _createOpen;
    private bool _closeOpen;

    private TimesheetCodeDto? _closeTarget;

    //======================================================================
    // Lifecycle
    //======================================================================

    protected override async Task OnInitializedAsync()
        => await LoadCodes();

    private async Task LoadCodes()
        => _codes = [.. await GetCodes.HandleAsync(new GetTimesheetPolicyCodesQuery())];

    //======================================================================
    // Selection
    //======================================================================

    private async Task SelectAsync(Guid codeId)
    {
        var policy = await GetPolicy.HandleAsync(new GetTimesheetPolicyForCodeQuery(codeId));
        if (policy is null)
        {
            _selected = null;
            Toasts.Warning("Код не знайдено або недоступний.");
            return;
        }

        _selected = policy.Code;

        _editTitle = policy.Code.Title ?? string.Empty;
        _editDescription = policy.Code.Description;
        _editSortOrder = policy.Code.SortOrder;
        _editPriority = policy.Code.Priority;
        _editIsTerminal = policy.Code.IsTerminal;

        _transitions = policy.AllowedTransitions
            .ToDictionary(x => x.ToCodeId, x => x.StartShiftDays);

        _canSave = false;
    }

    private IEnumerable<TimesheetCodeDto> RightPanelTargets()
    {
        if (_selected is null) return [];
        return _codes.Where(x => x.Id != _selected.Id);
    }

    //======================================================================
    // Edit code fields
    //======================================================================

    private void OnTitleChanged(ChangeEventArgs e)
    {
        _editTitle = e.Value?.ToString() ?? string.Empty;
        _canSave = true;
    }

    private void OnDescriptionChanged(ChangeEventArgs e)
    {
        var v = e.Value?.ToString();
        _editDescription = string.IsNullOrWhiteSpace(v) ? null : v;
        _canSave = true;
    }

    private void OnSortOrderChanged(ChangeEventArgs e)
    {
        if (int.TryParse(e.Value?.ToString(), out var v))
            _editSortOrder = v;

        _canSave = true;
    }

    private void OnPriorityChanged(ChangeEventArgs e)
    {
        if (int.TryParse(e.Value?.ToString(), out var v))
            _editPriority = v;

        _canSave = true;
    }

    private void OnTerminalChanged(ChangeEventArgs e)
    {
        _editIsTerminal = e.Value is bool b && b;
        _canSave = true;
    }

    //======================================================================
    // Transitions editing
    //======================================================================

    private void Toggle(Guid toId, bool enabled)
    {
        if (_selected is null) return;

        if (enabled)
        {
            _transitions[toId] = _transitions.TryGetValue(toId, out var cur) ? cur : 0;
        }
        else
        {
            _transitions.Remove(toId);
        }

        _canSave = true;
    }

    private void ChangeShift(Guid toId, string? raw)
    {
        if (_selected is null) return;
        if (!_transitions.ContainsKey(toId)) return;

        if (!int.TryParse(raw, out var shift))
            shift = 0;

        shift = shift switch { 0 => 0, 1 => 1, _ => 0 };
        _transitions[toId] = shift;

        _canSave = true;
    }

    private void SelectAllShift0()
    {
        if (_selected is null) return;

        _transitions = RightPanelTargets()
            .Where(x => x.IsActive)
            .ToDictionary(x => x.Id, _ => 0);

        _canSave = true;
    }

    private void ClearAll()
    {
        _transitions.Clear();
        _canSave = true;
    }

    //======================================================================
    // Save
    //======================================================================

    private async Task SaveAsync()
    {
        if (_selected is null) return;

        var title = (_editTitle ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(title))
        {
            Toasts.Warning("Назва коду не може бути порожньою.");
            return;
        }

        var cmd = new SaveTimesheetPolicyCommand(
            CodeId: _selected.Id,
            Title: title,
            Description: string.IsNullOrWhiteSpace(_editDescription) ? null : _editDescription.Trim(),
            SortOrder: _editSortOrder,
            Priority: _editPriority,
            IsTerminal: _editIsTerminal,
            AllowedTransitions: [.. _transitions.Select(kv => new TimesheetTransitionSpecDto(kv.Key, kv.Value))],
            Author: "ui", // TODO auth user
            NowUtc: DateTime.UtcNow
        );

        await SavePolicy.HandleAsync(cmd);

        // refresh left list (title/order/priority могли змінитись)
        _codes = [.. await GetCodes.HandleAsync(new GetTimesheetPolicyCodesQuery())];

        // refresh selected view (щоб показати реальні дані)
        await SelectAsync(_selected.Id);

        Toasts.Success("Збережено", $"Політика для {_selected.Code} оновлена.");
    }

    //======================================================================
    // Drawers (placeholders)
    //======================================================================

    private Task OpenCreateDrawer()
    {
        _createOpen = true;
        _closeOpen = false;
        return Task.CompletedTask;
    }

    private Task OpenCloseDrawer()
    {
        _closeTarget = _selected;
        _closeOpen = true;
        return Task.CompletedTask;
    }

    private async Task HandleCreated(Guid guid)
    {
        await LoadCodes();
        await SelectAsync(guid);
    }

    private async Task HandleClosedCode(Guid closedId)
    {
        _closeOpen = false;

        // refresh list
        _codes = [.. await GetCodes.HandleAsync(new GetTimesheetPolicyCodesQuery())];

        // якщо закрили поточний — можна або лишити в правій панелі,
        // або зняти selection (я б знімав, щоб не редагувати неактивний випадково)
        if (_selected?.Id == closedId)
            _selected = null;
    }
}