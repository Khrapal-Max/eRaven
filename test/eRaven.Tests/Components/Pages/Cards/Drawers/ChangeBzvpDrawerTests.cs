//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ChangeBzvpDrawerTests
//-----------------------------------------------------------------------------

using AngleSharp.Dom;
using Bunit;
using eRaven.Application.DTOs.Person;
using eRaven.Application.Validations.Personal;
using eRaven.Components.Pages.Persons.Cards.Drawers;
using eRaven.Domain.Enums;
using eRaven.Presentation.Toasts;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace eRaven.Tests.Components.Pages.Cards.Drawers;

public sealed class ChangeBzvpDrawerTests : BunitContext
{
    private void RegisterCommon(ToastService? toasts = null)
    {
        Services.AddSingleton(toasts ?? new ToastService());
        Services.AddSingleton<IValidator<ChangeBzvpDto>>(new ChangeBzvpDtoValidator());

        // Drawer може викликати JS всередині — робимо Loose
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
        Bzvp: "ВОС-1",
        Weapon: null,
        Callsign: null,
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
        var cut = Render<ChangeBzvpDrawer>(ps => ps
            .Add(p => p.Person, Person(id))
            .Add(p => p.IsOpen, true)
            .Add(p => p.IsOpenChanged, _ => Task.CompletedTask)
            .Add(p => p.OnChangeBzvp, _ => Task.CompletedTask)
        );

        // assert
        cut.Find("form#change-bzvp-form");

        var submit = cut.Find("button[type='submit']");
        Assert.Equal("change-bzvp-form", submit.GetAttribute("form"));
        Assert.Contains("Змінити", submit.TextContent);
    }

    [Fact]
    public async Task Cancel_should_close_drawer_and_not_invoke_OnChangeBzvp()
    {
        // arrange
        RegisterCommon();
        var id = Guid.NewGuid();

        var isOpen = true;
        var called = false;

        var cut = Render<ChangeBzvpDrawer>(ps => ps
            .Add(p => p.Person, Person(id))
            .Add(p => p.IsOpen, isOpen)
            .Add(p => p.IsOpenChanged, v => isOpen = v)
            .Add(p => p.OnChangeBzvp, _ => { called = true; return Task.CompletedTask; })
        );

        // act
        var cancel = cut.FindAll("button").Single(b => b.TextContent.Trim() == "Скасувати");
        cancel.Click();

        // assert
        Assert.False(isOpen);
        Assert.False(called);
    }

    [Fact]
    public async Task Submit_valid_form_should_invoke_OnChangeBzvp_normalize_fields_show_success_toast_and_close()
    {
        // arrange
        var toasts = new ToastService();
        var shown = new List<ToastMessage>();
        toasts.OnShow += m => shown.Add(m);

        RegisterCommon(toasts);

        var id = Guid.NewGuid();
        var isOpen = true;
        ChangeBzvpDto? captured = null;

        var cut = Render<ChangeBzvpDrawer>(ps => ps
            .Add(p => p.Person, Person(id))
            .Add(p => p.IsOpen, isOpen)
            .Add(p => p.IsOpenChanged, v => isOpen = v)
            .Add(p => p.OnChangeBzvp, dto => { captured = dto; return Task.CompletedTask; })
        );

        // act
        await cut.InvokeAsync(() =>
        {
            FindInputByLabel(cut, "БЗВП (ВОС/УБД)").Change("  ВОС-777  "); // валідно, і дозволяє перевірити Trim
            FindInputByLabel(cut, "Замітка (опц.)").Change("  note  ");
            // EffectiveDate вже має дефолт у Reset() -> валідно
        });

        await cut.InvokeAsync(() => cut.Find("form#change-bzvp-form").Submit());

        // assert
        Assert.NotNull(captured);
        Assert.Equal(id, captured!.PersonId);
        Assert.Equal("ВОС-777", captured.Bzvp);     // trim
        Assert.Equal("note", captured.Note);        // trim
        Assert.False(isOpen);

        Assert.Contains(shown, m =>
            m.Kind == ToastKind.Success &&
            m.Title.Contains("Запис змінено", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Submit_invalid_form_should_not_invoke_OnChangeBzvp_and_should_not_close()
    {
        // arrange
        RegisterCommon();
        var id = Guid.NewGuid();

        var isOpen = true;
        var called = false;

        var cut = Render<ChangeBzvpDrawer>(ps => ps
            .Add(p => p.Person, Person(id))
            .Add(p => p.IsOpen, isOpen)
            .Add(p => p.IsOpenChanged, v => isOpen = v)
            .Add(p => p.OnChangeBzvp, _ => { called = true; return Task.CompletedTask; })
        );

        // invalid: Bzvp empty
        await cut.InvokeAsync(() => FindInputByLabel(cut, "БЗВП (ВОС/УБД)").Change(""));

        // act
        await cut.InvokeAsync(() => cut.Find("form#change-bzvp-form").Submit());

        // assert
        Assert.True(isOpen);
        Assert.False(called);
    }

    [Fact]
    public async Task When_OnChangeBzvp_throws_InvalidOperationException_should_show_warning_and_keep_open()
    {
        // arrange
        var toasts = new ToastService();
        var shown = new List<ToastMessage>();
        toasts.OnShow += m => shown.Add(m);

        RegisterCommon(toasts);

        var id = Guid.NewGuid();
        var isOpen = true;

        var cut = Render<ChangeBzvpDrawer>(ps => ps
            .Add(p => p.Person, Person(id))
            .Add(p => p.IsOpen, isOpen)
            .Add(p => p.IsOpenChanged, v => isOpen = v)
            .Add(p => p.OnChangeBzvp, _ => throw new InvalidOperationException("boom"))
        );

        await cut.InvokeAsync(() => FindInputByLabel(cut, "БЗВП (ВОС/УБД)").Change("ВОС-OK"));

        // act
        await cut.InvokeAsync(() => cut.Find("form#change-bzvp-form").Submit());

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
    private static IElement FindInputByLabel(IRenderedComponent<ChangeBzvpDrawer> cut, string labelText)
    {
        var label = cut.FindAll("label")
            .First(l => l.TextContent.Trim().Equals(labelText, StringComparison.Ordinal));

        var container = label.ParentElement ?? throw new NullReferenceException();
        var input = container.QuerySelector("input");

        return input is null ? throw new InvalidOperationException($"Input not found for label '{labelText}'.") : input;
    }
}
