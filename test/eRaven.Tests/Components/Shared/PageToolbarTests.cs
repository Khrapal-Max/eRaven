//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PageToolbarTests -> PageToolbar
//-----------------------------------------------------------------------------

using Bunit;
using eRaven.Components.Shared.PageToolbar;

namespace eRaven.Tests.Components.Shared;

public class PageToolbarTests : BunitContext
{
    [Fact]
    public void Renders_Title_When_Provided()
    {
        // Act
        var cut = Render<PageToolbar>(ps => ps
            .Add(p => p.Title, "Посади")
            .Add(p => p.Right, b =>
            {
                b.AddMarkupContent(0, "<button id='right-btn'>Right</button>");
            })
        );

        // Assert
        cut.Markup.Contains("Посади");
        cut.Find("#right-btn");
    }

    [Fact]
    public void Does_Not_Render_Title_When_Null()
    {
        // Act
        var cut = Render<PageToolbar>(ps => ps
            .Add(p => p.Title, null)
            .Add(p => p.Right, b =>
            {
                b.AddMarkupContent(0, "<button id='right-btn'>Right</button>");
            })
        );

        // Assert
        Assert.DoesNotContain("Посади", cut.Markup); // просто приклад
        cut.Find("#right-btn"); // Right все одно має бути
    }

    [Fact]
    public void Renders_Left_When_Provided()
    {
        // Act
        var cut = Render<PageToolbar>(ps => ps
            .Add(p => p.Title, "Test")
            .Add(p => p.Left, b =>
            {
                b.AddMarkupContent(0, "<span id='left-slot'>Left</span>");
            })
            .Add(p => p.Right, b =>
            {
                b.AddMarkupContent(0, "<span id='right-slot'>Right</span>");
            })
        );

        // Assert
        cut.Find("#left-slot");
        cut.Find("#right-slot");
    }

    [Fact]
    public void Renders_Right_Content()
    {
        // Act
        var cut = Render<PageToolbar>(ps => ps
            .Add(p => p.Title, "Test")
            .Add(p => p.Right, b =>
            {
                b.AddMarkupContent(0, "<div id='actions'><button>Створити</button></div>");
            })
        );

        // Assert
        cut.Find("#actions");
        Assert.Contains("Створити", cut.Markup);
    }
}
