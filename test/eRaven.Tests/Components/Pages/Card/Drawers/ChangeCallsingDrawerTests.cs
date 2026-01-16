//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ChangeCallsingDrawerTests
//-----------------------------------------------------------------------------

using AngleSharp.Dom;
using Bunit;
using eRaven.Application.DTOs;
using eRaven.Application.Validations.Personal;
using eRaven.Components.Pages.Persons.Card.Drawers;
using eRaven.Domain.Enums;
using eRaven.Presentation.Toasts;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace eRaven.Tests.Components.Pages.Card.Drawers;

public sealed class ChangeCallsingDrawerTests : BunitContext
{
    private void RegisterCommon(ToastService? toasts = null)
    {
        Services.AddSingleton(toasts ?? new ToastService());
        Services.AddSingleton<IValidator<ChangeCallsingDto>>(new ChangeCallsingDtoValidator());
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private static PersonDetailsDto Person(Guid id) => new(
        Id: id,
        Lifecycle: PersonLifecycle.Enrolled,
        EnrollmentKind: EnrollmentKind.Unit,
        EnrollmentReference: "A",
        Rnokpp: "1234567890",
        LastName: "Іванов",
        FirstName: "Іван",
        MiddleName: "Іванович",
        FullName: "Іванов Іван Іванович",
        Rank: "Солдат",
        PositionSort: 10,
        Position: "Оператор",
        Bzvp: null,
        Weapon: null,
        Callsign: "Дніпро",
        EnrolledAt: new DateOnly(2026, 01, 10),
        ExcludedAt: null,
        Version: 1,
        UpdatedAtUtc: new DateTime(2026, 01, 10, 0, 0, 0, DateTimeKind.Utc));

    [Fact]
    public void When_open_should_render_form_and_submit_button_attributes()
    {
        // arrange
        RegisterCommon();
        var id = Guid.NewGuid();

        // act
        var cut = Render<ChangeCallsingDrawer>(ps => ps
            .Add(p => p.Person, Person(id))
            .Add(p => p.IsOpen, true)
            .Add(p => p.IsOpenChanged, _ => Task.CompletedTask)
            .Add(p => p.OnChangeCallsing, _ => Task.CompletedTask)
        );

        // assert
        cut.Find("form#change-callsing-form");

        var submit = cut.Find("button[type='submit']");
        Assert.Equal("change-callsing-form", submit.GetAttribute("form"));
        Assert.Contains("Змінити", submit.TextContent);
    }

    [Fact]
    public async Task Cancel_should_close_drawer_and_not_invoke_OnChangeCallsing()
    {
        // arrange
        RegisterCommon();
        var id = Guid.NewGuid();

        var isOpen = true;
        var called = false;

        var cut = Render<ChangeCallsingDrawer>(ps => ps
            .Add(p => p.Person, Person(id))
            .Add(p => p.IsOpen, isOpen)
            .Add(p => p.IsOpenChanged, v => isOpen = v)
            .Add(p => p.OnChangeCallsing, _ => { called = true; return Task.CompletedTask; })
        );

        // act
        var cancel = cut.FindAll("button").Single(b => b.TextContent.Trim() == "Скасувати");
        cancel.Click();

        // assert
        Assert.False(isOpen);
        Assert.False(called);
    }

    [Fact]
    public async Task Submit_valid_form_should_invoke_OnChangeCallsing_normalize_callsign_show_success_toast_and_close()
    {
        // arrange
        var toasts = new ToastService();
        var shown = new List<ToastMessage>();
        toasts.OnShow += m => shown.Add(m);

        RegisterCommon(toasts);

        var id = Guid.NewGuid();
        var isOpen = true;
        ChangeCallsingDto? captured = null;

        var cut = Render<ChangeCallsingDrawer>(ps => ps
            .Add(p => p.Person, Person(id))
            .Add(p => p.IsOpen, isOpen)
            .Add(p => p.IsOpenChanged, v => isOpen = v)
            .Add(p => p.OnChangeCallsing, dto => { captured = dto; return Task.CompletedTask; })
        );

        // act
        await cut.InvokeAsync(() =>
        {
            FindInputByLabel(cut, "Позивний").Change("  Блискавка  ");
            // дата має дефолтний валідний DateOnly в Reset()
        });

        await cut.InvokeAsync(() => cut.Find("form#change-callsing-form").Submit());

        // assert
        Assert.NotNull(captured);
        Assert.Equal(id, captured!.PersonId);
        Assert.Equal("Блискавка", captured.Callsign); // trim
        Assert.False(isOpen);

        Assert.Contains(shown, m =>
            m.Kind == ToastKind.Success &&
            m.Title.Contains("Запис змінено", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Submit_invalid_form_should_not_invoke_OnChangeCallsing_and_should_not_close()
    {
        // arrange
        RegisterCommon();
        var id = Guid.NewGuid();

        var isOpen = true;
        var called = false;

        var cut = Render<ChangeCallsingDrawer>(ps => ps
            .Add(p => p.Person, Person(id))
            .Add(p => p.IsOpen, isOpen)
            .Add(p => p.IsOpenChanged, v => isOpen = v)
            .Add(p => p.OnChangeCallsing, _ => { called = true; return Task.CompletedTask; })
        );

        // invalid: Callsign > 128 (валідація спрацює, OnValidSubmit не викличеться)
        await cut.InvokeAsync(() => FindInputByLabel(cut, "Позивний").Change(new string('C', 129)));

        // act
        await cut.InvokeAsync(() => cut.Find("form#change-callsing-form").Submit());

        // assert
        Assert.True(isOpen);
        Assert.False(called);
    }

    [Fact]
    public async Task When_OnChangeCallsing_throws_InvalidOperationException_should_show_warning_and_keep_open()
    {
        // arrange
        var toasts = new ToastService();
        var shown = new List<ToastMessage>();
        toasts.OnShow += m => shown.Add(m);

        RegisterCommon(toasts);

        var id = Guid.NewGuid();
        var isOpen = true;

        var cut = Render<ChangeCallsingDrawer>(ps => ps
            .Add(p => p.Person, Person(id))
            .Add(p => p.IsOpen, isOpen)
            .Add(p => p.IsOpenChanged, v => isOpen = v)
            .Add(p => p.OnChangeCallsing, _ => throw new InvalidOperationException("boom"))
        );

        await cut.InvokeAsync(() => FindInputByLabel(cut, "Позивний").Change("Дніпро"));

        // act
        await cut.InvokeAsync(() => cut.Find("form#change-callsing-form").Submit());

        // assert
        Assert.True(isOpen);

        Assert.Contains(shown, m =>
            m.Kind == ToastKind.Warning &&
            m.Title.Contains("Неможливо виконати дію", StringComparison.Ordinal) &&
            m.Body == "boom");
    }

    // -------------------------
    // Helpers: input by label
    // -------------------------
    private static IElement FindInputByLabel(IRenderedComponent<ChangeCallsingDrawer> cut, string labelText)
    {
        var label = cut.FindAll("label")
            .First(l => l.TextContent.Trim().Equals(labelText, StringComparison.Ordinal));

        var container = label.ParentElement ?? throw new NullReferenceException();
        var input = container.QuerySelector("input");

        return input is null ? throw new InvalidOperationException($"Input not found for label '{labelText}'.") : input;
    }
}
