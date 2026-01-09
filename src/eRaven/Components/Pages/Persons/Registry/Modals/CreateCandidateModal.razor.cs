//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CreateCandidateModal
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs;
using eRaven.Exceptions;
using eRaven.Presentation.Toasts;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace eRaven.Components.Pages.Persons.Registry.Modals;

public partial class CreateCandidateModal
{
    [Parameter] public bool IsOpen { get; set; }
    [Parameter] public EventCallback<bool> IsOpenChanged { get; set; }

    [Parameter] public EventCallback<CreateCandidateDto> OnCreate { get; set; }

    [Inject] public ToastService Toasts { get; set; } = default!;

    private EditContext _editContext = default!;
    private bool _busy;
    protected CreateCandidateDto Model { get; set; } = new();
    protected override void OnParametersSet()
    {
        // при першому рендері або кожному відкритті — нова модель/контекст
        if (IsOpen && (_editContext is null || ReferenceEquals(_editContext.Model, Model) == false))
        {
            Model = new CreateCandidateDto();
            _editContext = new EditContext(Model);
        }
    }

    // викликається кнопкою "Створити" з базового Modal
    private async Task<bool> OnCreateAsync()
    {
        if (_busy) return false;

        var isValid = _editContext.Validate();
        if (!isValid)
            return false;

        _busy = true;
        try
        {
            Model.Rnokpp = TrimOrEmpty(Model.Rnokpp);
            Model.LastName = TrimOrEmpty(Model.LastName);
            Model.FirstName = TrimOrEmpty(Model.FirstName);
            Model.MiddleName = TrimOrNull(Model.MiddleName);
            Model.PlannedPosition = TrimOrNull(Model.PlannedPosition);

            await OnCreate.InvokeAsync(Model);

            Toasts.Success("Кандидата створено");
            return true; // закроется
        }
        catch (ConcurrencyException)
        {
            Toasts.Warning("Конфлікт змін", "Запис був змінений кимось іншим. Оновіть сторінку і повторіть дію.");
            return false; // модалка остается открытой
        }
        catch (InvalidOperationException ex)
        {
            // доменные правила, например "RNOKPP уже существует"
            Toasts.Warning("Неможливо виконати дію", ex.Message);
            return false;
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            Toasts.Warning("Дубль", "Особа з таким РНОКПП вже існує.");
            return false;
        }
        catch (Exception)
        {
            Toasts.Error("Помилка", "Сталася неочікувана помилка. Спробуйте ще раз.");
            return false; // или throw; если хочешь ErrorBoundary
        }
        finally
        {
            _busy = false;
        }
    }

    // опціонально: при Cancel можемо просто скидати форму
    private Task OnCancel()
    {
        // якщо хочеш — скинути модель при cancel:
        Model = new CreateCandidateDto();
        _editContext = new EditContext(Model);

        return Task.CompletedTask;
    }

    private static string TrimOrEmpty(string? s) => (s ?? string.Empty).Trim();
    private static string? TrimOrNull(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
    private static bool IsUniqueViolation(DbUpdateException ex)
       => ex.InnerException is PostgresException pg && pg.SqlState == PostgresErrorCodes.UniqueViolation;
}