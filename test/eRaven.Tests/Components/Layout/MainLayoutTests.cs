//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// MainLayoutTests -> MainLayout
//-----------------------------------------------------------------------------

using Bunit;
using eRaven.Components.Layout;
using eRaven.Presentation.Errors;
using eRaven.Presentation.Toasts;
using Microsoft.Extensions.DependencyInjection;

namespace eRaven.Tests.Components.Layout;

public sealed class MainLayoutTests : BunitContext
{
    public MainLayoutTests()
    {
        Services.AddScoped<ErrorBoundaryHub>();
        Services.AddScoped<ToastService>(); // потрібен для <ToastContainer />
    }

    [Fact]
    public async Task RecoverRequested_ShouldRecoverErrorBoundary_AndRenderBodyAgain()
    {
        // Arrange
        var hub = Services.GetRequiredService<ErrorBoundaryHub>();
        var shouldThrow = true;

        var cut = Render<MainLayout>(p => p.Add(
            x => x.Body, builder =>
            {
                if (shouldThrow)
                    throw new InvalidOperationException("boom");

                builder.OpenElement(0, "div");
                builder.AddAttribute(1, "id", "ok");
                builder.AddContent(2, "OK");
                builder.CloseElement();
            })
        );

        // Assert: маємо побачити ErrorContent
        cut.WaitForAssertion(() =>
            Assert.Contains("Сталася неочікувана помилка.", cut.Markup));

        // Act: "виправляємо" причину і просимо Recover (ВАЖЛИВО: через Dispatcher)
        shouldThrow = false;
        await cut.InvokeAsync(() => hub.RequestRecover());

        // Assert: ErrorBoundary має відновитись і показати Body
        cut.WaitForAssertion(() =>
        {
            Assert.DoesNotContain("Сталася неочікувана помилка.", cut.Markup);
            Assert.Contains("OK", cut.Markup);
            _ = cut.Find("#ok"); // просто перевірка, що елемент існує
        });
    }
}
