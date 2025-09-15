/*// -----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
// -----------------------------------------------------------------------------
// AddBatchModal
// -----------------------------------------------------------------------------

using eRaven.Application.ViewModels.PlanViewModels;
using eRaven.Domain.Enums;
using eRaven.Domain.Models;
using eRaven.Infrastructure;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Components.Pages.Plans.Modals;

public partial class AddBatchModal : ComponentBase
{
    [Inject] private IDbContextFactory<AppDbContext> DbFactory { get; set; } = default!;
    [Inject] private eRaven.Application.Services.PlanService.IPlanService PlanService { get; set; } = default!;

    public sealed class Context
    {
        public string PlanNumber { get; set; } = default!;
    }

    public sealed class Row
    {
        public Guid? PersonId { get; set; }
        public PlanActionType ActionType { get; set; } = PlanActionType.Dispatch;
        public DateTime WhenLocal { get; set; } = DateTime.Now;
        public string Location { get; set; } = "";
        public string Group { get; set; } = "";
        public string Crew { get; set; } = "";
        public string? Note { get; set; }
    }

    [Parameter] public EventCallback<int> OnSaved { get; set; }

    private bool _open;
    private bool _busy;
    private string _planNumber = "";
    private string? _error;

    private List<Person> _people = [];
    private List<Row> _rows = [];

    public async Task OpenAsync(Context ctx)
    {
        _error = null;
        _busy = false;
        _open = true;
        _planNumber = ctx.PlanNumber;

        await using var db = await DbFactory.CreateDbContextAsync();
        _people = await db.Persons.AsNoTracking()
                  .OrderBy(p => p.LastName).ThenBy(p => p.FirstName)
                  .ToListAsync();

        _rows.Clear();
        _rows.Add(new Row()); // один порожній рядок на старті
        StateHasChanged();
    }

    private void AddRow() { _rows.Add(new Row()); }
    private void RemoveRow(int i) { if (i >= 0 && i < _rows.Count) _rows.RemoveAt(i); }

    protected bool IsDisabled
        => _busy || string.IsNullOrWhiteSpace(_planNumber) || _rows.Count == 0
            || _rows.Any(r => r.PersonId is null
                              || string.IsNullOrWhiteSpace(r.Location)
                              || string.IsNullOrWhiteSpace(r.Group)
                              || string.IsNullOrWhiteSpace(r.Crew));

    private void Cancel()
    {
        _open = false;
        StateHasChanged();
    }

    private async Task Save()
    {
        _error = null;

        try
        {
            _busy = true;

            var vm = new PlanBatchViewModel
            {
                PlanNumber = _planNumber,
                Actions = [.. _rows.Select(r => new PlanActionViewModel
                {
                    PlanNumber = _planNumber,
                    PersonId = r.PersonId!.Value,
                    ActionType = r.ActionType,
                    EventAtUtc = DateTime.SpecifyKind(r.WhenLocal, DateTimeKind.Local),
                    Location = r.Location.Trim(),
                    GroupName = r.Group.Trim(),
                    CrewName = r.Crew.Trim(),
                    Note = r.Note?.Trim()
                })]
            };

            await PlanService.ApplyBatchAsync(vm, author: "ui");
            var saved = vm.Actions.Count;

            _open = false;
            await OnSaved.InvokeAsync(saved);
        }
        catch (Exception ex)
        {
            _error = ex.Message;
        }
        finally
        {
            _busy = false;
            StateHasChanged();
        }
    }
}
*/