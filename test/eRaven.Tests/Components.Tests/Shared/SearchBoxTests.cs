//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// SearchBoxTests
//-----------------------------------------------------------------------------

using Bunit;
using eRaven.Components.Shared.SearchBox;
using Microsoft.AspNetCore.Components;

namespace eRaven.Tests.Components.Tests.Shared;

public sealed class SearchBoxTests : TestContext
{
    [Fact]
    public async Task Reload_Click_Fires_Immediately_When_Enabled()
    {
        // Arrange
        var calls = 0;
        var cut = RenderComponent<SearchBox>(ps => ps
            .Add(p => p.Disabled, false)
            .Add(p => p.OnSearch, EventCallback.Factory.Create(this, () => calls++))
        );

        // Act
        cut.Find("button.btn").Click();
        await Task.Delay(1);

        // Assert
        Assert.Equal(1, calls);
    }

    [Fact]
    public void Disabled_Button_HasNoClickHandler_And_IsDisabled()
    {
        // Arrange
        var calls = 0;
        var cut = RenderComponent<SearchBox>(ps => ps
            .Add(p => p.Disabled, true)
            .Add(p => p.OnSearch, EventCallback.Factory.Create(this, () => calls++))
        );

        var btn = cut.Find("button.btn");

        // Assert: кнопка вимкнена
        Assert.True(btn.HasAttribute("disabled"));

        // Assert: клік відсутній -> кидає MissingEventHandlerException
        Assert.Throws<MissingEventHandlerException>(() => btn.Click());

        // і, звісно, OnSearch не викликано
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task TwoWayBinding_Value_Updates_On_Input()
    {
        // Arrange
        var bound = "init";
        var cut = RenderComponent<SearchBox>(ps => ps
            .Add(p => p.Delay, 1)
            .Bind(p => p.Value, bound, v => bound = v)
        );

        var input = cut.Find("input.form-control");

        // Act
        input.Input("нове значення");
        await Task.Delay(5); // даємо Blazor завершити @bind

        // Assert
        Assert.Equal("нове значення", bound);
    }

    [Fact]
    public void ParentToChild_Syncs_When_Parameter_Changes()
    {
        // Arrange
        var cut = RenderComponent<SearchBox>(ps => ps.Add(p => p.Value, "початкове"));

        // Act: батько змінює параметр Value
        cut.SetParametersAndRender(ps => ps.Add(p => p.Value, "оновлене"));

        // Assert: інпут відобразив нове значення
        var input = cut.Find("input.form-control");
        Assert.Equal("оновлене", input.GetAttribute("value"));
    }

    [Fact]
    public void Renders_Placeholder_And_Icon()
    {
        // Arrange
        var cut = RenderComponent<SearchBox>(ps => ps
            .Add(p => p.Placeholder, "Custom PH")
            .Add(p => p.Icon, "bi bi-lightning")
        );

        // Assert
        Assert.Equal("Custom PH", cut.Find("input.form-control").GetAttribute("placeholder"));
        Assert.NotNull(cut.Find("span.input-group-text i.bi.bi-lightning"));
    }

    [Fact]
    public void Dispose_DoesNotThrow()
    {
        // Arrange & Act & Assert (просто не має кинути)
        var cut = RenderComponent<SearchBox>();
        cut.Dispose();
    }

    [Fact]
    public async Task Reload_CancelsPendingDebounce()
    {
        // Arrange
        var calls = 0;
        var cut = RenderComponent<SearchBox>(ps => ps
            .Add(p => p.Delay, 200)
            .Add(p => p.OnSearch, EventCallback.Factory.Create(this, () => calls++))
        );

        var input = cut.Find("input.form-control");

        // Act: стартуємо дебаунс та відразу тиснемо Reload (який має скасувати CTS)
        input.Input("a"); // oninput → bind:after → стартує відкладений виклик
        await cut.InvokeAsync(() => cut.Find("button.btn").Click());

        // Assert (миттєво після Reload) — рівно 1 виклик (через Reload)
        Assert.Equal(1, calls);

        // Чекаємо трохи довше за Delay, аби переконатись, що відкладений виклик не “догнав”
        await Task.Delay(350); // > 200ms з запасом

        // Досі лише 1 виклик
        Assert.Equal(1, calls);
    }

    [Fact]
    public void Disabled_DisablesInputAndButton()
    {
        var cut = RenderComponent<SearchBox>(ps => ps.Add(p => p.Disabled, true));
        Assert.True(cut.Find("input.form-control").HasAttribute("disabled"));
        Assert.True(cut.Find("button.btn").HasAttribute("disabled"));
    }
}