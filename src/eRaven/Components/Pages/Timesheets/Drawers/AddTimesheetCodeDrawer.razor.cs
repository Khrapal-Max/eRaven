//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// AddTimesheetCodeDrawer
//-----------------------------------------------------------------------------

using eRaven.Application.Commands;
using eRaven.Application.Commands.Timesheets;
using eRaven.Application.DTOs.Enums;
using eRaven.Application.DTOs.Timesheets.Models;
using eRaven.Application.DTOs.Timesheets.Policy;
using eRaven.Application.Queries;
using eRaven.Application.Queries.Timesheets;
using eRaven.Components.Shared.Drawer;
using eRaven.Presentation.Toasts;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace eRaven.Components.Pages.Timesheets.Drawers;

public partial class AddTimesheetCodeDrawer : ComponentBase
{
    //======================================================================
    // DI
    //======================================================================

    [Inject] public ICommandHandler<AddTimesheetCodeCommand, Guid> AddTimesheetCodeCommandHandler { get; set; } = default!;
    [Inject] public ICommandHandler<SaveTimesheetPolicyCommand> SaveTimesheetPolicyCommandHandler { get; set; } = default!;
    [Inject] public IQueryHandler<GetTimesheetPolicyCodesQuery, IReadOnlyList<TimesheetCodeDto>> GetTimesheetPolicyCodesQueryHandler { get; set; } = default!;
    [Inject] public ToastService ToastService { get; set; } = default!;

    //======================================================================
    // Params
    //======================================================================

    [Parameter] public bool IsOpen { get; set; }
    [Parameter] public EventCallback<bool> IsOpenChanged { get; set; }

    /// <summary>Повертаємо Id створеного коду (щоб батько оновив список і/або вибрав його).</summary>
    [Parameter] public EventCallback<Guid> OnCreated { get; set; }

    // Опційно: дефолтні значення для зручності
    [Parameter] public int DefaultSortOrder { get; set; } = 0;
    [Parameter] public int DefaultPriority { get; set; } = 0;

    //======================================================================
    // State
    //======================================================================

    private Drawer? _drawer;

    private AddTimesheetCodeModel _model = new();
    private EditContext _editContext = default!;

    private bool _busy;
    private bool _wasOpen;

    //======================================================================
    // Lifecycle
    //======================================================================

    protected override void OnInitialized()
    {
        ResetForm();
    }

    protected override void OnParametersSet()
    {
        // коли відкрили — ініціалізуємо “чисту” форму
        if (IsOpen && !_wasOpen)
            ResetForm();

        _wasOpen = IsOpen;
    }

    //======================================================================
    // Actions
    //======================================================================

    private async Task CreateAsync()
    {
        if (_busy) return;

        // валідація UI
        if (!_editContext.Validate())
            return;

        var code = (_model.Code ?? string.Empty).Trim();
        var title = (_model.Title ?? string.Empty).Trim();
        var desc = string.IsNullOrWhiteSpace(_model.Description) ? null : _model.Description.Trim();

        if (_model.RoleCode == RoleCodeDto.SystemCode)
        {
            ToastService.Warning("SystemCode не може бути створений вручну.");
            return;
        }

        _busy = true;

        var nowUtc = DateTime.UtcNow;

        try
        {
            var id = await AddTimesheetCodeCommandHandler.HandleAsync(new AddTimesheetCodeCommand(
                Code: code,
                Title: title,
                Description: desc,
                SortOrder: _model.SortOrder,
                Priority: _model.Priority,
                IsTerminal: _model.IsTerminal,
                RoleCode: _model.RoleCode,
                UiStyle: _model.UiStyle,
                Author: "ui",           // TODO: auth user
                NowUtc: nowUtc // або передати з батька
            ));

            // Ініціалізуємо policy для нового транзитного коду:
            // додаємо глобальні (EmergencyCode) переходи за замовченням.
            // Це “захищає оператора” — аварійні коди мають працювати з будь-якого стану.
            if (_model.RoleCode == RoleCodeDto.TransitionCode)
                await EnsureGlobalTransitionsForNewCodeAsync(id, title, desc, nowUtc);

            ToastService.Success("Створено", $"Додано код {code}.");

            if (OnCreated.HasDelegate)
                await OnCreated.InvokeAsync(id);

            await IsOpenChanged.InvokeAsync(false);
        }
        catch (Exception ex)
        {
            ToastService.Error("Не вдалося створити код", ex.Message);
            await InvokeAsync(ResetForm);
        }
        finally
        {
            _busy = false;
        }
    }

    private async Task EnsureGlobalTransitionsForNewCodeAsync(
        Guid newCodeId,
        string title,
        string? description,
        DateTime nowUtc)
    {
        // Беремо всі коди довідника, виділяємо активні EmergencyCode.
        var codes = await GetTimesheetPolicyCodesQueryHandler.HandleAsync(new GetTimesheetPolicyCodesQuery());

        var globals = codes
            .Where(x => x.IsActive)
            .Where(x => x.RoleCode == RoleCodeDto.EmergencyCode)
            .Where(x => x.Id != newCodeId)
            .Select(x => new TimesheetTransitionSpecDto(x.Id, 0))
            .ToList();

        if (globals.Count == 0)
            return;

        try
        {
            await SaveTimesheetPolicyCommandHandler.HandleAsync(new SaveTimesheetPolicyCommand(
                CodeId: newCodeId,
                Title: title,
                Description: description,
                SortOrder: _model.SortOrder,
                Priority: _model.Priority,
                IsTerminal: _model.IsTerminal,
                AllowedTransitions: globals,
                Author: "ui",
                NowUtc: nowUtc
            ));
        }
        catch (Exception ex)
        {
            // Код створено, але policy не ініціалізовано. Це не критична помилка для UI.
            ToastService.Warning("Створено, але політика не ініціалізована", ex.Message);
        }
    }

    private async Task CloseAsync()
    {
        if (_drawer is not null)
            await _drawer.CloseAsync();
    }

    private Task OnClosedAsync()
    {
        ResetForm();
        return Task.CompletedTask;
    }

    //======================================================================
    // Helpers
    //======================================================================

    private void ResetForm()
    {
        _model = new AddTimesheetCodeModel
        {
            Code = string.Empty,
            Title = string.Empty,
            Description = null,
            SortOrder = DefaultSortOrder,
            Priority = DefaultPriority,
            IsTerminal = false,
            RoleCode = RoleCodeDto.TransitionCode,
            UiStyle = TimesheetUiStyleDto.Warning
        };

        _editContext = new EditContext(_model);
        _busy = false;
    }
}
