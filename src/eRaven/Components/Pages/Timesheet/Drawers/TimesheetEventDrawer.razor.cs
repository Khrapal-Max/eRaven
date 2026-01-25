//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetEventDrawer
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.Timesheet;
using eRaven.Domain.Entities;
using eRaven.Domain.Enums;
using eRaven.Infrastructure.Repositories.TimesheetPolicyRepository;
using eRaven.Infrastructure.Repositories.TimesheetRepository;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System.ComponentModel.DataAnnotations;

namespace eRaven.Components.Pages.Timesheet.Drawers;

public partial class TimesheetEventDrawer
{
    [Parameter] public bool IsOpen { get; set; }
    [Parameter] public EventCallback<bool> IsOpenChanged { get; set; }

    [Parameter] public TimesheetPersonMonthRowDto? Person { get; set; }
    [Parameter] public TimesheetLane Lane { get; set; }
    [Parameter] public DateOnly Date { get; set; } // anchor date (from day/table)

    // NOTE: NEW payload
    [Parameter] public EventCallback<TimesheetTransitionCreateDto> OnSubmit { get; set; }

    [Inject] public ITimesheetPolicyRepository Policy { get; set; } = default!;
    [Inject] public ITimesheetTimelineRepository Timelines { get; set; } = default!;
    [Inject] public ITimesheetEntryRepository Entries { get; set; } = default!;

    private bool _busy;
    private bool _loading;
    private bool _wasOpen;

    private IReadOnlyList<TimesheetCodeDefinition> _allCodes = [];
    private IReadOnlyList<TimesheetCodeDefinition> _allowedCodes = [];

    private TimesheetTimeline? _timeline;
    private TimesheetEntry? _active;

    private TimesheetCodeDefinition? _activeDef;
    private TimesheetCodeDefinition? _nextDef;

    private DateOnly _computedPrevLastDay;
    private DateOnly _computedNextFrom;

    private EditContext _editContext = default!;
    protected TransitionModel Model { get; set; } = new();

    private bool DisabledSave =>
        _busy || _loading || Person is null || _timeline is null || _active is null || string.IsNullOrWhiteSpace(Model.NextCode);

    protected override void OnInitialized() => Reset();

    protected override async Task OnParametersSetAsync()
    {
        if (IsOpen && !_wasOpen)
        {
            _wasOpen = true;
            await LoadAsync();
            return;
        }

        if (!IsOpen && _wasOpen)
        {
            _wasOpen = false;
            Reset();
        }
    }

    private async Task LoadAsync()
    {
        Reset();

        if (Person is null)
            return;

        _loading = true;
        try
        {
            // base model
            Model.PersonId = Person.PersonId;
            Model.Lane = Lane;
            Model.AnchorDate = Date;

            _allCodes = await Policy.GetCodesAsync(Lane);

            _timeline = await Timelines.GetTimelineOnDateAsync(Person.PersonId, Lane, Date);
            _active = await Entries.GetActiveEntryOnDateAsync(Person.PersonId, Lane, Date);

            // No timeline or no active history => user canТt apply transition
            if (_timeline is null || _active is null)
            {
                _allowedCodes = [];
                Model.InputDate = Date;
                Model.NextCode = "";
                RecalcDates();
                _editContext = new EditContext(Model);
                return;
            }

            _activeDef = FindDef(_active.Code);
            _allowedCodes = await GetAllowedCodesAsync();

            // Default input: anchor date (interpreted by current state's meaning)
            Model.InputDate = Date;

            // Default next code: first allowed
            Model.NextCode = _allowedCodes.Count > 0 ? _allowedCodes[0].Code : "";
            _nextDef = FindDef(Model.NextCode);

            RecalcDates();

            _editContext = new EditContext(Model);
        }
        finally
        {
            _loading = false;
        }
    }

    private async Task<IReadOnlyList<TimesheetCodeDefinition>> GetAllowedCodesAsync()
    {
        if (_active is null || string.IsNullOrWhiteSpace(_active.Code))
            return _allCodes;

        var fromDef = FindDef(_active.Code);
        if (fromDef is null)
            return _allCodes;

        var allowedIds = await Policy.GetAllowedNextAsync(fromDef.Id);

        if (allowedIds.Count == 0)
            return _allCodes;

        return [.. _allCodes.Where(x => allowedIds.Contains(x.Id))];
    }

    private void OnInputDateChanged()
    {
        RecalcDates();
        _editContext.NotifyFieldChanged(new FieldIdentifier(Model, nameof(Model.InputDate)));
    }

    private void OnNextCodeChanged()
    {
        _nextDef = FindDef(Model.NextCode);
        _editContext.NotifyFieldChanged(new FieldIdentifier(Model, nameof(Model.NextCode)));
    }

    private void RecalcDates()
    {
        // Defaults even when no active (so UI stays stable)
        if (_activeDef is null)
        {
            _computedPrevLastDay = Model.InputDate;
            _computedNextFrom = Model.InputDate;
            return;
        }

        if (_activeDef.EndDateMeaning == TimesheetEndDateMeaning.LastDayOfThisCode)
        {
            _computedPrevLastDay = Model.InputDate;
            _computedNextFrom = Model.InputDate.AddDays(1);
        }
        else
        {
            _computedNextFrom = Model.InputDate;
            _computedPrevLastDay = Model.InputDate.AddDays(-1);
        }
    }

    private TimesheetCodeDefinition? FindDef(string? code)
    {
        var c = Normalize(code);
        if (c.Length == 0) return null;

        return _allCodes.FirstOrDefault(x => Normalize(x.Code) == c);
    }

    private async Task SubmitAsync()
    {
        if (_busy || _loading || Person is null)
            return;

        _busy = true;
        try
        {
            Model.NextCode = (Model.NextCode ?? "").Trim();
            Model.Reference = TrimOrNull(Model.Reference);
            Model.Note = TrimOrNull(Model.Note);

            if (OnSubmit.HasDelegate)
            {
                await OnSubmit.InvokeAsync(new TimesheetTransitionCreateDto(
                    PersonId: Model.PersonId,
                    Lane: Model.Lane,
                    AnchorDate: Model.AnchorDate,
                    InputDate: Model.InputDate,
                    NextCode: Model.NextCode,
                    Reference: Model.Reference,
                    Note: Model.Note
                ));
            }

            await IsOpenChanged.InvokeAsync(false);
        }
        finally
        {
            _busy = false;
        }
    }

    private async Task OnCancel()
    {
        if (_busy) return;
        await IsOpenChanged.InvokeAsync(false);
    }

    private Task OnDrawerClosed()
    {
        Reset();
        return Task.CompletedTask;
    }

    private void Reset()
    {
        _busy = false;
        _loading = false;

        _allCodes = [];
        _allowedCodes = [];

        _timeline = null;
        _active = null;

        _activeDef = null;
        _nextDef = null;

        Model = new TransitionModel
        {
            AnchorDate = Date,
            InputDate = Date,
            NextCode = ""
        };

        _computedPrevLastDay = Model.InputDate;
        _computedNextFrom = Model.InputDate;

        _editContext = new EditContext(Model);
    }

    private static string LaneTitle(TimesheetLane lane) => lane == TimesheetLane.Main ? "Main" : "Task";

    private static string Normalize(string? code) => (code ?? "").Trim().ToUpperInvariant();
    private static string? TrimOrNull(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    // -----------------------
    // Model with minimal validation
    // -----------------------
    public sealed class TransitionModel
    {
        public Guid PersonId { get; set; }
        public TimesheetLane Lane { get; set; }
        public DateOnly AnchorDate { get; set; }

        [Required]
        public DateOnly InputDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

        [Required(ErrorMessage = "ќбер≥ть наступний код.")]
        public string NextCode { get; set; } = string.Empty;

        public string? Reference { get; set; }
        public string? Note { get; set; }
    }
}