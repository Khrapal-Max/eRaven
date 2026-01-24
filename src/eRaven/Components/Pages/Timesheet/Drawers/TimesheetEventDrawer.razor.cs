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

namespace eRaven.Components.Pages.Timesheet.Drawers;

public partial class TimesheetEventDrawer
{
    [Parameter] public bool IsOpen { get; set; }
    [Parameter] public EventCallback<bool> IsOpenChanged { get; set; }

    [Parameter] public TimesheetPersonMonthRowDto? Person { get; set; }
    [Parameter] public TimesheetLane Lane { get; set; }
    [Parameter] public DateOnly Date { get; set; } // anchor date (from calendar)

    [Parameter] public EventCallback<TimesheetEntryCreateDto> OnSubmit { get; set; }

    [Inject] public ITimesheetPolicyRepository Policy { get; set; } = default!;
    [Inject] public ITimesheetTimelineRepository Timelines { get; set; } = default!;
    [Inject] public ITimesheetEntryRepository Entries { get; set; } = default!;

    private bool _busy;
    private bool _loading;
    private bool _wasOpen;

    private IReadOnlyList<TimesheetCodeDefinition> _allCodes = [];
    private IReadOnlyList<TimesheetCodeDefinition> _allowedCodes = [];
    private TimesheetCodeDefinition? _selectedDef;

    private TimesheetTimeline? _timeline;
    private TimesheetEntry? _active;

    private bool _isPeriod; // default false (period is rare)
    private bool DisabledSave =>
    _busy || _loading || Person is null || string.IsNullOrWhiteSpace(Model.Code);

    private EditContext _editContext = default!;
    protected TimesheetEntryCreateDto Model { get; set; } = new();

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
            // Base model
            Model.PersonId = Person.PersonId;
            Model.Lane = Lane;

            // Load code definitions
            _allCodes = await Policy.GetCodesAsync(Lane);

            // Always pull active entry (anchor)
            _timeline = await Timelines.GetTimelineOnDateAsync(Person.PersonId, Lane, Date);
            _active = await Entries.GetActiveEntryOnDateAsync(Person.PersonId, Lane, Date);

            // Start date: end of active (or anchor), with quick buttons to switch to active start
            if (_active is not null)
            {
                Model.From = _active.To ?? Date;
            }
            else
            {
                Model.From = Date;
            }

            // Period OFF by default
            _isPeriod = false;
            Model.To = null;

            // Allowed codes: based on active code transitions
            _allowedCodes = await GetAllowedCodesAsync();

            // Default selected code: first allowed (or empty)
            Model.Code = _allowedCodes[0].Code;
            _selectedDef = FindDef(Model.Code);

            _editContext = new EditContext(Model);
        }
        finally
        {
            _loading = false;
        }
    }

    private async Task<IReadOnlyList<TimesheetCodeDefinition>> GetAllowedCodesAsync()
    {
        // If no active entry or policy missing -> fallback to all codes (do not block UI)
        if (_active is null || string.IsNullOrWhiteSpace(_active.Code))
            return _allCodes;

        var fromDef = _allCodes.FirstOrDefault(x =>
            string.Equals(x.Code, _active.Code.Trim(), StringComparison.OrdinalIgnoreCase));

        if (fromDef is null)
            return _allCodes;

        var allowedIds = await Policy.GetAllowedNextAsync(fromDef.Id);

        // If transitions not configured -> fallback to all (so UI still works)
        if (allowedIds.Count == 0)
            return _allCodes;

        return [.. _allCodes.Where(x => allowedIds.Contains(x.Id))];
    }

    private void Reset()
    {
        _busy = false;
        _loading = false;

        _allCodes = [];
        _allowedCodes = [];
        _selectedDef = null;

        _timeline = null;
        _active = null;

        _isPeriod = false;

        Model = new TimesheetEntryCreateDto
        {
            From = DateOnly.FromDateTime(DateTime.Now),
            To = null
        };

        _editContext = new EditContext(Model);
    }

    private void OnPeriodChanged(ChangeEventArgs e)
    {
        var s = Convert.ToString(e.Value);
        var on = string.Equals(s, "true", StringComparison.OrdinalIgnoreCase)
            || string.Equals(s, "on", StringComparison.OrdinalIgnoreCase)
            || string.Equals(s, "1", StringComparison.OrdinalIgnoreCase);

        _isPeriod = on;

        if (_isPeriod)
            Model.To ??= Model.From;
        else
            Model.To = null;

        _editContext.NotifyFieldChanged(new FieldIdentifier(Model, nameof(Model.To)));
    }

    private void OnCodeChanged()
    {
        _selectedDef = FindDef(Model.Code);
        _editContext.NotifyFieldChanged(new FieldIdentifier(Model, nameof(Model.Code)));
    }

    private TimesheetCodeDefinition? FindDef(string? code)
    {
        var c = (code ?? string.Empty).Trim();
        if (c.Length == 0) return null;

        return _allCodes.FirstOrDefault(x =>
            string.Equals(x.Code, c, StringComparison.OrdinalIgnoreCase));
    }

    private async Task SubmitAsync()
    {
        if (_busy || _loading || Person is null)
            return;

        _busy = true;
        try
        {
            Model.Code = (Model.Code ?? string.Empty).Trim();
            Model.Reference = TrimOrNull(Model.Reference);
            Model.Note = TrimOrNull(Model.Note);

            if (OnSubmit.HasDelegate)
            {
                await OnSubmit.InvokeAsync(new TimesheetEntryCreateDto
                {
                    PersonId = Model.PersonId,
                    Lane = Model.Lane,
                    From = Model.From,
                    To = Model.To, // user-entered end date (policy applies +/-1 in handler)
                    Code = Model.Code,
                    Reference = Model.Reference,
                    Note = Model.Note
                });
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

    private static string LaneTitle(TimesheetLane lane)
        => lane == TimesheetLane.Main ? "Main" : "Task";

    private static string? TrimOrNull(string? s)
        => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
