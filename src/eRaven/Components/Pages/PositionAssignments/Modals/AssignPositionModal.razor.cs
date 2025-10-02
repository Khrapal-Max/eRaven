/*//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// AssignPositionModal
//-----------------------------------------------------------------------------

using Blazored.FluentValidation;
using Blazored.Toast.Services;
using eRaven.Domain.Models;
using Microsoft.AspNetCore.Components;

namespace eRaven.Components.Pages.PositionAssignments.Modals;

public partial class AssignPositionModal : ComponentBase
{
    [Parameter] public IReadOnlyList<PositionUnit> Positions { get; set; } = [];
    [Parameter] public EventCallback<PersonPositionAssignment> OnAssigned { get; set; }

    [Inject] public IPositionAssignmentService PositionAssignmentService { get; set; } = default!;
    [Inject] public IToastService ToastService { get; set; } = default!;

    private bool IsOpen { get; set; }
    private bool _busy;
    private FluentValidationValidator? _validator;

    private Person? _person;
    private string _date = string.Empty; // yyyy-MM-dd

    /// <summary>Якщо людина ВЖЕ на посаді — модал закриє її автоматично (close = open − 1 день).</summary>
    private PersonPositionAssignment? _activeAssign;

    /// <summary>Остання дата зняття з посади (UTC 00:00:00) для валід. мінімуму, якщо є.</summary>
    private DateTime? _lastUnassignCloseUtc;
    private string? _minDateAttr;

    private AssignViewModel Model { get; set; } = new();

    /// <summary>
    /// Відкрити модал: можна передати активне призначення (для авто-закриття) і останній closeUtc (для мінімальної дати).
    /// </summary>
    public void Open(Person person, PersonPositionAssignment? activeAssign = null, DateTime? lastUnassignCloseUtc = null)
    {
        _person = person ?? throw new ArgumentNullException(nameof(person));
        _activeAssign = activeAssign;

        _lastUnassignCloseUtc = lastUnassignCloseUtc.HasValue
            ? new DateTime(lastUnassignCloseUtc.Value.Year, lastUnassignCloseUtc.Value.Month, lastUnassignCloseUtc.Value.Day, 0, 0, 0, DateTimeKind.Utc)
            : null;

        // Мінімально дозволена дата:
        // - якщо є активне призначення: не раніше (active.OpenUtc + 1 день)
        // - інакше, якщо є останнє closeUtc: не раніше (closeUtc + 1 день)
        // - інакше — без мінімуму
        DateTime? minByActive = _activeAssign is null ? null : _activeAssign.OpenUtc.Date.AddDays(1);
        DateTime? minByClose = _lastUnassignCloseUtc?.AddDays(1);

        var minDate = (minByActive, minByClose) switch
        {
            (DateTime a, DateTime b) => (a > b ? a : b),
            (DateTime a, null) => a,
            (null, DateTime b) => b,
            _ => (DateTime?)null
        };

        _minDateAttr = minDate?.ToString("yyyy-MM-dd");

        // Дата за замовчуванням = max(minDate, сьогодні UTC)
        var def = DateTime.UtcNow.Date;
        if (minDate.HasValue && minDate.Value > def) def = minDate.Value;
        _date = def.ToString("yyyy-MM-dd");

        Model = new AssignViewModel
        {
            PersonId = person.Id,
            PositionUnitId = Guid.Empty,
            Note = null
        };

        IsOpen = true;
        StateHasChanged();
    }

    private void OnDateChanged(ChangeEventArgs e)
        => _date = Convert.ToString(e.Value) ?? string.Empty;

    private async Task OnSubmit()
    {
        if (_person is null) return;

        try
        {
            _busy = true;

            var openUtc = BuildUtcOrThrow();

            // Додаткова перевірка (ще до викликів сервісів)
            if (_activeAssign is not null)
            {
                var minAllowed = _activeAssign.OpenUtc.Date.AddDays(1);
                if (openUtc < minAllowed)
                {
                    ToastService.ShowInfo("Дата нового призначення повинна бути не раніше, ніж наступний день після початку попередньої посади.");
                    return;
                }
            }
            if (_lastUnassignCloseUtc.HasValue && openUtc <= _lastUnassignCloseUtc.Value)
            {
                ToastService.ShowInfo("Дата призначення має бути пізнішою за останню дату зняття з посади.");
                return;
            }

            // Якщо є активна посада — закриваємо її датою (open − 1 день)
            if (_activeAssign is not null)
            {
                var closeUtc = openUtc.AddDays(-1); // 00:00:00 попереднього дня
                if (closeUtc < _activeAssign.OpenUtc.Date)
                {
                    ToastService.ShowInfo("Обрана дата занадто рання для коректного закриття поточної посади.");
                    return;
                }

                await PositionAssignmentService.UnassignAsync(
                    _person.Id,
                    closeUtc,
                    null,
                    default);
            }

            // Нове призначення
            var created = await PositionAssignmentService.AssignAsync(
                _person.Id,
                Model.PositionUnitId,
                openUtc,
                string.IsNullOrWhiteSpace(Model.Note) ? null : Model.Note!.Trim(),
                default);

            await OnAssigned.InvokeAsync(created);
            Close();
        }
        catch (Exception ex)
        {
            ToastService.ShowError(ex.Message);
        }
        finally { _busy = false; }
    }

    private static string Trunc(string? s, int max)
       => string.IsNullOrEmpty(s) || s.Length <= max ? (s ?? string.Empty)
                                                     : string.Concat(s.AsSpan(0, max - 1), "…");

    private DateTime BuildUtcOrThrow()
    {
        if (string.IsNullOrWhiteSpace(_date) || !DateOnly.TryParse(_date, out var d))
            throw new InvalidOperationException("Оберіть коректну дату.");

        // Призначення фіксуємо на 00:00:00 UTC обраного дня
        return new DateTime(d.Year, d.Month, d.Day, 0, 0, 0, DateTimeKind.Utc);
    }

    private void Close() => IsOpen = false;
}
*/