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
            .Add(p => p.OnCreateCandidate, EventCallback.Factory.Create(this, () => Task.CompletedTask)));

        // Assert: title text
        Assert.Contains("Реєстр особового складу", cut.Markup);

        // Assert: button label text (rendered inside your shared <Button> component)
        Assert.Contains("+ Створити кандидата", cut.Markup);
    }

    [Fact]
    public void Clicking_create_button_invokes_callback()
    {
        // Arrange
        var called = 0;

        var cut = Render<RegistryToolbar>(ps => ps
            .Add(p => p.OnCreateCandidate, EventCallback.Factory.Create(this, () => called++)));

        // Act
        // Prefer: find the first actual <button> and click it
        cut.Find("button").Click();

        // Assert
        Assert.Equal(1, called);
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
