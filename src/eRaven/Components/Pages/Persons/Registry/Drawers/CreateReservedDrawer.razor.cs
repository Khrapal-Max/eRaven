//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CreateReservedDrawer
//-----------------------------------------------------------------------------

using eRaven.Application.Catalogs.Ranks;
using eRaven.Application.DTOs.Person;
using eRaven.Presentation.Toasts;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace eRaven.Components.Pages.Persons.Registry.Drawers;

public partial class CreateReservedDrawer
{
    // =========================
    // Parameters
    // =========================

    [Parameter] public bool IsOpen { get; set; }
    [Parameter] public EventCallback<bool> IsOpenChanged { get; set; }
    [Parameter] public EventCallback<CreateReservedDto> OnCreate { get; set; }

    // =========================
    // DI
    // =========================

    [Inject] public ToastService Toasts { get; set; } = default!;
    [Inject] public IRankCatalog RankCatalog { get; set; } = default!;

    // =========================
    // State
    // =========================

    private bool _busy;
    private bool _wasOpen;

    private EditContext _editContext = default!;
    private IReadOnlyList<RankOption> _ranks = [];

    protected CreateReservedDto Model { get; set; } = new();

    // =========================
    // Lifecycle
    // =========================

    protected override void OnInitialized()
    {
        Reset(reloadRanks: false);
    }

    protected override Task OnParametersSetAsync()
    {
        // Open transition
        if (IsOpen && !_wasOpen)
        {
            _wasOpen = true;
            Reset(reloadRanks: true);
            return Task.CompletedTask;
        }

        // Close transition
        if (!IsOpen && _wasOpen)
        {
            _wasOpen = false;
            Reset(reloadRanks: false);
        }

        return Task.CompletedTask;
    }

    // =========================
    // UI actions
    // =========================

    private async Task OnCreateAsync()
    {
        if (_busy)
            return;

        _busy = true;

        try
        {
            NormalizeModel();

            if (OnCreate.HasDelegate)
                await OnCreate.InvokeAsync(Model);

            Toasts.Success("Картка створено");
            await IsOpenChanged.InvokeAsync(false);
        }
        catch (InvalidOperationException ex)
        {
            Toasts.Warning("Неможливо виконати дію", ex.Message);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            Toasts.Warning("Дубль", "Особа з таким РНОКПП вже існує.");
        }
        catch
        {
            Toasts.Error("Помилка", "Сталася неочікувана помилка. Спробуйте ще раз.");
        }
        finally
        {
            _busy = false;
        }
    }

    private void SelectRank(string rank)
    {
        Model.Rank = rank;
        _editContext.NotifyFieldChanged(new FieldIdentifier(Model, nameof(Model.Rank)));
    }

    private async Task OnCancel()
    {
        if (_busy)
            return;

        await IsOpenChanged.InvokeAsync(false);
    }

    private Task OnDrawerClosed()
    {
        Reset(reloadRanks: false);
        return Task.CompletedTask;
    }

    // =========================
    // Internals
    // =========================

    private void Reset(bool reloadRanks)
    {
        _busy = false;

        Model = new CreateReservedDto();
        _editContext = new EditContext(Model);

        if (reloadRanks)
            _ranks = RankCatalog.GetActive();
    }

    private void NormalizeModel()
    {
        Model.Rnokpp = TrimOrEmpty(Model.Rnokpp);
        Model.LastName = TrimOrEmpty(Model.LastName);
        Model.FirstName = TrimOrEmpty(Model.FirstName);
        Model.MiddleName = TrimOrNull(Model.MiddleName);

        Model.Rank = TrimOrNull(Model.Rank);
        Model.Position = TrimOrNull(Model.Position);
    }

    // =========================
    // Helpers
    // =========================

    private static string TrimOrEmpty(string? s) => (s ?? string.Empty).Trim();
    private static string? TrimOrNull(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    private static bool IsUniqueViolation(DbUpdateException ex)
        => ex.InnerException is PostgresException pg && pg.SqlState == PostgresErrorCodes.UniqueViolation;
}
