//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CreateReservedCommandHandler
//-----------------------------------------------------------------------------

using eRaven.Application.Catalogs.Ranks;
using eRaven.Application.DTOs;
using eRaven.Presentation.Toasts;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace eRaven.Components.Pages.Persons.Registry.Drawers;

public partial class CreateReservedDrawer
{
    [Parameter] public bool IsOpen { get; set; }
    [Parameter] public EventCallback<bool> IsOpenChanged { get; set; }
    [Parameter] public EventCallback<CreateReservedDto> OnCreate { get; set; }

    [Inject] public ToastService Toasts { get; set; } = default!;
    [Inject] public IRankCatalog RankCatalog { get; set; } = default!;

    private bool _busy;
    private bool _wasOpen;

    private EditContext _editContext = default!;
    private IReadOnlyList<RankOption> _ranks = [];

    protected CreateReservedDto Model { get; set; } = new();

    protected override void OnInitialized()
    {
        Model = new CreateReservedDto();
        _editContext = new EditContext(Model);
    }

    protected override Task OnParametersSetAsync()
    {
        // open transition
        if (IsOpen && !_wasOpen)
        {
            _wasOpen = true;
            ResetForm(reloadRanks: true);
            return Task.CompletedTask;
        }

        // close transition
        if (!IsOpen && _wasOpen)
        {
            _wasOpen = false;
            ResetForm(reloadRanks: false);
        }

        return Task.CompletedTask;
    }

    private void ResetForm(bool reloadRanks)
    {
        Model = new CreateReservedDto();
        _editContext = new EditContext(Model);

        if (reloadRanks)
            _ranks = RankCatalog.GetActive();
    }

    private async Task OnCreateAsync()
    {
        if (_busy) return;

        _busy = true;
        try
        {
            // Trim
            Model.Rnokpp = TrimOrEmpty(Model.Rnokpp);
            Model.LastName = TrimOrEmpty(Model.LastName);
            Model.FirstName = TrimOrEmpty(Model.FirstName);
            Model.MiddleName = TrimOrNull(Model.MiddleName);

            Model.Rank = TrimOrNull(Model.Rank);
            Model.Position = TrimOrNull(Model.Position);
            Model.Bzvp = TrimOrNull(Model.Bzvp);
            Model.Weapon = TrimOrNull(Model.Weapon);
            Model.Callsign = TrimOrNull(Model.Callsign);

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

    private async Task OnCancel()
    {
        if (_busy) return;
        await IsOpenChanged.InvokeAsync(false);
    }

    private async Task OnDrawerClosed()
    {
        ResetForm(false);
    }

    private static string TrimOrEmpty(string? s) => (s ?? string.Empty).Trim();
    private static string? TrimOrNull(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    private static bool IsUniqueViolation(DbUpdateException ex)
        => ex.InnerException is PostgresException pg && pg.SqlState == PostgresErrorCodes.UniqueViolation;
}
