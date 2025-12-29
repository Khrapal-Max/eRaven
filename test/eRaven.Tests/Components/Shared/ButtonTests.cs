//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ButtonTests -> Button
//-----------------------------------------------------------------------------

using Bunit;
using eRaven.Components.Shared.Button;
using Microsoft.AspNetCore.Components;

namespace eRaven.Tests.Components.Shared;

public class ButtonTests : BunitContext
{
    [Fact]
    public void ShouldRenderButton_And_Invoke_Click()
    {
        // Arrange
        var clicked = false;
        void OnClick() => clicked = true;

        // Act
        var cut = Render<Button>(parameters => parameters
            .Add(p => p.Label, "Button")
            .Add(p => p.Class, "custom-class")
            .Add(p => p.Style, "width: 100px;")
            .Add(p => p.IsDisabled, false)
            .Add(p => p.OnClickButton, EventCallback.Factory.Create(this, OnClick))
        );

        cut.Find("button").Click();

        // Assert
        Assert.NotNull(cut);
        Assert.False(cut.Instance.IsDisabled);
        Assert.True(clicked);
    }

    [Fact]
    public void Button_Is_Not_Disabled_By_Default()
    {
        // Act
        var cut = Render<Button>();

        // Assert
        var button = cut.Find("button");
        Assert.False(button.HasAttribute("disabled"));
    }

    [Fact]
    public void StopPropagation_Is_False_By_Default()
    {
        // Act
        var cut = Render<Button>();

        // Assert
        Assert.False(cut.Instance.StopPropagation);

        // Blazor не рендерить атрибут, якщо false
        var markup = cut.Markup;
        Assert.DoesNotContain("stopPropagation", markup);
    }

    [Fact]
    public void StopPropagation_True_Renders_StopPropagation_Attribute()
    {
        // Act
        var cut = Render<Button>(parameters => parameters
            .Add(p => p.StopPropagation, true)
        );

        // Assert
        Assert.True(cut.Instance.StopPropagation);

        // Перевіряємо, що атрибут реально присутній у markup
        var markup = cut.Markup;
        Assert.Contains("stopPropagation", markup);
    }
}
