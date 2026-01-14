//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonsRegistry
//-----------------------------------------------------------------------------

using eRaven.Application.Commands;
using eRaven.Application.Commands.PersonMove;
using eRaven.Application.DTOs;
using eRaven.Application.Queries;
using eRaven.Presentation.Toasts;
using Microsoft.AspNetCore.Components;

namespace eRaven.Components.Pages.Persons.Registry;

public partial class PersonsRegistry
{
    // =========================
    // DI
    // =========================

    [Inject] public IQueryHandler<GetPersonsPageQuery, PagedResult<PersonListItemDto>> PersonsPageQuery { get; set; } = default!;
    [Inject] public ICommandHandler<CreateReservedCommand, Guid> CreateReservedHandler { get; set; } = default!;
    [Inject] public ICommandHandler<EnrollCommand, Guid> EnrollHandler { get; set; } = default!;
    [Inject] public ICommandHandler<ExcludeCommand, Guid> ExcludeHandler { get; set; } = default!;
    [Inject] public ToastService Toasts { get; set; } = default!;
    [Inject] public NavigationManager Nav { get; set; } = default!;

    // =========================
    // UI state
    // =========================

    private bool _loading;

    private int _page = 1;
    private int _pageSize = 6;
    private string? _search;

    private PersonsRegistryFilters _filters = new();

    private PagedResult<PersonListItemDto> _pageData = new([], 1, 6, 0);

    private PersonListItemDto? _selected;
    private PersonListItemDto? Selected
    {
        get => _selected;
        set => _selected = value;
    }

    // drawers
    private bool _createReservedOpen;

    private bool _enrollOpen;
    private Guid _enrollPersonId;

    private bool _excludeOpen;
    private Guid _excludePersonId;

    // paging helpers
    private int TotalPages =>
        _pageData.TotalCount <= 0 ? 1 : (int)Math.Ceiling(_pageData.TotalCount / (double)_pageSize);

    private bool IsPrevDisabled => _loading || _page <= 1;
    private bool IsNextDisabled => _loading || _page >= TotalPages;

    // =========================
    // Lifecycle
    // =========================

    protected override async Task OnInitializedAsync()
        => await ReloadAsync();

    // =========================
    // Data loading
    // =========================

    private async Task ReloadAsync()
    {
        _loading = true;
        try
        {
            _pageData = await PersonsPageQuery.HandleAsync(new GetPersonsPageQuery(
                Page: _page,
                PageSize: _pageSize,
                Search: _filters.Search,                 
                Lifecycle: _filters.Lifecycle,           
                EnrollmentKind: _filters.EnrollmentKind  
            ));
        }
        finally
        {
            _loading = false;
        }
    }

    // =========================
    // Toolbar callbacks
    // =========================

    private async Task OnFiltersChanged(PersonsRegistryFilters f)
    {
        _filters = f;
        _page = 1;
        await ReloadAsync();
    }

    private Task OpenCreateReserved()
    {
        _createReservedOpen = true;
        return Task.CompletedTask;
    }

    // =========================
    // Table callbacks
    // =========================

    private Task OnRowClick(PersonListItemDto row)
    {
        _selected = row;
        return Task.CompletedTask;
    }

    private Task OpenCard(PersonListItemDto row)
    {
        _selected = row;
        // Nav.NavigateTo($"/persons/{row.Id}");
        return Task.CompletedTask;
    }

    private Task OpenEnroll(PersonListItemDto row)
    {
        _selected = row;
        _enrollPersonId = row.Id;
        _enrollOpen = true;
        return Task.CompletedTask;
    }

    private Task OpenExclude(PersonListItemDto row)
    {
        _selected = row;
        _excludePersonId = row.Id;
        _excludeOpen = true;
        return Task.CompletedTask;
    }

    // =========================
    // Drawer submit handlers
    // =========================

    private async Task HandleCreateReservedAsync(CreateReservedDto dto)
    {
        var cmd = new CreateReservedCommand(
            PersonId: Guid.NewGuid(),
            Rnokpp: dto.Rnokpp,
            LastName: dto.LastName,
            FirstName: dto.FirstName,
            MiddleName: dto.MiddleName,
            Rank: dto.Rank,
            PositionSort: dto.PositionSort,
            Position: dto.Position,
            Author: "system", // TODO: auth user
            NowUtc: DateTime.UtcNow
        );

        await CreateReservedHandler.HandleAsync(cmd);
        await ReloadAsync();
    }

    private async Task HandleEnrollSubmitAsync(EnrollDto enroll)
    {
        var author = "system"; // TODO: auth user
        var nowUtc = DateTime.UtcNow;

        try
        {
            await EnrollHandler.HandleAsync(new EnrollCommand(
                PersonId: enroll.Id,
                Kind: enroll.Kind,
                Reference: enroll.Reference,
                Reason: enroll.Reason,
                EnrollDate: enroll.EnrollDate,
                Rank: enroll.Rank,
                PositionSort: enroll.PositionSort,
                Position: enroll.Position,
                Author: author,
                NowUtc: nowUtc
            ));

            Toasts.Success("Зараховано в табель");
            await ReloadAsync();

            _selected = _pageData.Items.FirstOrDefault(x => x.Id == enroll.Id) ?? _selected;
        }
        catch (InvalidOperationException ex)
        {
            Toasts.Warning("Неможливо виконати дію", ex.Message);
        }
        catch
        {
            Toasts.Error("Помилка", "Сталася неочікувана помилка. Спробуйте ще раз.");
        }
    }

    private async Task HandleExcludeSubmitAsync(ExcludeDto dto)
    {
        var author = "system"; // TODO: auth user
        var nowUtc = DateTime.UtcNow;

        try
        {
            await ExcludeHandler.HandleAsync(new ExcludeCommand(
                PersonId: dto.Id,
                Reason: dto.Reason,
                EffectiveDate: dto.EffectiveDate,
                Author: author,
                NowUtc: nowUtc
            ));

            Toasts.Success("Виключено з табеля");
            await ReloadAsync();

            _selected = _pageData.Items.FirstOrDefault(x => x.Id == dto.Id) ?? _selected;
        }
        catch (InvalidOperationException ex)
        {
            Toasts.Warning("Неможливо виконати дію", ex.Message);
        }
        catch
        {
            Toasts.Error("Помилка", "Сталася неочікувана помилка. Спробуйте ще раз.");
        }
    }

    // =========================
    // Paging
    // =========================

    private async Task PrevPage()
    {
        if (IsPrevDisabled) return;
        _page--;
        await ReloadAsync();
    }

    private async Task NextPage()
    {
        if (IsNextDisabled) return;
        _page++;
        await ReloadAsync();
    }
}
