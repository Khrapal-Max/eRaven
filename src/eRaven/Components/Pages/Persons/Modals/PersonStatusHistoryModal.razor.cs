//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// PersonStatusHistoryModal (code-behind)
//-----------------------------------------------------------------------------

using System;
using System.Globalization;
using eRaven.Application.Services.PersonStatusReadService;
using eRaven.Domain.Models;
using Microsoft.AspNetCore.Components;

namespace eRaven.Components.Pages.Persons.Modals;

public sealed partial class PersonStatusHistoryModal : ComponentBase
{
    // Керування з батька
    [Parameter] public bool Open { get; set; }
    [Parameter] public Person? Person { get; set; }
    [Parameter] public EventCallback OnClose { get; set; }

    [Inject] private IPersonStatusReadService PersonStatusReadService { get; set; } = default!;

    private readonly List<HistoryRow> _view = [];
    private bool _busy;
    private string? _personName;

    protected override async Task OnParametersSetAsync()
    {
        if (Person is not null)
        {
            _personName = Person?.FullName;

            // Кожен раз, коли модал відкривають і відомий PersonId — оновлюємо список
            await LoadAsync();
        }
    }

    private async Task LoadAsync()
    {
        try
        {
            SetBusy(true);

            var personId = Person!.Id;

            var historyTask = PersonStatusReadService.OrderForHistoryAsync(personId);
            var firstPresenceTask = PersonStatusReadService.GetFirstPresenceUtcAsync(personId);
            var notPresentTask = PersonStatusReadService.ResolveNotPresentAsync();

            await Task.WhenAll(historyTask, firstPresenceTask, notPresentTask);

            var history = await historyTask;
            var firstPresenceUtc = await firstPresenceTask;
            var notPresentKind = await notPresentTask;
            var normalizedFirstPresenceUtc = firstPresenceUtc is { } fp ? EnsureUtc(fp) : (DateTime?)null;

            _view.Clear();

            if (notPresentKind is not null && normalizedFirstPresenceUtc is { } firstPresence)
            {
                var firstLocal = firstPresence.ToLocalTime();
                var statusLabel = ResolveStatusLabel(notPresentKind);
                var dateLabel = $"до {firstLocal:dd.MM.yyyy}";
                var note = string.Format(CultureInfo.CurrentCulture, "Відсутній до {0:dd.MM.yyyy}", firstLocal);

                _view.Add(new HistoryRow(Guid.Empty, statusLabel, dateLabel, note));
            }

            if (history is not null)
            {
                foreach (var status in history)
                {
                    var kindName = status.StatusKind?.Name ?? $"ID {status.StatusKindId}";
                    var openUtc = EnsureUtc(status.OpenDate);

                    var openLocal = openUtc.ToLocalTime();
                    var dateLabel = openLocal.ToString("dd.MM.yyyy", CultureInfo.CurrentCulture);

                    _view.Add(new HistoryRow(status.Id, kindName, dateLabel, status.Note));
                }
            }
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void SetBusy(bool value)
    {
        _busy = value;
        StateHasChanged();
    }

    private async Task CloseAsync()
        => await OnClose.InvokeAsync();

    private static DateTime EnsureUtc(DateTime value)
        => value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };

    private static string ResolveStatusLabel(StatusKind kind)
    {
        var code = string.IsNullOrWhiteSpace(kind.Code) ? null : kind.Code.Trim();
        if (!string.IsNullOrEmpty(code)) return code;

        if (!string.IsNullOrWhiteSpace(kind.Name))
            return kind.Name;

        return $"ID {kind.Id}";
    }

    private sealed record HistoryRow(Guid Id, string StatusLabel, string DateLabel, string? Note);
}
