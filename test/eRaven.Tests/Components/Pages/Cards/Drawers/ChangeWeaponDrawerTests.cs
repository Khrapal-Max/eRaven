//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ChangeWeaponDrawerTests
//-----------------------------------------------------------------------------

using AngleSharp.Dom;
using Bunit;
using eRaven.Application.DTOs;
using eRaven.Application.Validations.Personal;
using eRaven.Components.Pages.Persons.Cards.Drawers;
using eRaven.Domain.Enums;
using eRaven.Presentation.Toasts;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace eRaven.Tests.Components.Pages.Cards.Drawers;

public sealed class ChangeWeaponDrawerTests : BunitContext
{
    private void RegisterCommon(ToastService? toasts = null)
    {
        Services.AddSingleton(toasts ?? new ToastService());
        Services.AddSingleton<IValidator<ChangeWeaponDto>>(new ChangeWeaponDtoValidator());
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
        Weapon: "АК-74 №001",
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
        var cut = Render<ChangeWeaponDrawer>(ps => ps
            .Add(p => p.Person, Person(id))
            .Add(p => p.IsOpen, true)
            .Add(p => p.IsOpenChanged, _ => Task.CompletedTask)
            .Add(p => p.OnChangeWeapon, _ => Task.CompletedTask)
        );

        // assert
        cut.Find("form#change-weapon-form");

        var submit = cut.Find("button[type='submit']");
        Assert.Equal("change-weapon-form", submit.GetAttribute("form"));
        Assert.Contains("Змінити", submit.TextContent);
    }

    [Fact]
    public async Task Cancel_should_close_drawer_and_not_invoke_OnChangeWeapon()
    {
        // arrange
        RegisterCommon();
        var id = Guid.NewGuid();

        var isOpen = true;
        var called = false;

        var cut = Render<ChangeWeaponDrawer>(ps => ps
            .Add(p => p.Person, Person(id))
            .Add(p => p.IsOpen, isOpen)
            .Add(p => p.IsOpenChanged, v => isOpen = v)
            .Add(p => p.OnChangeWeapon, _ => { called = true; return Task.CompletedTask; })
        );

        // act
        var cancel = cut.FindAll("button").Single(b => b.TextContent.Trim() == "Скасувати");
        cancel.Click();

        // assert
        Assert.False(isOpen);
        Assert.False(called);
    }

    [Fact]
    public async Task Submit_valid_form_should_invoke_OnChangeWeapon_normalize_weapon_show_success_toast_and_close()
    {
        // arrange
        var toasts = new ToastService();
        var shown = new List<ToastMessage>();
        toasts.OnShow += m => shown.Add(m);

        RegisterCommon(toasts);

        var id = Guid.NewGuid();
        var isOpen = true;
        ChangeWeaponDto? captured = null;

        var cut = Render<ChangeWeaponDrawer>(ps => ps
            .Add(p => p.Person, Person(id))
            .Add(p => p.IsOpen, isOpen)
            .Add(p => p.IsOpenChanged, v => isOpen = v)
            .Add(p => p.OnChangeWeapon, dto => { captured = dto; return Task.CompletedTask; })
        );

        // act
        await cut.InvokeAsync(() =>
        {
            FindInputByLabel(cut, "Назва та номер зброї").Change("  АК-74 №777  ");
            // EffectiveDate має дефолт в Reset() => валідно
        });

        await cut.InvokeAsync(() => cut.Find("form#change-weapon-form").Submit());

        // assert
        Assert.NotNull(captured);
        Assert.Equal(id, captured!.PersonId);
        Assert.Equal("АК-74 №777", captured.Weapon); // trim
        Assert.False(isOpen);

        Assert.Contains(shown, m =>
            m.Kind == ToastKind.Success &&
            m.Title.Contains("Запис змінено", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Submit_invalid_form_should_not_invoke_OnChangeWeapon_and_should_not_close()
    {
        // arrange
        RegisterCommon();
        var id = Guid.NewGuid();

        var isOpen = true;
        var called = false;

        var cut = Render<ChangeWeaponDrawer>(ps => ps
            .Add(p => p.Person, Person(id))
            .Add(p => p.IsOpen, isOpen)
            .Add(p => p.IsOpenChanged, v => isOpen = v)
            .Add(p => p.OnChangeWeapon, _ => { called = true; return Task.CompletedTask; })
        );

        // invalid: дата = default
        await cut.InvokeAsync(() =>
        {
            FindInputByLabel(cut, "Дата зміни").Change(""); // InputDate -> default(DateOnly) фактично
        });

        // act
        await cut.InvokeAsync(() => cut.Find("form#change-weapon-form").Submit());

        // assert
        Assert.True(isOpen);
        Assert.False(called);
    }

    [Fact]
    public async Task When_OnChangeWeapon_throws_InvalidOperationException_should_show_warning_and_keep_open()
    {
        // arrange
        var toasts = new ToastService();
        var shown = new List<ToastMessage>();
        toasts.OnShow += m => shown.Add(m);

        RegisterCommon(toasts);

        var id = Guid.NewGuid();
        var isOpen = true;

        var cut = Render<ChangeWeaponDrawer>(ps => ps
            .Add(p => p.Person, Person(id))
            .Add(p => p.IsOpen, isOpen)
            .Add(p => p.IsOpenChanged, v => isOpen = v)
            .Add(p => p.OnChangeWeapon, _ => throw new InvalidOperationException("boom"))
        );

        await cut.InvokeAsync(() => FindInputByLabel(cut, "Назва та номер зброї").Change("АК-74"));

        // act
        await cut.InvokeAsync(() => cut.Find("form#change-weapon-form").Submit());

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
    private static IElement FindInputByLabel(IRenderedComponent<ChangeWeaponDrawer> cut, string labelText)
    {
        var label = cut.FindAll("label")
            .First(l => l.TextContent.Trim().Equals(labelText, StringComparison.Ordinal));

        var container = label.ParentElement ?? throw new NullReferenceException();
        var input = container.QuerySelector("input");

        return input is null ? throw new InvalidOperationException($"Input not found for label '{labelText}'.") : input;
    }
}
