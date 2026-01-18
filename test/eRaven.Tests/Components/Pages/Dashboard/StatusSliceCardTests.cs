//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// StatusSliceCardTests
//-----------------------------------------------------------------------------

using Bunit;
using eRaven.Components.Pages.Dashboard;
using Microsoft.AspNetCore.Components;

namespace eRaven.Tests.Components.Pages.Dashboard;

public sealed class StatusSliceCardTests : BunitContext
{
    [Fact]
    public void Renders_hint_count_and_badge_class()
    {
        // Arrange
        const string hint = "В табелі (всього)";
        const int count = 123;
        const string badgeClass = "text-bg-success";

        // Act
        var cut = Render<StatusSliceCard>(ps => ps
            .Add(p => p.Hint, hint)
            .Add(p => p.Count, count)
            .Add(p => p.BadgeClass, badgeClass)
        );

        // Assert
        // Hint
        var hintEl = cut.Find(".fw-semibold");
        Assert.Equal(hint, hintEl.TextContent.Trim());

        // Badge class + count text
        var badgeEl = cut.Find("span.badge");
        Assert.Contains(badgeClass, badgeEl.ClassList);
        Assert.Equal(count.ToString(), badgeEl.TextContent.Trim());
    }

    [Fact]
    public void Click_invokes_OnClick_callback()
    {
        // Arrange
        var clicked = false;

        var cut = Render<StatusSliceCard>(ps => ps
            .Add(p => p.Hint, "Test")
            .Add(p => p.Count, 1)
            .Add(p => p.OnClick, EventCallback.Factory.Create(this, () => clicked = true))
        );

        // Act
        cut.Find("div.card").Click();

        // Assert
        Assert.True(clicked);
    }

    [Fact]
    public void Click_without_delegate_does_not_throw()
    {
        // Arrange
        var cut = Render<StatusSliceCard>(ps => ps
            .Add(p => p.Hint, "Test")
            .Add(p => p.Count, 1)
        );

        // Act + Assert
        var ex = Record.Exception(() => cut.Find("div.card").Click());
        Assert.Null(ex);
    }
}
