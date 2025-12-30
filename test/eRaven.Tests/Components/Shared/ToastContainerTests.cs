//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ToastContainerTests -> ToastContainer
//-----------------------------------------------------------------------------

using Bunit;
using eRaven.Components.Shared.ToastContainer;
using eRaven.Presentation.Toasts;
using Microsoft.Extensions.DependencyInjection;

namespace eRaven.Tests.Components.Shared;

public sealed class ToastContainerTests : BunitContext
{
    public ToastContainerTests()
    {
        Services.AddScoped<ToastService>();
    }

    [Fact]
    public void Show_ShouldRenderToast_WithTitleAndBody()
    {
        // Arrange
        var toasts = Services.GetRequiredService<ToastService>();
        var cut = Render<ToastContainer>();

        // Act
        toasts.Show(new ToastMessage(
            Kind: ToastKind.Info,
            Title: "Title 1",
            Body: "Body 1",
            AutoHideMs: 0)); // щоб не зник сам

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Contains(cut.FindAll("strong.me-auto").Select(x => x.TextContent), t => t == "Title 1");
            Assert.Contains(cut.FindAll(".toast-body").Select(x => x.TextContent), b => b == "Body 1");
        });
    }

    [Fact]
    public void CloseButton_Click_ShouldRemoveToast()
    {
        // Arrange
        var toasts = Services.GetRequiredService<ToastService>();
        var cut = Render<ToastContainer>();

        toasts.Show(new ToastMessage(
            Kind: ToastKind.Warning,
            Title: "Closable",
            Body: "Some body",
            AutoHideMs: 0));

        cut.WaitForAssertion(() => Assert.Contains("Closable", cut.Markup));

        // Act
        // у тосту є один btn-close — клікнемо по ньому
        cut.Find("button.btn-close").Click();

        // Assert
        cut.WaitForAssertion(() => Assert.DoesNotContain("Closable", cut.Markup));
    }

    [Fact]
    public void AutoHide_ShouldRemoveToast_AfterDelay()
    {
        // Arrange
        var toasts = Services.GetRequiredService<ToastService>();
        var cut = Render<ToastContainer>();

        toasts.Show(new ToastMessage(
            Kind: ToastKind.Success,
            Title: "AutoHide",
            Body: null,
            AutoHideMs: 20)); // дуже коротко для тесту

        cut.WaitForAssertion(() => Assert.Contains("AutoHide", cut.Markup));

        // Assert: дочекаємось, що тост зникне
        cut.WaitForAssertion(
            () => Assert.DoesNotContain("AutoHide", cut.Markup),
            timeout: TimeSpan.FromMilliseconds(500));
    }
}
