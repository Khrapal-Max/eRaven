//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonsRegistry
//-----------------------------------------------------------------------------

using eRaven.Application.Commands;
using eRaven.Application.Commands.PersonMove;
using eRaven.Application.DTOs.Person;
using eRaven.Application.Queries;
using eRaven.Application.Queries.Personal;
using eRaven.Domain.Enums;
using eRaven.Presentation.Toasts;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;

namespace eRaven.Components.Pages.Persons.Registry;

public partial class PersonsRegistry
{
    // =========================
    // DI
    // =========================

    [Inject] public IQueryHandler<GetPersonsPageQuery, PagedResult<PersonListItemDto>> PersonsPageQuery { get; set; } = default!;
    [Inject] public ICommandHandler<CreateReservedCommand, Guid> CreateReservedHandler { get; set; } = default!;
    [Inject] public ICommandHandler<EnrollCommand> EnrollHandler { get; set; } = default!;
    [Inject] public ICommandHandler<ExcludeCommand> ExcludeHandler { get; set; } = default!;
    [Inject] public ToastService Toasts { get; set; } = default!;
    [Inject] public NavigationManager Nav { get; set; } = default!;

    // =========================
    // UI state
    // =========================

    private bool _loading;

    private int _page = 1;
    private readonly int _pageSize = 10;

    private PersonsRegistryFilters _filters = new();

    private PagedResult<PersonListItemDto> _pageData = new([], 1, 6, 0);

    private PersonListItemDto? _selected;

    // drawers
    private bool _createReservedOpen;

    private bool _enrollOpen;
    private Guid _enrollPersonId;

    private bool _excludeOpen;
    private Guid _excludePersonId;

    private bool _importExportOpen;

    // paging helpers
    private int TotalPages =>
        _pageData.TotalCount <= 0 ? 1 : (int)Math.Ceiling(_pageData.TotalCount / (double)_pageSize);

    private bool IsPrevDisabled => _loading || _page <= 1;
    private bool IsNextDisabled => _loading || _page >= TotalPages;

    // track uri changes for query-string filters
    private string _lastUri = "";

    // =========================
    // Lifecycle
    // =========================

    protected override async Task OnParametersSetAsync()
    {
        // якщо Uri не змінився — нічого не робимо, щоб не було зайвих перезавантажень
        if (string.Equals(_lastUri, Nav.Uri, StringComparison.Ordinal))
            return;

        _lastUri = Nav.Uri;

        ApplyFiltersFromQueryString();
        _page = 1;
        await ReloadAsync();
    }

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

        // (опційно) якщо хочеш синхронізувати фільтри з URL — тут можна робити NavigateTo з query-string,
        // але зараз не чіпаємо, щоб не ускладнювати.
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
        Nav.NavigateTo($"/persons/{row.Id}");
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

    private Task OpenImportExport()
    {
        _importExportOpen = true;
        return Task.CompletedTask;
    }

    private async Task HandleImportedAsync()
    {
        _page = 1;
        await ReloadAsync();
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

    private void ApplyFiltersFromQueryString()
    {
        // Важливо: стартуємо з чистого state, щоб “старі” фільтри не прилипали
        var f = new PersonsRegistryFilters();

        var uri = Nav.ToAbsoluteUri(Nav.Uri);
        var qs = QueryHelpers.ParseQuery(uri.Query);

        if (qs.TryGetValue("lifecycle", out var lifecycleStr) &&
            Enum.TryParse<PersonLifecycle>(lifecycleStr, ignoreCase: true, out var lifecycle))
            f = f with { Lifecycle = lifecycle };

        if (qs.TryGetValue("enrollmentKind", out var kindStr) &&
            Enum.TryParse<EnrollmentKind>(kindStr, ignoreCase: true, out var kind))
            f = f with { EnrollmentKind = kind };

        _filters = f;
    }
}
