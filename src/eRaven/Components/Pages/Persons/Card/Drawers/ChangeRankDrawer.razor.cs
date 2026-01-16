//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CreateReservedDrawer
//-----------------------------------------------------------------------------

using eRaven.Application.Catalogs.Ranks;
using eRaven.Application.DTOs;
using eRaven.Presentation.Toasts;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace eRaven.Components.Pages.Persons.Card.Drawers;

public partial class ChangeRankDrawer
{
    // =========================
    // Parameters
    // =========================  
    [Parameter, EditorRequired] public PersonDetailsDto Person { get; set; } = default!;
    [Parameter] public bool IsOpen { get; set; }
    [Parameter] public EventCallback<bool> IsOpenChanged { get; set; }
    [Parameter] public EventCallback<ChangeRankDto> OnChangeRank { get; set; }

    // =========================
    // DI
    // =========================
    [Inject] public IRankCatalog RankCatalog { get; set; } = default!;
    [Inject] public ToastService Toasts { get; set; } = default!;

    // =========================
    // State
    // =========================

    private bool _busy;
    private bool _wasOpen;

    private EditContext _editContext = default!;
    private IReadOnlyList<RankOption> _ranks = [];

    protected ChangeRankDto Model { get; set; } = new();

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

    private async Task OnChangeAsync()
    {
        if (_busy)
            return;

        _busy = true;

        try
        {
            NormalizeModel();

            if (OnChangeRank.HasDelegate)
                await OnChangeRank.InvokeAsync(Model);

            Toasts.Success("Звання змінено");
            await IsOpenChanged.InvokeAsync(false);
        }
        catch (InvalidOperationException ex)
        {
            Toasts.Warning("Неможливо виконати дію", ex.Message);
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
        if (Person is null) return;

        _busy = false;

        Model = new ChangeRankDto
        {
            PersonId = Person.Id,                               // ✅ критично
            EffectiveDate = DateOnly.FromDateTime(DateTime.Now),// ✅ дефолт
            Rank = Person.Rank ?? string.Empty,                 // (можеш лишити пусто, якщо хочеш примусово обирати)
            Note = null
        };

        _editContext = new EditContext(Model);

        if (reloadRanks)
            _ranks = RankCatalog.GetActive();
    }

    private void SelectRank(string rank)
    {
        Model.Rank = rank;
        _editContext.NotifyFieldChanged(new FieldIdentifier(Model, nameof(Model.Rank)));
    }

    private void NormalizeModel()
    {
        Model.Rank = TrimOrEmpty(Model.Rank);
        Model.Note = string.IsNullOrWhiteSpace(Model.Note) ? null : Model.Note.Trim();
    }

    // =========================
    // Helpers
    // =========================

    private static string TrimOrEmpty(string? s) => (s ?? string.Empty).Trim();
}
