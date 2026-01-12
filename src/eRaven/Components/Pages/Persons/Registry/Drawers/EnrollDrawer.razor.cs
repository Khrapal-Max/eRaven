//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// EnrollDrawer
//-----------------------------------------------------------------------------

using eRaven.Application.Commands;
using eRaven.Application.Commands.PersonMove;
using eRaven.Application.DTOs;
using eRaven.Application.Queries;
using eRaven.Domain.Enums;
using eRaven.Presentation.Toasts;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace eRaven.Components.Pages.Persons.Registry.Drawers;

public partial class EnrollDrawer
{
    [Parameter] public bool IsOpen { get; set; }
    [Parameter] public EventCallback<bool> IsOpenChanged { get; set; }

    [Parameter] public Guid PersonId { get; set; }                // кого зараховуємо
    [Parameter] public string Author { get; set; } = "system";    // TODO: підставити реального юзера

    [Parameter] public EventCallback<Guid> OnEnrolled { get; set; }

    [Inject] public ToastService Toasts { get; set; } = default!;
    [Inject] public IQueryHandler<GetPersonDetailsQuery, PersonDetailsDto?> DetailsQuery { get; set; } = default!;
    [Inject] public ICommandHandler<EnrollCommand, Guid> EnrollHandler { get; set; } = default!;

    private bool _busy;
    private bool _wasOpen;
    private bool _loading;

    private PersonDetailsDto? _person;

    private EditContext _editContext = default!;
    protected EnrollDto Model { get; set; } = new();

    private static readonly EnrollmentKind[] _kinds = Enum.GetValues<EnrollmentKind>();

    protected override void OnInitialized()
    {
        Model = new EnrollDto();
        _editContext = new EditContext(Model);
    }

    protected override async Task OnParametersSetAsync()
    {
        if (IsOpen && !_wasOpen)
        {
            _wasOpen = true;
            await LoadAsync();
            return;
        }

        if (!IsOpen && _wasOpen)
        {
            _wasOpen = false;
            Reset();
        }
    }

    private async Task LoadAsync()
    {
        Reset();

        if (PersonId == Guid.Empty)
        {
            _person = null;
            return;
        }

        _loading = true;
        try
        {
            _person = await DetailsQuery.HandleAsync(new GetPersonDetailsQuery(PersonId));
            if (_person is not null)
            {
                // prefill
                Model.Position = _person.Position ?? string.Empty;
                Model.EnrollDate = DateOnly.FromDateTime(DateTime.UtcNow);

                // дефолтний kind (якщо треба): Unit
                // Model.Kind = EnrollmentKind.Unit;
            }

            _editContext = new EditContext(Model);
        }
        finally
        {
            _loading = false;
        }
    }

    private void Reset()
    {
        _busy = false;
        _loading = false;

        _person = null;
        Model = new EnrollDto();
        _editContext = new EditContext(Model);
    }

    private async Task OnEnrollAsync()
    {
        if (_busy || _loading || _person is null)
            return;

        _busy = true;
        try
        {
            // trim
            Model.Reference = TrimOrNull(Model.Reference);
            Model.Position = TrimOrEmpty(Model.Position);
            Model.Reason = TrimOrEmpty(Model.Reason);

            var cmd = new EnrollCommand(
                PersonId: _person.Id,
                Kind: Model.Kind,
                Reference: Model.Reference,
                Reason: Model.Reason,
                EnrollDate: Model.EnrollDate,
                Rank: _person.Rank ?? string.Empty,
                Position: Model.Position,
                Author: string.IsNullOrWhiteSpace(Author) ? "system" : Author.Trim(),
                NowUtc: DateTime.UtcNow);

            await EnrollHandler.HandleAsync(cmd);

            Toasts.Success("Зараховано в табель");
            await IsOpenChanged.InvokeAsync(false);

            if (OnEnrolled.HasDelegate)
                await OnEnrolled.InvokeAsync(_person.Id);
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
        if (_busy) return;
        await IsOpenChanged.InvokeAsync(false);
    }

    private Task OnDrawerClosed()
    {
        Reset();
        return Task.CompletedTask;
    }

    private static string TrimOrEmpty(string? s) => (s ?? string.Empty).Trim();
    private static string? TrimOrNull(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    private static string KindTitle(EnrollmentKind k) => k switch
    {
        EnrollmentKind.Unit => "У штат (Unit)",
        EnrollmentKind.AttachedByOrder => "Прикріплено наказом",
        EnrollmentKind.AttachedByList => "Прикріплено списком",
        _ => k.ToString()
    };
}
