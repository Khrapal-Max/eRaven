//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// RegistryToolbarTests
//-----------------------------------------------------------------------------

using Bunit;
using eRaven.Application.DTOs.Person;
using eRaven.Components.Pages.Persons.Registry;
using eRaven.Domain.Enums;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace eRaven.Tests.Components.Pages.Persons.Registry;

public sealed class RegistryToolbarTests : BunitContext
{
    [Fact]
    public void Renders_title_and_main_buttons()
    {
        var cut = Render<RegistryToolbar>(ps => ps
            .Add(p => p.Value, new PersonsRegistryFilters())
        );

        Assert.Contains("Реєстр особового складу", cut.Markup);

        // Основні кнопки
        Assert.Contains("Імпорт/Експорт", cut.Markup);
        Assert.Contains("+ Створити", cut.Markup);
        Assert.Contains("Скинути", cut.Markup);
    }

    [Fact]
    public void Clicking_create_button_invokes_callback()
    {
        var called = 0;

        var cut = Render<RegistryToolbar>(ps => ps
            .Add(p => p.Value, new PersonsRegistryFilters())
            .Add(p => p.OnCreateReserved, EventCallback.Factory.Create(this, () => called++))
        );

        var createBtn = cut.FindAll("button")
            .Single(b => b.TextContent.Contains("Створити", StringComparison.OrdinalIgnoreCase));

        createBtn.Click();

        cut.WaitForAssertion(() => Assert.Equal(1, called));
    }

    [Fact]
    public void Clicking_import_export_invokes_callback()
    {
        var called = 0;

        var cut = Render<RegistryToolbar>(ps => ps
            .Add(p => p.Value, new PersonsRegistryFilters())
            .Add(p => p.OnOpenImportExport, EventCallback.Factory.Create(this, () => called++))
        );

        var btn = cut.FindAll("button")
            .Single(b => b.TextContent.Contains("Імпорт/Експорт", StringComparison.OrdinalIgnoreCase));

        btn.Click();

        cut.WaitForAssertion(() => Assert.Equal(1, called));
    }

    [Fact]
    public void Reset_is_disabled_when_no_filters_and_enabled_when_filters_present()
    {
        // no filters => disabled
        {
            var cut = Render<RegistryToolbar>(ps => ps.Add(p => p.Value, new PersonsRegistryFilters()));

            var resetBtn = cut.FindAll("button")
                .Single(b => b.TextContent.Trim().Equals("Скинути", StringComparison.OrdinalIgnoreCase));

            Assert.True(resetBtn.HasAttribute("disabled"));
        }

        // with filters => enabled
        {
            var cut = Render<RegistryToolbar>(ps => ps.Add(p => p.Value, new PersonsRegistryFilters(
                Search: "x",
                Lifecycle: PersonLifecycle.Enrolled,
                EnrollmentKind: null
            )));

            var resetBtn = cut.FindAll("button")
                .Single(b => b.TextContent.Trim().Equals("Скинути", StringComparison.OrdinalIgnoreCase));

            Assert.False(resetBtn.HasAttribute("disabled"));
        }
    }

    [Fact]
    public void Clicking_lifecycle_filter_invokes_ValueChanged_with_expected_value()
    {
        PersonsRegistryFilters? got = null;

        var cut = Render<RegistryToolbar>(ps => ps
            .Add(p => p.Value, new PersonsRegistryFilters())
            .Add(p => p.ValueChanged, EventCallback.Factory.Create<PersonsRegistryFilters>(this, f => got = f))
        );

        var enrolledBtn = cut.FindAll("button")
            .Single(b => b.TextContent.Trim().Equals("В табелі", StringComparison.OrdinalIgnoreCase));

        enrolledBtn.Click();

        cut.WaitForAssertion(() =>
        {
            Assert.NotNull(got);
            Assert.Equal(PersonLifecycle.Enrolled, got!.Lifecycle);
        });
    }

    [Fact]
    public void Search_enter_applies_trimmed_text_and_escape_clears()
    {
        PersonsRegistryFilters? got = null;

        var cut = Render<RegistryToolbar>(ps => ps
            .Add(p => p.Value, new PersonsRegistryFilters())
            .Add(p => p.ValueChanged, EventCallback.Factory.Create<PersonsRegistryFilters>(this, f => got = f))
        );

        var input = cut.Find("input[placeholder='ПІБ або РНОКПП']");

        // emulate @bind:event="oninput"
        input.TriggerEvent("oninput", new ChangeEventArgs { Value = "  Іванов  " });

        // Enter => apply
        input.KeyDown(new KeyboardEventArgs { Key = "Enter" });

        cut.WaitForAssertion(() =>
        {
            Assert.NotNull(got);
            Assert.Equal("Іванов", got!.Search);
        });

        // type again, then Escape => clear
        got = null;
        input.TriggerEvent("oninput", new ChangeEventArgs { Value = "  тест  " });
        input.KeyDown(new KeyboardEventArgs { Key = "Escape" });

        cut.WaitForAssertion(() =>
        {
            Assert.NotNull(got);
            Assert.Null(got!.Search);
        });
    }

    [Fact]
    public void Callback_can_be_empty_and_click_does_not_throw()
    {
        // Нічого не передали в callbacks — не повинно падати
        var cut = Render<RegistryToolbar>(ps => ps.Add(p => p.Value, new PersonsRegistryFilters()));

        var createBtn = cut.FindAll("button")
            .Single(b => b.TextContent.Contains("Створити", StringComparison.OrdinalIgnoreCase));

        createBtn.Click();
    }
}
