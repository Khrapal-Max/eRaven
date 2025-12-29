//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ModalTests -> Modal
//-----------------------------------------------------------------------------

using Bunit;
using eRaven.Components.Shared.Modal;
using Microsoft.AspNetCore.Components;

namespace eRaven.Tests.Components.Shared;

public class ModalTests : BunitContext
{
    [Fact]
    public void Should_Not_Render_When_IsOpen_False()
    {
        // Act
        var cut = Render<Modal>(ps => ps
            .Add(p => p.IsOpen, false)
            .AddChildContent("<div>Body</div>"));

        // Assert
        Assert.DoesNotContain("modal-backdrop", cut.Markup);
        Assert.DoesNotContain("modal-content", cut.Markup);
    }

    [Fact]
    public void Should_Render_Title_And_ChildContent_When_IsOpen_True()
    {
        // Act
        var cut = Render<Modal>(ps => ps
            .Add(p => p.IsOpen, true)
            .Add(p => p.Title, "Створення посади")
            .AddChildContent("<div class='body-test'>Hello</div>"));

        // Assert
        Assert.Contains("Створення посади", cut.Markup);
        Assert.Contains("body-test", cut.Markup);
        Assert.Contains("modal-backdrop", cut.Markup);
    }

    [Fact]
    public void Cancel_Click_Should_Request_Close()
    {
        // Arrange
        var isOpen = true;

        var cut = Render<Modal>(ps => ps
            .Add(p => p.IsOpen, true)
            .Add(p => p.IsOpenChanged, v => isOpen = v)
            .AddChildContent("<div>Body</div>"));

        // Act: first button in footer is Cancel (secondary)
        var buttons = cut.FindAll("button");
        Assert.True(buttons.Count >= 2);

        buttons[0].Click();

        // Assert
        Assert.False(isOpen);
    }

    [Fact]
    public async Task Create_Click_Should_Close_When_OnCreateAsync_Returns_True()
    {
        // Arrange
        var isOpen = true;

        static Task<bool> OnCreateAsync() => Task.FromResult(true);

        var cut = Render<Modal>(ps => ps
            .Add(p => p.IsOpen, true)
            .Add(p => p.IsOpenChanged, v => isOpen = v)
            .Add(p => p.OnCreateAsync, OnCreateAsync)
            .AddChildContent("<div>Body</div>"));

        // Act: second button is Create (primary)
        var buttons = cut.FindAll("button");
        buttons[1].Click();

        // Allow async callbacks to run
        await cut.InvokeAsync(() => Task.CompletedTask);

        // Assert
        Assert.False(isOpen);
    }

    [Fact]
    public async Task Create_Click_Should_Stay_Open_When_OnCreateAsync_Returns_False()
    {
        // Arrange
        var isOpen = true;

        static Task<bool> OnCreateAsync() => Task.FromResult(false);

        var cut = Render<Modal>(ps => ps
            .Add(p => p.IsOpen, true)
            .Add(p => p.IsOpenChanged, v => isOpen = v)
            .Add(p => p.OnCreateAsync, OnCreateAsync)
            .AddChildContent("<div>Body</div>"));

        // Act
        var buttons = cut.FindAll("button");
        buttons[1].Click();
        await cut.InvokeAsync(() => Task.CompletedTask);

        // Assert (modal should remain open)
        Assert.True(isOpen);
    }

    [Fact]
    public async Task When_IsBusy_True_Clicks_Should_Do_Nothing()
    {
        // Arrange
        var isOpen = true;
        var cancelCalled = false;
        var createCalled = false;

        async Task<bool> OnCreateAsync()
        {
            createCalled = true;
            await Task.Yield();
            return true;
        }

        Task OnCancel()
        {
            cancelCalled = true;
            return Task.CompletedTask;
        }

        var cut = Render<Modal>(ps => ps
            .Add(p => p.IsOpen, true)
            .Add(p => p.IsBusy, true)
            .Add(p => p.IsOpenChanged, v => isOpen = v)
            .Add(p => p.OnCreateAsync, OnCreateAsync)
            .Add(p => p.OnCancel, EventCallback.Factory.Create(this, OnCancel))
            .AddChildContent("<div>Body</div>"));

        var buttons = cut.FindAll("button");
        Assert.True(buttons.Count >= 2);

        // Act
        buttons[0].Click(); // cancel
        buttons[1].Click(); // create
        await cut.InvokeAsync(() => Task.CompletedTask);

        // Assert: no actions because IsBusy
        Assert.True(isOpen);
        Assert.False(cancelCalled);
        Assert.False(createCalled);

        // Also ensure disabled attribute exists
        Assert.True(buttons[0].HasAttribute("disabled"));
        Assert.True(buttons[1].HasAttribute("disabled"));
    }
}
