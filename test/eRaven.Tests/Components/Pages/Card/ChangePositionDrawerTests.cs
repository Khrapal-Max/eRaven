//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ChangePositionDrawerTests
//-----------------------------------------------------------------------------

using Bunit;
using eRaven.Application.DTOs;
using eRaven.Components.Pages.Persons.Card.Drawers;
using eRaven.Presentation.Toasts;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace eRaven.Tests.Components.Pages.Persons;

public sealed class ChangePositionDrawerTests : BunitContext
{
    private sealed class ChangePositionDtoValidator : AbstractValidator<ChangePositionDto>
    {
        public ChangePositionDtoValidator()
        {
            RuleFor(x => x.EffectiveDate).NotEmpty();
            RuleFor(x => x.PositionSort).GreaterThanOrEqualTo(0);
        }
    }

    private static PersonDetailsDto Person(Guid id, int positionSort = 10, string? position = "Оператор") => new(
        Id: id,
        Lifecycle: eRaven.Domain.Enums.PersonLifecycle.Enrolled,
        EnrollmentKind: eRaven.Domain.Enums.EnrollmentKind.Unit,
        EnrollmentReference: "A",
        Rnokpp: "1234567890",
        LastName: "Іванов",
        FirstName: "Іван",
        MiddleName: null,
        FullName: "Іванов Іван",
        Rank: "Солдат",
        PositionSort: positionSort,
        Position: position,
        Bzvp: null,
        Weapon: null,
        Callsign: null,
        EnrolledAt: new DateOnly(2026, 01, 10),
        ExcludedAt: null,
        Version: 1,
        UpdatedAtUtc: new DateTime(2026, 01, 10, 0, 0, 0, DateTimeKind.Utc));

    private void RegisterCommon(ToastService? toasts = null)
    {
        Services.AddSingleton(toasts ?? new ToastService());
        Services.AddSingleton<IValidator<ChangePositionDto>>(new ChangePositionDtoValidator());
    }

    [Fact]
    public void When_open_should_render_form_and_submit_button_attributes()
    {
        // arrange
        JSInterop.Mode = JSRuntimeMode.Loose;
        RegisterCommon();

        var id = Guid.NewGuid();

        // act
        var cut = Render<ChangePositionDrawer>(ps => ps
            .Add(p => p.Person, Person(id))
            .Add(p => p.IsOpen, true)
            .Add(p => p.IsOpenChanged, _ => Task.CompletedTask)
            .Add(p => p.OnChangePosition, _ => Task.CompletedTask)
        );

        // assert
        cut.Find("form#change-position-form");

        var submit = cut.Find("button[type='submit']");
        Assert.Equal("change-position-form", submit.GetAttribute("form"));
        Assert.Contains("Змінити", submit.TextContent);
    }

    [Fact]
    public async Task Cancel_should_close_drawer_and_not_invoke_OnChangePosition()
    {
        // arrange
        JSInterop.Mode = JSRuntimeMode.Loose;
        RegisterCommon();

        var id = Guid.NewGuid();
        var isOpen = true;
        var called = false;

        var cut = Render<ChangePositionDrawer>(ps => ps
            .Add(p => p.Person, Person(id))
            .Add(p => p.IsOpen, isOpen)
            .Add(p => p.IsOpenChanged, v => isOpen = v)
            .Add(p => p.OnChangePosition, _ => { called = true; return Task.CompletedTask; })
        );

        // act
        var cancel = cut.FindAll("button").Single(b => b.TextContent.Trim() == "Скасувати");
        cancel.Click();

        // assert
        Assert.False(isOpen);
        Assert.False(called);
    }

    [Fact]
    public async Task Submit_valid_form_should_invoke_OnChangePosition_normalize_fields_show_success_toast_and_close()
    {
        // arrange
        JSInterop.Mode = JSRuntimeMode.Loose;

        var toasts = new ToastService();
        var toastMessages = new List<ToastMessage>();
        toasts.OnShow += m => toastMessages.Add(m);

        RegisterCommon(toasts);

        var id = Guid.NewGuid();
        var isOpen = true;
        ChangePositionDto? captured = null;

        var cut = Render<ChangePositionDrawer>(ps => ps
            .Add(p => p.Person, Person(id, positionSort: 10, position: "Old"))
            .Add(p => p.IsOpen, isOpen)
            .Add(p => p.IsOpenChanged, v => isOpen = v)
            .Add(p => p.OnChangePosition, dto => { captured = dto; return Task.CompletedTask; })
        );

        // act: position sort
        await cut.InvokeAsync(() =>
        {
            // InputNumber<int?> рендерить input type="number"
            var number = cut.Find("input[type='number']");
            number.Change("12");
        });

        // position (TrimOrEmpty)
        await cut.InvokeAsync(() =>
        {
            var position = cut.Find("input[name='Model.Position']");
            var note = cut.Find("input[name='Model.Note']");
            // є 2 input[type=text]: Position і Note (в такому порядку в markup)
            position.Change("  Командир  ");
            note.Change("  note  ");
        });

        // date
        await cut.InvokeAsync(() => cut.Find("input[type='date']").Change("2026-02-01"));

        // submit
        await cut.InvokeAsync(() => cut.Find("form#change-position-form").Submit());

        // assert
        Assert.NotNull(captured);
        Assert.Equal(id, captured!.PersonId);
        Assert.Equal(new DateOnly(2026, 02, 01), captured.EffectiveDate);
        Assert.Equal(12, captured.PositionSort);
        Assert.Equal("Командир", captured.Position);     // trimmed
        Assert.Equal("note", captured.Note);             // trimmed

        Assert.False(isOpen);

        Assert.Contains(toastMessages, m =>
            m.Kind == ToastKind.Success &&
            m.Title.Contains("Посада змінена", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Submit_invalid_form_should_not_invoke_OnChangePosition_and_should_not_close()
    {
        // arrange
        JSInterop.Mode = JSRuntimeMode.Loose;
        RegisterCommon();

        var id = Guid.NewGuid();
        var isOpen = true;
        var called = false;

        var cut = Render<ChangePositionDrawer>(ps => ps
            .Add(p => p.Person, Person(id))
            .Add(p => p.IsOpen, isOpen)
            .Add(p => p.IsOpenChanged, v => isOpen = v)
            .Add(p => p.OnChangePosition, _ => { called = true; return Task.CompletedTask; })
        );

        // робимо invalid: PositionSort = -1 (під наш валідатор)
        await cut.InvokeAsync(() => cut.Find("input[type='number']").Change("-1"));

        // act
        await cut.InvokeAsync(() => cut.Find("form#change-position-form").Submit());

        // assert
        Assert.True(isOpen);
        Assert.False(called);
    }

    [Fact]
    public async Task When_OnChangePosition_throws_InvalidOperationException_should_show_warning_and_keep_open()
    {
        // arrange
        JSInterop.Mode = JSRuntimeMode.Loose;

        var toasts = new ToastService();
        var toastMessages = new List<ToastMessage>();
        toasts.OnShow += m => toastMessages.Add(m);

        RegisterCommon(toasts);

        var id = Guid.NewGuid();
        var isOpen = true;

        var cut = Render<ChangePositionDrawer>(ps => ps
            .Add(p => p.Person, Person(id))
            .Add(p => p.IsOpen, isOpen)
            .Add(p => p.IsOpenChanged, v => isOpen = v)
            .Add(p => p.OnChangePosition, _ => throw new InvalidOperationException("boom"))
        );

        // зробимо форму валідною (на всяк)
        await cut.InvokeAsync(() => cut.Find("input[type='number']").Change("10"));

        // act
        await cut.InvokeAsync(() => cut.Find("form#change-position-form").Submit());

        // assert
        Assert.True(isOpen);
        Assert.Contains(toastMessages, m =>
            m.Kind == ToastKind.Warning &&
            m.Title.Contains("Неможливо виконати дію", StringComparison.Ordinal));
    }
}
