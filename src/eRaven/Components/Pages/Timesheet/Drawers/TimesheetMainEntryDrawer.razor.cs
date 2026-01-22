//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetMainEntryDrawer
//-----------------------------------------------------------------------------

using eRaven.Application.Catalogs.Timesheet;
using eRaven.Application.DTOs.Timesheet;
using eRaven.Components.Shared.Drawer;
using eRaven.Domain.Enums;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace eRaven.Components.Pages.Timesheet.Drawers;

public partial class TimesheetMainEntryDrawer
{
    private const string CustomKey = "__custom__";
    private const string FormId = "ts-main-entry-form";

    [Inject] public ITimesheetStatusCatalog Catalog { get; set; } = default!;

    private Drawer? _drawer;

    private bool _busy;
    private string? _error;

    private IReadOnlyList<TimesheetStatusOption> _mainOptions = [];

    private EditContext _editContext = default!;
    private TimesheetMainEntryFormModel Model { get; set; } = new();

    private string _codePlaceholder = "";
    private CodeMode _codeMode = CodeMode.ReadOnly;

    [Parameter] public bool IsOpen { get; set; }
    [Parameter] public EventCallback<bool> IsOpenChanged { get; set; }

    [Parameter] public TimesheetPersonHeaderDto? Person { get; set; }
    [Parameter] public DateOnly? DefaultDate { get; set; }

    /// <summary>Сабміт форми (поки без інтеграції — батько сам викличе команду/хендлер).</summary>
    [Parameter] public EventCallback<CreateTimesheetMainEntryDto> OnSubmit { get; set; }

    /// <summary>Щоб батько міг обнулити Person після закриття.</summary>
    [Parameter] public EventCallback OnClosed { get; set; }

    private Dictionary<string, object?> SubmitAttrs => new()
    {
        ["disabled"] = _busy || Person is null
    };

    protected override void OnInitialized()
    {
        _mainOptions = [.. Catalog.GetAll().Where(o => o.Lane == TimesheetLane.Main)];

        _editContext = new EditContext(Model);
        ResetForm();
    }

    protected override void OnParametersSet()
    {
        // коли відкриваємось або змінюється Person — оновлюємо дефолти
        if (IsOpen)
            ResetForm();
    }

    private void ResetForm()
    {
        _error = null;
        _busy = false;

        if (Person is null)
            return;

        Model.PersonId = Person.PersonId;
        Model.From = DefaultDate ?? DateOnly.FromDateTime(DateTime.Today);
        Model.UsePeriod = false;
        Model.OpenEnded = false;
        Model.To = Model.From;

        // дефолтно — нічого не вибрано, код порожній
        Model.OptionKey = "";
        Model.Code = "";
        Model.Reference = "";
        Model.Note = "";

        _codeMode = CodeMode.ReadOnly;
        _codePlaceholder = "";

        _editContext = new EditContext(Model);
    }

    private void OnCodeOptionChanged(ChangeEventArgs e)
    {
        var key = Convert.ToString(e.Value) ?? "";
        Model.OptionKey = key;

        if (string.IsNullOrWhiteSpace(key))
        {
            Model.Code = "";
            _codeMode = CodeMode.ReadOnly;
            _codePlaceholder = "";
            _editContext.NotifyFieldChanged(new FieldIdentifier(Model, nameof(Model.Code)));
            return;
        }

        if (key == CustomKey)
        {
            Model.Code = "";
            _codeMode = CodeMode.Editable;
            _codePlaceholder = "Напр: 30, НБ, 100, ...";
            _editContext.NotifyFieldChanged(new FieldIdentifier(Model, nameof(Model.Code)));
            return;
        }

        var opt = _mainOptions.FirstOrDefault(x => x.Code == key);

        if (opt is null)
        {
            Model.Code = "";
            _codeMode = CodeMode.Editable;
            _codePlaceholder = "";
            _editContext.NotifyFieldChanged(new FieldIdentifier(Model, nameof(Model.Code)));
            return;
        }

        if (opt.IsCodeTemplate)
        {
            // шаблон — просимо ввести код, підказуємо шаблон
            Model.Code = "";
            _codeMode = CodeMode.Editable;
            _codePlaceholder = opt.Code;
        }
        else
        {
            // фіксований — ставимо код і робимо readonly
            Model.Code = opt.Code;
            _codeMode = CodeMode.ReadOnly;
            _codePlaceholder = "";
        }

        _editContext.NotifyFieldChanged(new FieldIdentifier(Model, nameof(Model.Code)));
    }

    private void OnOpenEndedChanged(ChangeEventArgs e)
    {
        Model.OpenEnded = Convert.ToBoolean(e.Value);
        if (Model.OpenEnded)
            Model.To = null;
        else if (Model.To is null)
            Model.To = Model.From;

        _editContext.NotifyFieldChanged(new FieldIdentifier(Model, nameof(Model.To)));
    }

    private async Task SubmitAsync()
    {
        if (Person is null)
            return;

        _error = null;

        if (!_editContext.Validate())
            return;

        _busy = true;

        try
        {
            var code = (Model.Code ?? "").Trim();

            // нормалізуємо діапазон
            DateOnly? to = Model.UsePeriod
                ? (Model.OpenEnded ? null : Model.To)
                : Model.From;

            var dto = new CreateTimesheetMainEntryDto(
                PersonId: Model.PersonId,
                From: Model.From,
                To: to,
                Code: code,
                Reference: string.IsNullOrWhiteSpace(Model.Reference) ? null : Model.Reference.Trim(),
                Note: string.IsNullOrWhiteSpace(Model.Note) ? null : Model.Note.Trim()
            );

            await OnSubmit.InvokeAsync(dto);

            await CloseAsync(); // після успіху закриваємо
        }
        catch (Exception ex)
        {
            _error = ex.Message;
        }
        finally
        {
            _busy = false;
        }
    }

    private async Task CloseAsync()
    {
        if (_drawer is not null)
            await _drawer.CloseAsync();
        else
            await IsOpenChanged.InvokeAsync(false);
    }

    private async Task HandleClosed()
        => await OnClosed.InvokeAsync();

    private sealed class TimesheetMainEntryFormModel
    {
        public Guid PersonId { get; set; }

        public string OptionKey { get; set; } = "";

        public DateOnly From { get; set; } = DateOnly.FromDateTime(DateTime.Today);
        public bool UsePeriod { get; set; }

        public DateOnly? To { get; set; }
        public bool OpenEnded { get; set; }

        public string Code { get; set; } = "";

        public string? Reference { get; set; }
        public string? Note { get; set; }
    }

    private enum CodeMode
    {
        ReadOnly = 0,
        Editable = 1
    }
}
