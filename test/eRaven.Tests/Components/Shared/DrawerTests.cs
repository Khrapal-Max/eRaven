//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// DrawerTests (bUnit)
//-----------------------------------------------------------------------------

using Bunit;
using eRaven.Components.Shared.Drawer;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace eRaven.Tests.Components.Shared;

public sealed class DrawerTests : BunitContext
{
    [Fact]
    public void Render_when_closed_should_not_have_show_classes_and_aria_hidden_true()
    {
        var cut = Render<Drawer>(ps => ps
            .Add(p => p.IsOpen, false)
            .Add(p => p.Title, "T"));

        var backdrop = cut.Find(".drawer-backdrop");
        var panel = cut.Find(".drawer-panel");

        Assert.DoesNotContain("show", backdrop.ClassList);
        Assert.DoesNotContain("show", panel.ClassList);

        Assert.Equal("true", backdrop.GetAttribute("aria-hidden"));
        Assert.Equal("dialog", panel.GetAttribute("role"));
        Assert.Equal("true", panel.GetAttribute("aria-modal"));
        Assert.Equal("T", panel.GetAttribute("aria-label"));
    }

    [Fact]
    public void Render_when_open_should_have_show_classes_and_aria_hidden_false_and_title()
    {
        // щоб FocusAsync не падав у тесті
        JSInterop.Mode = JSRuntimeMode.Loose;

        var cut = Render<Drawer>(ps => ps
            .Add(p => p.IsOpen, true)
            .Add(p => p.Title, "My drawer"));

        var backdrop = cut.Find(".drawer-backdrop");
        var panel = cut.Find(".drawer-panel");

        Assert.Contains("show", backdrop.ClassList);
        Assert.Contains("show", panel.ClassList);

        Assert.Equal("false", backdrop.GetAttribute("aria-hidden"));
        Assert.Contains("My drawer", cut.Markup);
    }

    [Fact]
    public void Backdrop_click_should_close_when_allowed_and_invoke_callbacks()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        bool? newIsOpen = null;
        var closed = false;

        var cut = Render<Drawer>(ps => ps
            .Add(p => p.IsOpen, true)
            .Add(p => p.IsOpenChanged, EventCallback.Factory.Create<bool>(this, v => newIsOpen = v))
            .Add(p => p.OnClosed, EventCallback.Factory.Create(this, () => closed = true)));

        cut.Find(".drawer-backdrop").Click();

        Assert.Equal(false, newIsOpen);
        Assert.True(closed);

        // компонент сам змінює IsOpen всередині -> DOM має прибрати show
        Assert.DoesNotContain("show", cut.Find(".drawer-panel").ClassList);
    }

    [Fact]
    public void Backdrop_click_should_not_close_when_disable_close_true()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        bool callbackCalled = false;

        var cut = Render<Drawer>(ps => ps
            .Add(p => p.IsOpen, true)
            .Add(p => p.DisableClose, true)
            .Add(p => p.IsOpenChanged, EventCallback.Factory.Create<bool>(this, _ => callbackCalled = true)));

        cut.Find(".drawer-backdrop").Click();

        Assert.False(callbackCalled);
        Assert.Contains("show", cut.Find(".drawer-panel").ClassList);
    }

    [Fact]
    public void Backdrop_click_should_not_close_when_close_on_backdrop_false()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        bool callbackCalled = false;

        var cut = Render<Drawer>(ps => ps
            .Add(p => p.IsOpen, true)
            .Add(p => p.CloseOnBackdrop, false)
            .Add(p => p.IsOpenChanged, EventCallback.Factory.Create<bool>(this, _ => callbackCalled = true)));

        cut.Find(".drawer-backdrop").Click();

        Assert.False(callbackCalled);
        Assert.Contains("show", cut.Find(".drawer-panel").ClassList);
    }

    [Fact]
    public void Escape_should_close_when_enabled()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        bool? newIsOpen = null;

        var cut = Render<Drawer>(ps => ps
            .Add(p => p.IsOpen, true)
            .Add(p => p.IsOpenChanged, EventCallback.Factory.Create<bool>(this, v => newIsOpen = v)));

        cut.Find(".drawer-panel")
           .KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.Equal(false, newIsOpen);
        Assert.DoesNotContain("show", cut.Find(".drawer-panel").ClassList);
    }

    [Fact]
    public void Escape_should_not_close_when_close_on_escape_false()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        bool callbackCalled = false;

        var cut = Render<Drawer>(ps => ps
            .Add(p => p.IsOpen, true)
            .Add(p => p.CloseOnEscape, false)
            .Add(p => p.IsOpenChanged, EventCallback.Factory.Create<bool>(this, _ => callbackCalled = true)));

        cut.Find(".drawer-panel")
           .KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.False(callbackCalled);
        Assert.Contains("show", cut.Find(".drawer-panel").ClassList);
    }

    [Fact]
    public void Close_button_should_be_disabled_when_disable_close_true()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        var cut = Render<Drawer>(ps => ps
            .Add(p => p.IsOpen, true)
            .Add(p => p.DisableClose, true));

        // кнопка close має disabled
        var btn = cut.Find("button[aria-label='Close']");
        Assert.True(btn.HasAttribute("disabled"));
    }

    [Fact]
    public void OnBeforeClose_should_block_close_when_returns_false()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        bool isOpenChangedCalled = false;
        bool closedCalled = false;

        var cut = Render<Drawer>(ps => ps
            .Add(p => p.IsOpen, true)
            .Add(p => p.IsOpenChanged, EventCallback.Factory.Create<bool>(this, _ => isOpenChangedCalled = true))
            .Add(p => p.OnClosed, EventCallback.Factory.Create(this, () => closedCalled = true))
            .Add(p => p.OnBeforeClose, () => Task.FromResult(false)));

        cut.Find("button[aria-label='Close']").Click();

        Assert.False(isOpenChangedCalled);
        Assert.False(closedCalled);
        Assert.Contains("show", cut.Find(".drawer-panel").ClassList);
    }

    [Fact]
    public void Should_render_header_actions_and_footer_content()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        static void header(RenderTreeBuilder b) => b.AddMarkupContent(0, "<span id='hdr'>H</span>");
        static void footer(RenderTreeBuilder b) => b.AddMarkupContent(0, "<button id='ftr'>F</button>");

        var cut = Render<Drawer>(ps => ps
            .Add(p => p.IsOpen, true)
            .Add(p => p.Title, "T")
            .Add(p => p.HeaderActions, header)
            .Add(p => p.FooterContent, footer)
            .AddChildContent("<div id='body'>Body</div>"));

        cut.Find("#hdr");
        cut.Find("#ftr");
        cut.Find("#body");
    }
}