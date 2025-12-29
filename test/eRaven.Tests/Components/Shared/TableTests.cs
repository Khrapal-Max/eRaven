//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TableTests -> Table
//-----------------------------------------------------------------------------

using Bunit;
using eRaven.Components.Shared.Table;
using Microsoft.AspNetCore.Components;

namespace eRaven.Tests.Components.Shared;

public class TableTests : BunitContext
{
    private sealed class TestRow
    {
        public required Guid Id { get; init; }
        public required string Name { get; init; }
    }

    private static RenderFragment<TestRow> RowTemplateTestRow() => item => builder =>
    {
        builder.OpenElement(0, "td");
        builder.AddContent(1, item.Name);
        builder.CloseElement();
    };

    private static RenderFragment Header(string text) => builder =>
    {
        builder.OpenElement(0, "th");
        builder.AddContent(1, text);
        builder.CloseElement();
    };

    private static RenderFragment<string> RowTemplateString() => value => builder =>
    {
        builder.OpenElement(0, "td");
        builder.AddContent(1, value);
        builder.CloseElement();
    };

    [Fact]
    public void RenderTableBase()
    {
        // Arrange
        var list = new List<string> { "Foo", "Bar", "Baz" };
        var classParametr = "m-0";

        // Act
        var cut = Render<Table<string>>(parameters => parameters
            .Add(p => p.Class, classParametr)
            .Add(p => p.Items, list)
            .Add(p => p.TableHeader, Header("TableHeader"))
            .Add(p => p.RowTemplate, RowTemplateString())
            .Add(p => p.SelectedItem, list.First())
        );

        // Assert (мінімальна перевірка рендера)
        Assert.NotNull(cut);
        Assert.Equal(classParametr, cut.Instance.Class);

        var rows = cut.FindAll("tbody tr");
        Assert.Equal(3, rows.Count);
    }

    [Fact]
    public void ShouldInvoke_OnClick_and_SelectRow_OnItemClick()
    {
        // Arrange
        var list = new List<string> { "Foo", "Bar", "Baz" };
        string? clicked = null;

        var cut = Render<Table<string>>(parameters => parameters
           .Add(p => p.Items, list)
           .Add(p => p.TableHeader, Header("TableHeader"))
           .Add(p => p.RowTemplate, RowTemplateString())
           .Add(p => p.OnClick, EventCallback.Factory.Create<string>(this, v => clicked = v))
        );

        // Act: клікаємо по 2й строкі ("Bar")
        var rows = cut.FindAll("tbody tr");
        rows[1].Click();

        // Assert 1: callback отримав item
        Assert.Equal("Bar", clicked);

        // Assert 2: компонент виставив SelectedItem
        Assert.Equal("Bar", cut.Instance.SelectedItem);

        // Assert 3: у DOM додався клас table-active на вибраній строкі
        rows = cut.FindAll("tbody tr"); // перечитуємо після ререндеру
        Assert.Contains("table-active", rows[1].GetAttribute("class") ?? string.Empty);
        Assert.DoesNotContain("is-selected", rows[0].GetAttribute("class") ?? string.Empty);
        Assert.DoesNotContain("is-selected", rows[2].GetAttribute("class") ?? string.Empty);
    }

    [Fact]
    public void KeySelector_ShouldControlSelection()
    {
        // Arrange
        var idA = Guid.NewGuid();
        var idB = Guid.NewGuid();

        var items = new List<TestRow>
        {
            new() { Id = idA, Name = "A" },
            new() { Id = idB, Name = "B" }
        };

        // SelectedItem НЕ з цього списку (інший instance), але з таким самим Id => має підсвітитись
        var selected = new TestRow { Id = idB, Name = "B (external instance)" };

        var cut = Render<Table<TestRow>>(parameters => parameters
            .Add(p => p.Items, items)
            .Add(p => p.TableHeader, Header("Name"))
            .Add(p => p.RowTemplate, RowTemplateTestRow())
            .Add(p => p.SelectedItem, selected)
            .Add(p => p.KeySelector, x => x.Id)
        );

        // Act
        var rows = cut.FindAll("tbody tr");

        // Assert: друга строка (B) має бути selected завдяки KeySelector
        Assert.DoesNotContain("table-active", rows[0].GetAttribute("class") ?? string.Empty);
        Assert.Contains("table-active", rows[1].GetAttribute("class") ?? string.Empty);
    }
}