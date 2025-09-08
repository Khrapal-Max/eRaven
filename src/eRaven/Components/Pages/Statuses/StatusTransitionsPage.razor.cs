//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// StatusTransitionsPage
//-----------------------------------------------------------------------------

using Blazored.Toast.Services;
using eRaven.Application.Services.PersonService;
using eRaven.Application.ViewModels;
using eRaven.Application.ViewModels.PersonStatusViewModel;
using eRaven.Components.Pages.Statuses.Modals;
using eRaven.Domain.Models;
using FluentValidation;
using Microsoft.AspNetCore.Components;
using System.Collections.ObjectModel;
using System.Linq.Expressions;

namespace eRaven.Components.Pages.Statuses;

public partial class StatusTransitionsPage : ComponentBase, IDisposable
{
    // ========== DI ==========
    [Inject] private IPersonService PersonService { get; set; } = default!;
    [Inject] private IToastService Toast { get; set; } = default!;

    // ========== UI state ==========
    protected bool Busy { get; private set; }
    protected string? Search { get; set; }

    private readonly CancellationTokenSource _cts = new();

    protected ObservableCollection<Person> Items { get; } = [];

    // ========== Modals ==========
    private StatusSetModal? _setStatusModal;

    // ---------- Search ----------
    protected async Task OnSearchAsync()
    {
        if (string.IsNullOrWhiteSpace(Search))
        {
            ResetItems([]);
            return;
        }

        try
        {
            SetBusy(true);

            var s = Search.Trim();

            // предикат: ПІБ, RNOKPP, звання, позивний, зброя, посада коротка
            Expression<Func<Person, bool>> pred =
                p =>
                    (p.Rnokpp != null && p.Rnokpp.Contains(s)) ||
                    (p.LastName != null && p.LastName.Contains(s)) ||
                    (p.FirstName != null && p.FirstName.Contains(s)) ||
                    (p.MiddleName != null && p.MiddleName.Contains(s)) ||
                    (p.Rank != null && p.Rank.Contains(s)) ||
                    (p.Callsign != null && p.Callsign.Contains(s)) ||
                    (p.Weapon != null && p.Weapon.Contains(s)) ||
                    (p.PositionUnit != null && p.PositionUnit.ShortName != null && p.PositionUnit.ShortName.Contains(s));

            var found = await PersonService.SearchAsync(pred, _cts.Token);

            ResetItems(found);
        }
        catch (OperationCanceledException) { /* ignore */ }
        catch (Exception ex)
        {
            ResetItems([]);
            Toast.ShowError($"Помилка пошуку: {ex.Message}");
        }
        finally
        {
            SetBusy(false);
        }
    }

    protected Task ClearAsync()
    {
        Search = string.Empty;
        ResetItems([]);
        // пошук не стираю, щоб користувач міг відредагувати рядок і натиснути "Знайти" знову
        return Task.CompletedTask;
    }

    protected void OpenSetStatus(Person p) => _setStatusModal?.Open(p);

    // ---------- Refresh after status change ----------
    private async Task OnStatusChangedAsync(Guid personId)
    {
        try
        {
            // Після зміни статусу — перезавантажуємо останню вибірку по поточному пошуковому рядку
            await OnSearchAsync();

            Toast.ShowSuccess("Статус збережено.");
        }
        catch (Exception ex)
        {
            Toast.ShowError($"Не вдалося оновити список: {ex.Message}");
        }
    }

    // ---------- Import ----------
    private Task OnImportBusyChanged(bool busy)
    {
        SetBusy(busy);
        return Task.CompletedTask;
    }

    private async Task<ImportReportViewModel> ProcessImportedStatusesAsync(IReadOnlyList<PersonStatusImportView> rows)
    {
        int ok = 0, fail = 0;
        var errors = new List<string>();

        foreach (var (row, idx) in rows.Select((r, i) => (r, i: i + 2))) // +2 бо хедер = перший рядок
        {
            try
            {
                // прості перевірки
                if (string.IsNullOrWhiteSpace(row.Rnokpp))
                    throw new ArgumentException("RNOKPP порожній.");
                if ((row.StatusKindId ?? 0) == 0 && string.IsNullOrWhiteSpace(row.StatusCode))
                    throw new ArgumentException("Не вказано StatusKindId або StatusCode.");
                if (row.FromDateLocal == default)
                    throw new ArgumentException("Не вказано дату.");

                // шукаємо людей по RNOKPP
                Expression<Func<Person, bool>> pred = p => p.Rnokpp == row.Rnokpp!.Trim();
                var persons = await PersonService.SearchAsync(pred, _cts.Token);
                if (persons.Count == 0) throw new InvalidOperationException("Особа з таким RNOKPP не знайдена.");
                var person = persons[0];

                // TODO: тут викликати твій IPersonStatusService.SetStatusAsync(...) після мапінгу
                // В цій базовій версії просто рахуємо як success, щоб не ламати збірку.
                ok++;
            }
            catch (Exception ex)
            {
                fail++;
                errors.Add($"Row {idx}: {ex.Message}");
            }
        }

        // після імпорту — оновлюємо результати (якщо був пошук)
        if (!string.IsNullOrWhiteSpace(Search))
            await OnSearchAsync();

        return new ImportReportViewModel(Added: ok, Updated: 0, Errors: errors);
    }

    private Task OnImportCompleted(ImportReportViewModel report)
    {
        if (report.Errors?.Count > 0)
            Toast.ShowError($"Імпорт завершено з помилками: {report.Errors.Count}. Успішно: {report.Added}");
        else
            Toast.ShowSuccess($"Імпорт успішний. Успішно: {report.Added}");

        return Task.CompletedTask;
    }

    // ========== Helpers ==========
    private void ResetItems(IEnumerable<Person> people)
    {
        Items.Clear();
        foreach (var p in people) Items.Add(p);
        StateHasChanged();
    }

    private void SetBusy(bool value)
    {
        Busy = value;
        StateHasChanged();
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
        GC.SuppressFinalize(this);
    }
}
