//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TableTests -> Table
//-----------------------------------------------------------------------------

using Bunit;
using eRaven.Components.Shared.Table;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace eRaven.Tests.Components.Shared;

public class TableTests : BunitContext
{
    //======================================================================
    // Test helpers
    //======================================================================

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

    //======================================================================
    // Host component: emulates @bind-SelectedItem
    //======================================================================

    /// <summary>
    /// Host для тесту selection через @bind-SelectedItem:
    /// - тримає Selected у себе
    /// - у SelectedItemChanged оновлює Selected і викликає StateHasChanged()
    /// </summary>
    private sealed class BindHost : ComponentBase
    {
        [Parameter] public IReadOnlyCollection<string> Items { get; set; } = Array.Empty<string>();

        public string? Selected { get; private set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<Table<string>>(0);

            builder.AddAttribute(1, nameof(Table<string>.Items), Items);
            builder.AddAttribute(2, nameof(Table<string>.Class), "m-0");
            builder.AddAttribute(3, nameof(Table<string>.TableHeader), Header("TableHeader"));
            builder.AddAttribute(4, nameof(Table<string>.RowTemplate), RowTemplateString());

            // параметр SelectedItem + callback = selection enabled
            builder.AddAttribute(5, nameof(Table<string>.SelectedItem), Selected);
            builder.AddAttribute(6, nameof(Table<string>.SelectedItemChanged),
                EventCallback.Factory.Create<string?>(this, async (string? v) =>
                {
                    Selected = v;
                    await InvokeAsync(StateHasChanged);
                }));

            builder.CloseComponent();
        }
    }

    //======================================================================
    // Tests
    //======================================================================

    [Fact]
    public void RenderTableBase_ShouldRenderRows_AndNotHighlight_WhenSelectionDisabled()
    {
        // Arrange
        var list = new List<string> { "Foo", "Bar", "Baz" };

        // Act
        var cut = Render<Table<string>>(parameters => parameters
            .Add(p => p.Class, "m-0")
            .Add(p => p.Items, list)
            .Add(p => p.TableHeader, Header("TableHeader"))
            .Add(p => p.RowTemplate, RowTemplateString())
            // SelectedItem заданий, але selection НЕ увімкнений (бо немає SelectedItemChanged)
            .Add(p => p.SelectedItem, list.First())
        );

        // Assert
        var rows = cut.FindAll("tbody tr");
        Assert.Equal(3, rows.Count);

        // selection disabled => table-active не має бути
        Assert.DoesNotContain("table-active", rows[0].GetAttribute("class") ?? string.Empty);
        Assert.DoesNotContain("table-active", rows[1].GetAttribute("class") ?? string.Empty);
        Assert.DoesNotContain("table-active", rows[2].GetAttribute("class") ?? string.Empty);
    }

    [Fact]
    public void ShouldInvoke_OnClick_OnItemClick_ButNotSelectRow_WhenSelectionDisabled()
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

        // Act
        var rows = cut.FindAll("tbody tr");
        rows[1].Click(); // "Bar"

        // Assert: OnClick викликано
        Assert.Equal("Bar", clicked);

        // selection disabled => компонент НЕ встановлює SelectedItem сам
        Assert.Null(cut.Instance.SelectedItem);

        // і row не підсвічується
        rows = cut.FindAll("tbody tr");
        Assert.DoesNotContain("table-active", rows[0].GetAttribute("class") ?? string.Empty);
        Assert.DoesNotContain("table-active", rows[1].GetAttribute("class") ?? string.Empty);
        Assert.DoesNotContain("table-active", rows[2].GetAttribute("class") ?? string.Empty);
    }

    [Fact]
    public void Selection_BindFlow_ShouldHighlightRow_AfterClick()
    {
        // Arrange
        var list = new List<string> { "Foo", "Bar", "Baz" };
        var host = Render<BindHost>(ps => ps.Add(p => p.Items, list));

        // Act: клікаємо по "Bar"
        var rows = host.FindAll("tbody tr");
        rows[1].Click();

        // Assert: host отримав Selected через SelectedItemChanged
        Assert.Equal("Bar", host.Instance.Selected);

        // Table має перерендеритись із SelectedItem="Bar" => підсвітка
        rows = host.FindAll("tbody tr");
        Assert.DoesNotContain("table-active", rows[0].GetAttribute("class") ?? string.Empty);
        Assert.Contains("table-active", rows[1].GetAttribute("class") ?? string.Empty);
        Assert.DoesNotContain("table-active", rows[2].GetAttribute("class") ?? string.Empty);
    }

    [Fact]
    public void KeySelector_ShouldControlSelection_WhenSelectionEnabled()
    {
        // Arrange
        var idA = Guid.NewGuid();
        var idB = Guid.NewGuid();

        var items = new List<TestRow>
        {
            new() { Id = idA, Name = "A" },
            new() { Id = idB, Name = "B" }
        };

        // SelectedItem інший instance, але Id співпадає
        var selected = new TestRow { Id = idB, Name = "B (external instance)" };

        var cut = Render<Table<TestRow>>(parameters => parameters
            .Add(p => p.Items, items)
            .Add(p => p.TableHeader, Header("Name"))
            .Add(p => p.RowTemplate, RowTemplateTestRow())
            .Add(p => p.SelectedItem, selected)
            // важливо: selection enabled => є SelectedItemChanged
            .Add(p => p.SelectedItemChanged, EventCallback.Factory.Create<TestRow?>(this, _ => { }))
            .Add(p => p.KeySelector, x => x.Id)
        );

        // Act
        var rows = cut.FindAll("tbody tr");

        // Assert: "B" підсвічується
        Assert.DoesNotContain("table-active", rows[0].GetAttribute("class") ?? string.Empty);
        Assert.Contains("table-active", rows[1].GetAttribute("class") ?? string.Empty);
    }
}
