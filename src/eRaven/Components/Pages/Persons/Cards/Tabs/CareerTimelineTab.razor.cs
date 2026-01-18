//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CareerTimelineTab
//-----------------------------------------------------------------------------

using eRaven.Application.Commands;
using eRaven.Application.Commands.PersonMove;
using eRaven.Application.DTOs.Person;
using eRaven.Application.Queries;
using eRaven.Application.Queries.Personal;
using Microsoft.AspNetCore.Components;

namespace eRaven.Components.Pages.Persons.Cards.Tabs;

public partial class CareerTimelineTab
{
    // =========================
    // Parameters
    // =========================
    [Parameter] public Guid PersonId { get; set; }

    // =========================
    // DI
    // =========================
    [Inject] public IQueryHandler<GetPersonHistoryQuery, IReadOnlyList<PersonEventListItemDto>> HistoryQuery { get; set; } = default!;
    [Inject] public ICommandHandler<VoidPersonEventCommand> VoidHandler { get; set; } = default!;

    // =========================
    // State
    // =========================
    private bool _loading;
    private List<PersonEventListItemDto> _events = [];
    private PersonEventListItemDto? _selected;

    private bool _voidOpen;
    private Guid? _voidTargetEventId;
    private PersonEventListItemDto? _voidTarget;

    // =========================
    // Lifecycle
    // =========================
    protected override async Task OnParametersSetAsync()
    {
        if (PersonId == Guid.Empty)
        {
            ResetState();
            return;
        }

        await ReloadAsync();
    }

    // =========================
    // UI Actions
    // =========================
    private Task OnRowClick(PersonEventListItemDto e)
    {
        _selected = e;
        return Task.CompletedTask;
    }

    private void OpenVoid(PersonEventListItemDto e)
    {
        _voidTarget = e;
        _voidTargetEventId = e.EventId;
        _voidOpen = true;
    }

    private async Task HandleVoidAsync(VoidPersonEventDto dto)
    {
        if (PersonId == Guid.Empty)
            return;

        if (dto.TargetEventId == Guid.Empty)
            return;

        var cmd = new VoidPersonEventCommand(
            PersonId: PersonId,                      // ✅ з контексту таба
            TargetEventId: dto.TargetEventId,
            Reason: dto.Reason?.Trim() ?? string.Empty,
            Author: "system",
            NowUtc: DateTime.UtcNow
        );


        await VoidHandler.HandleAsync(cmd);

        CloseVoid();
        await ReloadAsync();
    }

    // =========================
    // Internals
    // =========================
    private async Task ReloadAsync()
    {
        _loading = true;
        try
        {
            var list = await HistoryQuery.HandleAsync(new GetPersonHistoryQuery(PersonId));
            _events = list?.OrderByDescending(x => x.Version).ToList() ?? [];

            if (_selected is not null)
                _selected = _events.FirstOrDefault(x => x.EventId == _selected.EventId);
        }
        finally
        {
            _loading = false;
        }
    }

    private void ResetState()
    {
        _events.Clear();
        _selected = null;
        CloseVoid();
    }

    private void CloseVoid()
    {
        _voidOpen = false;
        _voidTargetEventId = null;
        _voidTarget = null;
    }
}