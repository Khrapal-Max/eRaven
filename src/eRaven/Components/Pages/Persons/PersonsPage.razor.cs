//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonsPage
//-----------------------------------------------------------------------------

using Blazored.Toast.Services;
using eRaven.Application.Services.PersonService;
using eRaven.Application.ViewModels;
using eRaven.Application.ViewModels.PersonViewModels;
using eRaven.Components.Pages.Persons.Modals;
using eRaven.Domain.Models;
using FluentValidation;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Net.Http;

namespace eRaven.Components.Pages.Persons;

public partial class PersonsPage : ComponentBase, IDisposable
{
    // =================== DI ===================
    [Inject] protected IPersonService PersonService { get; set; } = default!;
    [Inject] protected IToastService Toast { get; set; } = default!;
    [Inject] protected IValidator<CreatePersonViewModel> CreateValidator { get; set; } = default!;
    [Inject] protected ILogger<PersonsPage> Logger { get; set; } = default!;

    // ================= UI state =================
    protected bool Busy { get; private set; }
    protected string? Search { get; set; }
    protected Person? Selected { get; set; }

    private readonly CancellationTokenSource _cts = new();

    // ================= Modals =================
    private PersonCreateModal? _createModal;

    // Джерело даних
    private List<Person> _all = [];
    protected ObservableCollection<Person> Items { get; private set; } = [];

    // ================= Lifecycle =================
    protected override async Task OnInitializedAsync() => await ReloadAsync();

    // ================= Handlers =================
    protected async Task ReloadAsync()
    {
        try
        {
            await SetBusyAsync(true);
            _all = [.. await PersonService.SearchAsync(null, _cts.Token)];
            await ApplyFilterAsync();
        }
        catch (Exception ex)
        {
            if (!TryHandleKnownException(ex, "Не вдалося завантажити картки"))
            {
                throw;
            }
        }
        finally { await SetBusyAsync(false); }
    }

    protected Task OnSearchAsync() => ApplyFilterAsync();

    protected void OnRowClick(Person p) => Selected = p;

    protected Task CreateAsync()
    {
        _createModal?.Open();
        return Task.CompletedTask;
    }

    private async Task OnCreatedAsync(Person created)
    {
        // Найпростіше — перевантажити весь список
        await ReloadAsync();
    }

    // -------- ЕКСПОРТ --------
    protected Task OnExportBusyChanged(bool busy) => SetBusyAsync(busy);

    // -------- ІМПОРТ --------
    protected Task OnImportBusyChanged(bool busy) => SetBusyAsync(busy);

    /// <summary>
    /// Обробка імпортованих рядків.
    /// 1) Валідуємо кожен CreatePersonViewModel через FluentValidation.
    /// 2) Нормалізуємо -> мапимо у Person.
    /// 3) Upsert по RNOKPP: якщо існує — оновити дозволені поля; якщо ні — створити.
    /// </summary>
    private async Task<ImportReportViewModel> ProcessImportedAsync(IReadOnlyList<CreatePersonViewModel> rows)
    {
        Toast.ShowInfo("Виконується імпорт");

        int added = 0, updated = 0;
        var errors = new List<string>();

        foreach (var (vm, idx) in rows.Select((vm, i) => (vm, i: i + 2))) // +2 бо заголовок у першому рядку
        {
            // 1) Валідація
            var res = await CreateValidator.ValidateAsync(vm, _cts.Token);
            if (!res.IsValid)
            {
                foreach (var e in res.Errors)
                    errors.Add($"Row {idx}: {e.ErrorMessage}");
                continue;
            }

            try
            {
                // 2) Нормалізація + мапінг
                var entity = PersonUi.ToPerson(vm);

                // 3) Upsert по RNOKPP
                Expression<Func<Person, bool>> pred = p => p.Rnokpp == entity.Rnokpp;
                var exists = await PersonService.SearchAsync(pred, _cts.Token);

                if (exists.Count == 0)
                {
                    await PersonService.CreateAsync(entity, _cts.Token);
                    added++;
                }
                else
                {
                    var current = exists[0];

                    // Оновлюємо тільки дозволені з картки поля (як у PersonService.UpdateAsync)
                    current.LastName = entity.LastName;
                    current.FirstName = entity.FirstName;
                    current.MiddleName = entity.MiddleName;
                    current.Rnokpp = entity.Rnokpp;
                    current.Rank = entity.Rank;
                    current.Callsign = entity.Callsign;
                    current.BZVP = entity.BZVP;
                    current.Weapon = entity.Weapon;

                    current.IsAttached = entity.IsAttached;
                    current.AttachedFromUnit = entity.AttachedFromUnit;

                    var ok = await PersonService.UpdateAsync(current, _cts.Token);
                    if (ok) updated++;
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (FluentValidation.ValidationException ex)
            {
                errors.Add($"Row {idx}: {ex.Message}");
            }
            catch (System.ComponentModel.DataAnnotations.ValidationException ex)
            {
                errors.Add($"Row {idx}: {ex.Message}");
            }
            catch (InvalidOperationException ex)
            {
                errors.Add($"Row {idx}: {ex.Message}");
            }
            catch (ArgumentException ex)
            {
                errors.Add($"Row {idx}: {ex.Message}");
            }
            catch (HttpRequestException ex)
            {
                errors.Add($"Row {idx}: {ex.Message}");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Unexpected error while importing person row {RowIndex}", idx);
                throw;
            }
        }

        // Після імпорту — оновлюємо список
        await ReloadAsync();

        return new ImportReportViewModel(added, updated, errors);
    }

    protected Task OnImportCompleted(ImportReportViewModel report)
    {
        if (report.Errors?.Count > 0)
        {
            Toast.ShowError($"Імпорт завершено з помилками: {report.Errors.Count}. Додано: {report.Added}, Оновлено: {report.Updated}");
        }
        else
        {
            Toast.ShowSuccess($"Імпорт успішний. Додано: {report.Added}, Оновлено: {report.Updated}");
        }
        return Task.CompletedTask;
    }

    // ================= Helpers =================
    private async Task ApplyFilterAsync()
    {
        IEnumerable<Person> query = _all;
        var term = Search?.Trim();

        if (!string.IsNullOrWhiteSpace(term))
        {
            query = query.Where(p =>
                (p.FullName?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (p.Rnokpp?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (p.Rank?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (p.Callsign?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (p.PositionUnit?.ShortName?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        var next = query.ToList();
        var listChanged = !SamePersons(Items, next);
        var hadSelection = Selected is not null;

        if (!listChanged && !hadSelection)
        {
            return;
        }

        if (listChanged)
        {
            Items.Clear();
            foreach (var i in next) Items.Add(i);
        }

        if (hadSelection)
        {
            Selected = null;
        }

        await InvokeAsync(StateHasChanged);
    }

    private async Task SetBusyAsync(bool value)
    {
        if (Busy == value)
        {
            return;
        }

        Busy = value;
        await InvokeAsync(StateHasChanged);
    }

    private static bool SamePersons(IReadOnlyList<Person> current, IReadOnlyList<Person> next)
    {
        if (current.Count != next.Count) return false;

        for (var i = 0; i < current.Count; i++)
        {
            if (current[i].Id != next[i].Id)
            {
                return false;
            }

            if (!ReferenceEquals(current[i], next[i]))
            {
                return false;
            }
        }

        return true;
    }

    private static string Trunc(string? s, int max)
       => string.IsNullOrEmpty(s) || s.Length <= max ? (s ?? string.Empty)
                                                     : string.Concat(s.AsSpan(0, max - 1), "…");

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
        GC.SuppressFinalize(this);
    }

    private bool TryHandleKnownException(Exception ex, string message)
    {
        switch (ex)
        {
            case OperationCanceledException:
                return false;
            case System.ComponentModel.DataAnnotations.ValidationException:
            case FluentValidation.ValidationException:
            case InvalidOperationException:
            case ArgumentException:
            case HttpRequestException:
                Toast.ShowError($"{message}: {ex.Message}");
                return true;
            default:
                Logger.LogError(ex, "Unexpected error: {Context}", message);
                return false;
        }
    }
}
