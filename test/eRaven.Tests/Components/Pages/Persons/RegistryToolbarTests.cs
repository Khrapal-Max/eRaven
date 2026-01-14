//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// RegistryToolbarTests
//-----------------------------------------------------------------------------

using Bunit;
using eRaven.Components.Pages.Persons.Registry;
using Microsoft.AspNetCore.Components;

namespace eRaven.Tests.Components.Pages.Persons;

public sealed class RegistryToolbarTests : BunitContext
{
    [Fact]
    public void Renders_title_and_create_button_label()
    {
        // Arrange + Act
        var cut = Render<RegistryToolbar>(ps => ps
            .Add(p => p.OnCreateReserved, EventCallback.Factory.Create(this, () => Task.CompletedTask)));

        // Assert: title text
        Assert.Contains("Реєстр особового складу", cut.Markup);

        // Assert: button label text (rendered inside your shared <Button> component)
        Assert.Contains("+ Створити", cut.Markup);
    }

    [Fact]
    public void Clicking_create_button_invokes_callback()
    {
        // Arrange
        var called = 0;

        var cut = Render<RegistryToolbar>(ps => ps
            .Add(p => p.OnCreateReserved,
                EventCallback.Factory.Create(this, () => called++)));

        // Act: знайти саме кнопку "Створити" (а не перший button в DOM)
        var createBtn = cut
            .FindAll("button")
            .Single(b => b.TextContent.Contains("Створити", StringComparison.OrdinalIgnoreCase));

        cut.InvokeAsync(() => createBtn.Click());

        // Assert
        cut.WaitForAssertion(() => Assert.Equal(1, called));
    }


    [Fact]
    public void Callback_can_be_empty_and_click_does_not_throw()
    {
        // Arrange
        var cut = Render<RegistryToolbar>();

        // Act + Assert
        // Should not throw even if OnCreateCandidate is default (no delegate)
        cut.Find("button").Click();
    }
}