//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ButtonComponentTests -> ButtonComponent
//-----------------------------------------------------------------------------

using Bunit;
using eRaven.Components.Shared.ButtonComponent;
using Microsoft.AspNetCore.Components;

namespace eRaven.Tests.Components.Shared;

public class ButtonComponentTests : BunitContext
{
    [Fact]
    public void ShouldRenderButton()
    {
        //Arrange
        var clicked = false;
        void OnClick() => clicked = true;

        //Act
        var cut = Render<ButtonComponent>(parameters => parameters
            .Add(p => p.Label, "Button")
            .Add(p => p.Class, "custom-class")
            .Add(p => p.Style, "width: 100px;")
            .Add(p => p.IsDisabled, false)
            .Add(p => p.ChildContent, builder =>
            {
                builder.AddMarkupContent(0, "<h1>Hello World</h1>");
            })
            .Add(p => p.OnClickButton, EventCallback.Factory.Create(this, OnClick))
        );

        cut.Instance.ClickButton();

        //Assert
        Assert.NotNull(cut);
        Assert.False(cut.Instance.IsDisabled);
        Assert.True(clicked);
    }

    [Fact]
    public void Button_Is_Not_Disabled_By_Default()
    {
        // Act
        var cut = Render<ButtonComponent>();

        // Assert
        var button = cut.Find("button");
        Assert.False(button.HasAttribute("disabled"));
    }
}