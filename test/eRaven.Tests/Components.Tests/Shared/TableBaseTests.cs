//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TableBaseTests
//-----------------------------------------------------------------------------

using Bunit;
using eRaven.Components.Shared.TableBaseComponent;
using Microsoft.AspNetCore.Components;

namespace eRaven.Tests.Components.Tests.Shared;

public class TableBaseTests : TestContext
{
    // ✅ Хелпер без seq++ / інкрементів усередині аргументів.
    // Варіант 1: константні sequence numbers (рекомендовано й достатньо)
    private static RenderFragment Header(params string[] cols) => builder =>
    {
        foreach (var c in cols)
        {
            builder.OpenElement(0, "th");          // константи всередині циклу — ок для Blazor
            builder.AddAttribute(1, "scope", "col");
            builder.AddContent(2, c);
            builder.CloseElement();
        }
    };

    private static RenderFragment<string> RowTmpl => value => builder =>
    {
        builder.OpenElement(0, "td");
        builder.AddContent(1, value);
        builder.CloseElement();
    };

    [Fact]
    public void Renders_WithStickyHeader_AndInnerScroll()
    {
        var items = new[] { "A", "B" };

        var cut = RenderComponent<TableBaseComponent<string>>(ps => ps
            .Add(p => p.Class, "m-0")
            .Add(p => p.Items, items)
            .Add(p => p.TableHeader, Header("Col 1"))
            .Add(p => p.RowTemplate, RowTmpl)
            .Add(p => p.MaxHeight, "400px")
            .Add(p => p.MinHeight, "100px")
        );

        var markup = cut.Markup;
        Assert.Contains("class=\"table-scroll", markup);
        Assert.Contains("<thead", markup);
        Assert.Contains("<th", markup);
        Assert.Contains("class=\"table table-hover fs-6 w-100 mb-0 m-0", markup);
    }

    [Fact]
    public void RowClick_SetsSelected_AndAddsSelectedClass_ManualParams()
    {
        // Arrange
        var items = new[] { "Foo", "Bar", "Baz" };
        string? selected = null;

        var cut = RenderComponent<TableBaseComponent<string>>(ps => ps
            .Add(p => p.Items, items)
            .Add(p => p.TableHeader, Header("Name"))
            .Add(p => p.RowTemplate, RowTmpl)
            .Add(p => p.SelectedItem, selected)
            .Add(p => p.SelectedItemChanged, EventCallback.Factory.Create<string?>(this, v => selected = v))
        );

        // Act
        cut.FindAll("tbody tr")[1].Click(); // клік по "Bar"

        // ВАЖЛИВО: оновити параметр компонента значенням з батька
        cut.SetParametersAndRender(ps => ps.Add(p => p.SelectedItem, selected));

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Equal("Bar", selected);
            var rowsNow = cut.FindAll("tbody tr"); // перечитуємо DOM тут, а не користуємось старим rows
            var cls = rowsNow[1].GetAttribute("class") ?? string.Empty;
            Assert.Contains("is-selected", cls);
            Assert.Equal("true", rowsNow[1].GetAttribute("aria-selected"));
        });
    }

    [Fact]
    public void RowClick_Fires_OnClick()
    {
        var items = new[] { "X", "Y" };
        string? clicked = null;

        var cut = RenderComponent<TableBaseComponent<string>>(ps => ps
            .Add(p => p.Items, items)
            .Add(p => p.TableHeader, Header("Name"))
            .Add(p => p.RowTemplate, RowTmpl)
            .Add(p => p.OnClick, EventCallback.Factory.Create<string>(this, v => clicked = v))
        );

        cut.FindAll("tbody tr")[0].Click();
        Assert.Equal("X", clicked);
    }

    [Fact]
    public void SelectedItem_Preselected_Row_HasSelectedClass_AndAria()
    {
        var items = new[] { "L", "M" };

        var cut = RenderComponent<TableBaseComponent<string>>(ps => ps
            .Add(p => p.Items, items)
            .Add(p => p.TableHeader, Header("Name"))
            .Add(p => p.RowTemplate, RowTmpl)
            .Add(p => p.SelectedItem, "M")
        );

        var rows = cut.FindAll("tbody tr");
        Assert.DoesNotContain("is-selected", rows[0].GetAttribute("class") ?? string.Empty);
        Assert.Contains("is-selected", rows[1].GetAttribute("class") ?? string.Empty);
        Assert.Equal("true", rows[1].GetAttribute("aria-selected"));
    }
}