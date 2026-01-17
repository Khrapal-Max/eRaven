//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// RoutesTests
//-----------------------------------------------------------------------------

using Bunit;
using eRaven.Components;
using eRaven.Presentation.Errors;
using eRaven.Presentation.Toasts;
using Microsoft.Extensions.DependencyInjection;

namespace eRaven.Tests.Components;

public sealed class RoutesTests : BunitContext
{
    public RoutesTests()
    {
        Services.AddScoped<ErrorBoundaryHub>();
        Services.AddScoped<ToastService>();
    }

    [Fact]
    public void Should_render_without_exceptions()
    {
        var cut = Render<Routes>();
        Assert.NotNull(cut);
    }
}
