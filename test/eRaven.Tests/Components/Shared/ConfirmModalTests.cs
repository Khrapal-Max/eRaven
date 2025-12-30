//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ConfirmModalTests -> ConfirmModal
//-----------------------------------------------------------------------------

using Bunit;
using eRaven.Components.Shared.ConfirmModal;

namespace eRaven.Tests.Components.Shared;

public class ConfirmModalTests : BunitContext
{
    [Fact]
    public async Task ShowAsync_WithBodyText_RendersTitleAndTextAndBackdrop()
    {
        // Arrange
        var cut = Render<ConfirmModal<string>>(ps => ps
            .Add(p => p.Title, "Заголовок")
            .Add(p => p.ConfirmText, "Так")
            .Add(p => p.CancelText, "Ні"));

        // Act
        var showTask = cut.InvokeAsync(() => cut.Instance.ShowAsync("model", "Текст підтвердження"));

        // Cleanup: cancel so task completes
        await cut.InvokeAsync(cut.Instance.Cancel);
        var result = await showTask;
        Assert.False(result);

        cut.WaitForAssertion(() =>
        {
            Assert.DoesNotContain("modal-backdrop", cut.Markup);
        });
    }

    [Fact]
    public async Task ShowAsync_WithBodyTemplate_RendersTemplateInsteadOfBodyText()
    {
        // Arrange
        var cut = Render<ConfirmModal<string>>(ps => ps
            .Add(p => p.Title, "Підтвердження")
            .Add(p => p.BodyTemplate!, m => builder =>
            {
                builder.OpenElement(0, "div");
                builder.AddAttribute(1, "class", "tpl");
                builder.AddContent(2, $"Custom: {m}");
                builder.CloseElement();
            }));

        // Act
        var showTask = cut.InvokeAsync(() => cut.Instance.ShowAsync("ABC", "Ignored text"));

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Custom: ABC", cut.Markup);
            Assert.DoesNotContain("Ignored text", cut.Markup);
        });

        // Cleanup
        await cut.InvokeAsync(cut.Instance.Cancel);
        Assert.False(await showTask);
    }

    [Fact]
    public async Task Confirm_ReturnsTrue_AndHidesModal()
    {
        // Arrange
        var cut = Render<ConfirmModal<int>>(ps => ps
            .Add(p => p.Title, "Confirm")
            .Add(p => p.ConfirmText, "Підтвердити")
            .Add(p => p.CancelText, "Скасувати"));

        // Act
        var task = cut.InvokeAsync(() => cut.Instance.ShowAsync(123, "Підтвердити дію?"));

        // Wait visible
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Підтвердити дію?", cut.Markup);
            Assert.Contains("modal-backdrop", cut.Markup);
        });

        await cut.InvokeAsync(cut.Instance.Confirm);
        var result = await task;

        // Assert
        Assert.True(result);
        cut.WaitForAssertion(() =>
        {
            Assert.DoesNotContain("modal-backdrop", cut.Markup);
            Assert.DoesNotContain("Підтвердити дію?", cut.Markup);
        });
    }

    [Fact]
    public async Task Cancel_ReturnsFalse_AndHidesModal()
    {
        // Arrange
        var cut = Render<ConfirmModal<Guid>>();

        // Act
        var task = cut.InvokeAsync(() => cut.Instance.ShowAsync(Guid.NewGuid(), "Cancel me"));

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Cancel me", cut.Markup);
            Assert.Contains("modal-backdrop", cut.Markup);
        });

        await cut.InvokeAsync(cut.Instance.Cancel);
        var result = await task;

        // Assert
        Assert.False(result);
        cut.WaitForAssertion(() => Assert.DoesNotContain("modal-backdrop", cut.Markup));
    }

    [Fact]
    public async Task ShowAsync_WhenAlreadyOpen_ThrowsInvalidOperationException()
    {
        // Arrange
        var cut = Render<ConfirmModal<string>>();

        // Act: open modal (do NOT await completion yet)
        var pending = cut.InvokeAsync(() => cut.Instance.ShowAsync("A", "First"));

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("First", cut.Markup);
            Assert.Contains("modal-backdrop", cut.Markup);
        });

        // Assert: second open attempt while busy throws
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await cut.InvokeAsync(() => cut.Instance.ShowAsync("B", "Second"));
        });

        // Cleanup
        await cut.InvokeAsync(cut.Instance.Cancel);
        Assert.False(await pending);
    }
}
