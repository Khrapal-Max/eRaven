//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// UpdatePersonalInfoDrawerTests
//-----------------------------------------------------------------------------

using AngleSharp.Dom;
using Bunit;
using eRaven.Application.DTOs.Enums;
using eRaven.Application.DTOs.Person;
using eRaven.Application.Validations.Personal;
using eRaven.Components.Pages.Persons.Cards.Drawers;
using eRaven.Presentation.Toasts;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace eRaven.Tests.Components.Pages.Cards.Drawers;

public sealed class UpdatePersonalInfoDrawerTests : BunitContext
{
    private static PersonDetailsDto Person(Guid id) => new(
        Id: id,
        Lifecycle: PersonLifecycleDto.Enrolled,
        EnrollmentKind: EnrollmentKindDto.Unit,
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
        Callsign: null,
        EnrolledAt: new DateOnly(2026, 01, 10),
        ExcludedAt: null,
        Version: 1,
        UpdatedAtUtc: new DateTime(2026, 01, 10, 0, 0, 0, DateTimeKind.Utc));

    private void RegisterCommon(ToastService? toasts = null)
    {
        Services.AddSingleton(toasts ?? new ToastService());
        Services.AddSingleton<IValidator<UpdatePersonalInfoDto>>(new UpdatePersonalInfoDtoValidator());
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void When_open_should_render_form_and_submit_button_attributes()
    {
        RegisterCommon();
        var id = Guid.NewGuid();

        var cut = Render<UpdatePersonalInfoDrawer>(ps => ps
            .Add(p => p.Person, Person(id))
            .Add(p => p.IsOpen, true)
            .Add(p => p.IsOpenChanged, _ => Task.CompletedTask)
            .Add(p => p.OnUpdatePersonal, _ => Task.CompletedTask)
        );

        cut.Find("form#update-personal-form");

        var submit = cut.Find("button[type='submit']");
        Assert.Equal("update-personal-form", submit.GetAttribute("form"));
        Assert.Contains("Оновити", submit.TextContent);
    }

    [Fact]
    public async Task Cancel_should_close_drawer_and_not_invoke_OnUpdatePersonal()
    {
        RegisterCommon();
        var id = Guid.NewGuid();

        var isOpen = true;
        var called = false;

        var cut = Render<UpdatePersonalInfoDrawer>(ps => ps
            .Add(p => p.Person, Person(id))
            .Add(p => p.IsOpen, isOpen)
            .Add(p => p.IsOpenChanged, v => isOpen = v)
            .Add(p => p.OnUpdatePersonal, _ => { called = true; return Task.CompletedTask; })
        );

        var cancel = cut.FindAll("button").Single(b => b.TextContent.Trim() == "Скасувати");
        cancel.Click();

        Assert.False(isOpen);
        Assert.False(called);
    }

    [Fact]
    public async Task Submit_valid_form_should_invoke_OnUpdatePersonal_normalize_fields_show_success_toast_and_close()
    {
        var toasts = new ToastService();
        var shown = new List<ToastMessage>();
        toasts.OnShow += m => shown.Add(m);

        RegisterCommon(toasts);

        var id = Guid.NewGuid();
        var isOpen = true;
        UpdatePersonalInfoDto? captured = null;

        var cut = Render<UpdatePersonalInfoDrawer>(ps => ps
            .Add(p => p.Person, Person(id))
            .Add(p => p.IsOpen, isOpen)
            .Add(p => p.IsOpenChanged, v => isOpen = v)
            .Add(p => p.OnUpdatePersonal, dto => { captured = dto; return Task.CompletedTask; })
        );

        // act: заповнюємо по label (стабільно)
        await cut.InvokeAsync(() =>
        {
            FindByNameOrLabel(cut, "Rnokpp", "РНОКПП (10 цифр)").Change("1234567890");
            FindByNameOrLabel(cut, "LastName", "Прізвище").Change("  Петров  ");
            FindByNameOrLabel(cut, "FirstName", "Ім’я").Change("  Петро ");
            FindByNameOrLabel(cut, "MiddleName", "По батькові (опц.)").Change("   ");
            FindByNameOrLabel(cut, "Note", "Замітка (опц.)").Change("  note text  ");
        });

        await cut.InvokeAsync(() => cut.Find("form#update-personal-form").Submit());

        // assert
        Assert.NotNull(captured);
        Assert.Equal(id, captured!.PersonId);
        Assert.Equal("1234567890", captured.Rnokpp);
        Assert.Equal("Петров", captured.LastName);
        Assert.Equal("Петро", captured.FirstName);
        Assert.Null(captured.MiddleName);
        Assert.Equal("note text", captured.Note);

        Assert.False(isOpen);

        Assert.Contains(shown, m =>
            m.Kind == ToastKind.Success &&
            m.Title.Contains("Персональна інфо оновлена", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Submit_invalid_form_should_not_invoke_OnUpdatePersonal_and_should_not_close()
    {
        RegisterCommon();
        var id = Guid.NewGuid();

        var isOpen = true;
        var called = false;

        var cut = Render<UpdatePersonalInfoDrawer>(ps => ps
            .Add(p => p.Person, Person(id))
            .Add(p => p.IsOpen, isOpen)
            .Add(p => p.IsOpenChanged, v => isOpen = v)
            .Add(p => p.OnUpdatePersonal, _ => { called = true; return Task.CompletedTask; })
        );

        // invalid: РНОКПП неправильний
        await cut.InvokeAsync(() =>
            FindByNameOrLabel(cut, "Rnokpp", "РНОКПП (10 цифр)").Change("123")
        );

        await cut.InvokeAsync(() => cut.Find("form#update-personal-form").Submit());

        Assert.True(isOpen);
        Assert.False(called);
    }

    [Fact]
    public async Task When_OnUpdatePersonal_throws_InvalidOperationException_should_show_warning_and_keep_open()
    {
        var toasts = new ToastService();
        var shown = new List<ToastMessage>();
        toasts.OnShow += m => shown.Add(m);

        RegisterCommon(toasts);

        var id = Guid.NewGuid();
        var isOpen = true;

        var cut = Render<UpdatePersonalInfoDrawer>(ps => ps
            .Add(p => p.Person, Person(id))
            .Add(p => p.IsOpen, isOpen)
            .Add(p => p.IsOpenChanged, v => isOpen = v)
            .Add(p => p.OnUpdatePersonal, _ => throw new InvalidOperationException("boom"))
        );

        // зробимо форму валідною
        await cut.InvokeAsync(() =>
        {
            FindByNameOrLabel(cut, "Rnokpp", "РНОКПП (10 цифр)").Change("1234567890");
            FindByNameOrLabel(cut, "LastName", "Прізвище").Change("Іванов");
            FindByNameOrLabel(cut, "FirstName", "Ім’я").Change("Іван");
            FindByNameOrLabel(cut, "MiddleName", "По батькові (опц.)").Change("Іванович");
            FindByNameOrLabel(cut, "Note", "Замітка (опц.)").Change("");
        });

        await cut.InvokeAsync(() => cut.Find("form#update-personal-form").Submit());

        Assert.True(isOpen);

        Assert.Contains(shown, m =>
            m.Kind == ToastKind.Warning &&
            m.Title.Contains("Неможливо виконати дію", StringComparison.Ordinal));
    }

    // -------------------------
    // Helpers
    // -------------------------

    private static IElement FindByNameOrLabel(
        IRenderedComponent<UpdatePersonalInfoDrawer> cut,
        string fieldName,
        string labelText)
    {
        // 1) пробуємо name="<fieldName>"
        var byName = cut.FindAll("input")
            .FirstOrDefault(i => string.Equals(i.GetAttribute("name"), fieldName, StringComparison.OrdinalIgnoreCase));

        if (byName is not null)
            return byName;

        // 2) fallback: знаходимо input через label текст (найстабільніше)
        return FindInputByLabel(cut, labelText);
    }

    private static IElement FindInputByLabel(
        IRenderedComponent<UpdatePersonalInfoDrawer> cut,
        string labelText)
    {
        // шукаємо <label> з потрібним текстом
        var label = cut.FindAll("label")
            .First(l => l.TextContent.Trim().Equals(labelText, StringComparison.Ordinal));

        // беремо найближчий контейнер (col-...) і знаходимо в ньому input
        // (у твоєму markup input завжди поруч у тому ж div)
        var container = label.ParentElement ?? throw new NullReferenceException();
        var input = container.QuerySelector("input");

        return input is null ? throw new InvalidOperationException($"Input not found for label '{labelText}'.") : input;
    }
}