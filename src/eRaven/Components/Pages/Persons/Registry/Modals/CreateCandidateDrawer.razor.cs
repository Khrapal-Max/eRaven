//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CreateCandidateModal
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs;
using eRaven.Application.Queries;
using eRaven.Exceptions;
using eRaven.Presentation.Toasts;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace eRaven.Components.Pages.Persons.Registry.Modals;

public partial class CreateCandidateDrawer
{
    [Parameter] public bool IsOpen { get; set; }
    [Parameter] public EventCallback<bool> IsOpenChanged { get; set; }
    [Parameter] public EventCallback<CreateCandidateDto> OnCreate { get; set; }

    [Inject] public ToastService Toasts { get; set; } = default!;
    [Inject] public IQueryHandler<GetVacantPositionUnitsQuery, IReadOnlyList<PositionUnitOptionDto>> VacantPositionsQuery { get; set; } = default!;

    private bool _busy;
    private bool _wasOpen;
    private string AriaHidden => IsOpen ? "false" : "true";
    private string AriaModal => IsOpen ? "true" : "false";

    private EditContext _editContext = default!;
    private IReadOnlyList<PositionUnitOptionDto> _positions = [];

    protected CreateCandidateDto Model { get; set; } = new();

    private string SelectedPositionLabel
    {
        get
        {
            if (Model.PlannedPositionUnitId is not Guid id)
                return "— не обирати —";

            var opt = _positions.FirstOrDefault(x => x.Id == id);
            if (opt is null)
                return "— не обирати —";

            return $"{opt.Code} • {opt.ShortName} • {opt.Rank} • {opt.Tarif}";
        }
    }

    protected override void OnInitialized()
    {
        // щоб EditForm ніколи не був без контексту
        Model = new CreateCandidateDto();
        _editContext = new EditContext(Model);
    }

    protected override async Task OnParametersSetAsync()
    {
        // open transition
        if (IsOpen && !_wasOpen)
        {
            _wasOpen = true;
            await ResetFormAsync(reloadPositions: true);
            return;
        }

        // close transition
        if (!IsOpen && _wasOpen)
        {
            _wasOpen = false;
            await ResetFormAsync(reloadPositions: false);
        }
    }

    private async Task ResetFormAsync(bool reloadPositions)
    {
        Model = new CreateCandidateDto();
        _editContext = new EditContext(Model);

        _positions = reloadPositions
            ? await VacantPositionsQuery.HandleAsync(new GetVacantPositionUnitsQuery(Take: 200))
            : [];
    }

    private void SelectPlannedPosition(PositionUnitOptionDto p)
    {
        Model.PlannedPositionUnitId = p.Id;
        Model.PlannedPosition = TrimOrNull(p.FullName);

        _editContext.NotifyFieldChanged(new FieldIdentifier(Model, nameof(Model.PlannedPositionUnitId)));
    }

    private void ClearPlannedPosition()
    {
        Model.PlannedPositionUnitId = null;
        Model.PlannedPosition = null;

        _editContext.NotifyFieldChanged(new FieldIdentifier(Model, nameof(Model.PlannedPositionUnitId)));
    }

    private async Task OnCreateAsync()
    {
        if (_busy) return;

        if (!_editContext.Validate())
            return;

        _busy = true;
        try
        {
            Model.Rnokpp = TrimOrEmpty(Model.Rnokpp);
            Model.LastName = TrimOrEmpty(Model.LastName);
            Model.FirstName = TrimOrEmpty(Model.FirstName);
            Model.MiddleName = TrimOrNull(Model.MiddleName);
            Model.PlannedPosition = _positions.FirstOrDefault(x => x.Id == Model.PlannedPositionUnitId) is null
                            ? null : _positions.FirstOrDefault(x => x.Id == Model.PlannedPositionUnitId)?.FullName;

            await OnCreate.InvokeAsync(Model);

            Toasts.Success("Кандидата створено");

            // ✅ закриваємо drawer
            await IsOpenChanged.InvokeAsync(false);

            // ✅ після створення вакансії змінюються — при наступному open вони будуть перезавантажені
            // (або можеш одразу перезавантажити, якщо хочеш)
        }
        catch (ConcurrencyException)
        {
            Toasts.Warning("Конфлікт змін", "Запис був змінений кимось іншим. Оновіть сторінку і повторіть дію.");
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

    private void OnPositionSelected(PositionUnitOptionDto? p)
    {
        if (p is null)
        {
            Model.PlannedPositionUnitId = null;
            Model.PlannedPosition = null;
        }
        else
        {
            Model.PlannedPositionUnitId = p.Id;
            Model.PlannedPosition = TrimOrNull(p.FullName);
        }

        _editContext.NotifyFieldChanged(new FieldIdentifier(Model, nameof(Model.PlannedPositionUnitId)));
    }

    private async Task OnCancel()
    {
        if (_busy) return;

        await IsOpenChanged.InvokeAsync(false);
    }

    private async Task Close()
    {
        if (_busy) return;

        await IsOpenChanged.InvokeAsync(false);
    }

    private async Task OnBackdropClick()
    {
        if (_busy) return;

        await IsOpenChanged.InvokeAsync(false);
    }

    private static string TrimOrEmpty(string? s) => (s ?? string.Empty).Trim();
    private static string? TrimOrNull(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    private static bool IsUniqueViolation(DbUpdateException ex)
        => ex.InnerException is PostgresException pg && pg.SqlState == PostgresErrorCodes.UniqueViolation;
}
