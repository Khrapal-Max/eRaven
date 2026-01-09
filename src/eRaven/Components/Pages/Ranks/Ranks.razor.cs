//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// Ranks
//-----------------------------------------------------------------------------

using eRaven.Components.Shared.ConfirmModal;
using eRaven.Domain.Entities;
using eRaven.Extensions;
using eRaven.Infrastructure.Repositories.RankRepository;
using FluentValidation;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Components.Pages.Ranks;

public partial class Ranks : IDisposable
{
    // ==== table of position units ====
    protected List<Rank> RanksList { get; set; } = [];
    protected Rank? Selected { get; set; }

    // ==== ui ====
    private readonly CancellationTokenSource _cts = new();
    protected bool IsLoading { get; private set; } = true;

    // ==== create modal state ====
    private EditContext? _createEditContext;
    protected bool CreateOpen { get; set; }
    protected Rank CreateModel { get; set; } = new();

    // ==== comfirm modal state ====
    private ConfirmModal<Rank> _deactivateModal = default!;

    // ==== dependensy ====
    [Inject] private IRankRepository RankRepository { get; set; } = default!;
    [Inject] private IValidator<Rank> RankValidator { get; set; } = default!;

    protected override async Task OnInitializedAsync() => await LoadAsync();

    private async Task LoadAsync()
    {
        try
        {
            IsLoading = true;
            RanksList = [.. await RankRepository.GetAllRanksAsync(_cts.Token)];
        }
        catch (OperationCanceledException) { }
        finally
        {
            IsLoading = false;
        }
    }

    protected void OnAddRankClick()
    {
        CreateModel = new Rank { Id = Guid.NewGuid(), IsActived = true };
        _createEditContext = new EditContext(CreateModel);
        CreateOpen = true;
    }

    private async Task<bool> CreateAsync()
    {
        if (_createEditContext is null) return false;

        var isValid = await _createEditContext.ValidateWithFluentValidationAsync(RankValidator, _cts.Token);
        if (!isValid) return false;

        if (await RankRepository.ActiveTitleExistsAsync(CreateModel.Title, _cts.Token))
        {
            _createEditContext.AddFieldError(CreateModel, nameof(Rank.Title), "Звання з такою назваю вже активне");
            return false;
        }

        try
        {
            await RankRepository.AddRankAsync(CreateModel, _cts.Token);
        }
        catch (DbUpdateException)
        {
            _createEditContext.AddFieldError(CreateModel, nameof(Rank.Title), "Звання вже існує");
            return false;
        }

        await LoadAsync();

        // cleanup
        _createEditContext = null;
        CreateModel = new Rank();

        return true;
    }

    protected async Task AskDeactivate(Rank rank)
    {
        var ok = await _deactivateModal.ShowAsync(rank);
        if (!ok) return;

        await RankRepository.DeActivatedRankAsync(rank.Id, _cts.Token);
        await LoadAsync();
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
        GC.SuppressFinalize(this);
    }
}
