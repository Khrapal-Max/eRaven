//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// DropDownTests -> DropDown
//-----------------------------------------------------------------------------

using Bunit;
using eRaven.Components.Shared.DropDown;
using Microsoft.AspNetCore.Components;

namespace eRaven.Tests.Components.Shared;

public class DropDownTests : BunitContext, IDisposable
{
    public enum TestEnum { One, Two, Three }

    [Fact]
    public void Renders_All_Enum_Options()
    {
        var enums = Enum.GetValues<TestEnum>().ToList();

        var cut = Render<DropDown<TestEnum>>(parameters => parameters
            .Add(p => p.EnumNames, enums)
            .Add(p => p.Value, TestEnum.Two)
        );

        var options = cut.FindAll("option");
        Assert.Equal(3, options.Count);
        Assert.Equal("One", options[0].TextContent);
        Assert.Equal("Two", options[1].TextContent);
        Assert.Equal("Three", options[2].TextContent);
    }

    [Fact]
    public void Calls_ValueChanged_On_Change()
    {
        var selected = TestEnum.One;
        var enums = Enum.GetValues<TestEnum>().ToList();

        var cut = Render<DropDown<TestEnum>>(parameters => parameters
            .Add(p => p.EnumNames, enums)
            .Add(p => p.ValueChanged, EventCallback.Factory.Create<TestEnum>(this, v => selected = v))
        );

        cut.Find("select").Change("Three");
        Assert.Equal(TestEnum.Three, selected);
    }

    [Fact]
    public void Applies_Css_Class_To_Select_Element()
    {
        var enums = Enum.GetValues<TestEnum>().ToList();

        var cut = Render<DropDown<TestEnum>>(parameters => parameters
            .Add(p => p.EnumNames, enums)
            .Add(p => p.Class, "form-control custom-class")
        );

        var select = cut.Find("select");

        Assert.Contains("form-control", select.ClassName);
        Assert.Contains("custom-class", select.ClassName);
    }
}
