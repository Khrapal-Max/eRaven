//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PositionUnitPickerTests
//-----------------------------------------------------------------------------

using AngleSharp.Dom;
using Bunit;
using eRaven.Application.DTOs;
using eRaven.Components.Pages.Persons.Registry.Pickers;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System.Linq.Expressions;

namespace eRaven.Tests.Components.Pages.Persons.Pickers;

public sealed class PositionUnitPickerTests : BunitContext
{
    private static IReadOnlyList<PositionUnitOptionDto> Items()
        =>
        [
            new(
                Id: Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Code: "BBS-001",
                ShortName: "Командир відділення",
                FullName: "Командир 1 відділення 2 взводу 3 роти батальйону безпілотних систем (м. Київ)",
                Rank: "Сержант",
                Tarif: "10"
            ),
            new(
                Id: Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Code: "BBS-002",
                ShortName: "Оператор БПЛА",
                FullName: "Оператор ударних БПЛА 3 відділення 1 взводу 2 роти батальйону безпілотних систем (м. Львів)",
                Rank: "Солдат",
                Tarif: "8"
            ),
            new(
                Id: Guid.Parse("33333333-3333-3333-3333-333333333333"),
                Code: "AUX-777",
                ShortName: "Польова лазня",
                FullName: "Начальник польової лазні (м. Одеса)",
                Rank: "Старшина",
                Tarif: "9"
            ),
        ];

    private static IElement FindButtonByText(IRenderedComponent<PositionUnitPicker> cut, string contains)
        => cut.FindAll("button").Single(x => x.TextContent.Contains(contains, StringComparison.OrdinalIgnoreCase));

    [Fact]
    public void Render_when_no_selection_should_show_search_input_and_hint()
    {
        // arrange
        var cut = Render<PositionUnitPicker>(p => p
            .Add(x => x.Label, "Вакантна посада (опц.)")
            .Add(x => x.Items, Items())
            .Add(x => x.SelectedId, null)
        );

        // assert
        cut.Find("label").MarkupMatches(@"<label class=""form-label mb-1"">Вакантна посада (опц.)</label>");
        cut.Find("input.form-control"); // search exists
        Assert.Contains("Введіть мінімум 2 символи", cut.Markup);
    }

    [Fact]
    public void Search_with_less_than_2_chars_should_not_show_table()
    {
        var cut = Render<PositionUnitPicker>(p => p
            .Add(x => x.Items, Items())
        );

        var input = cut.Find("input.form-control");
        input.Input("B"); // 1 char

        Assert.Contains("Введіть мінімум 2 символи", cut.Markup);
        Assert.Empty(cut.FindAll("table"));
    }

    [Fact]
    public void Search_with_2_chars_should_filter_and_show_table_rows()
    {
        var cut = Render<PositionUnitPicker>(p => p
            .Add(x => x.Items, Items())
        );

        var input = cut.Find("input.form-control");
        input.Input("BBS"); // matches first 2

        var rows = cut.FindAll("tbody tr");
        Assert.Equal(2, rows.Count);
        Assert.Contains("BBS-001", rows[0].TextContent);
        Assert.Contains("BBS-002", rows[1].TextContent);
    }

    [Fact]
    public void Select_row_should_show_selected_card_hide_search_and_invoke_callbacks()
    {
        // arrange
        Guid? selectedId = null;
        PositionUnitOptionDto? selectedDto = null;
        var receiver = new object();

        var cut = Render<PositionUnitPicker>(p => p
            .Add(x => x.Items, Items())
            .Add(x => x.SelectedId, null)
            .Add(x => x.SelectedIdChanged, EventCallback.Factory.Create<Guid?>(receiver, v => selectedId = v))
            .Add(x => x.OnSelected, EventCallback.Factory.Create<PositionUnitOptionDto?>(receiver, v => selectedDto = v))
        );

        // act: search -> click first row
        cut.Find("input.form-control").Input("BBS");
        cut.Find("tbody tr").Click();

        // assert: selected view
        Assert.Contains("Вибрано:", cut.Markup);
        Assert.Contains("BBS-001", cut.Markup);
        Assert.Contains("Командир 1 відділення", cut.Markup); // full name preview exists
        Assert.Empty(cut.FindAll("input.form-control")); // search hidden

        // callbacks
        Assert.Equal(Guid.Parse("11111111-1111-1111-1111-111111111111"), selectedId);
        Assert.NotNull(selectedDto);
        Assert.Equal("BBS-001", selectedDto!.Code);
    }

    [Fact]
    public void ShowSearch_should_hide_selected_card_and_show_search_again_without_clearing_selectedId()
    {
        // arrange
        var items = Items();
        var selected = items[0];

        var cut = Render<PositionUnitPicker>(p => p
            .Add(x => x.Items, items)
            .Add(x => x.SelectedId, selected.Id)
        );

        Assert.Contains("Вибрано:", cut.Markup);

        // act
        FindButtonByText(cut, "Змінити").Click();

        // assert
        cut.Find("input.form-control"); // search exists again
        Assert.DoesNotContain("Вибрано:", cut.Markup);
    }

    [Fact]
    public void Clear_should_remove_selection_and_invoke_callbacks_with_null()
    {
        // arrange
        Guid? selectedId = Guid.NewGuid(); // non-null start
        PositionUnitOptionDto? selectedDto = new(
            Id: Guid.NewGuid(),
            Code: "X", ShortName: "X", FullName: "X", Rank: "X", Tarif: "X");

        var receiver = new object();
        var items = Items();
        var selected = items[1];

        var cut = Render<PositionUnitPicker>(p => p
            .Add(x => x.Items, items)
            .Add(x => x.SelectedId, selected.Id)
            .Add(x => x.SelectedIdChanged, EventCallback.Factory.Create<Guid?>(receiver, v => selectedId = v))
            .Add(x => x.OnSelected, EventCallback.Factory.Create<PositionUnitOptionDto?>(receiver, v => selectedDto = v))
        );

        Assert.Contains("Вибрано:", cut.Markup);

        // act
        FindButtonByText(cut, "Очистити").Click();

        // assert
        cut.Find("input.form-control"); // back to search mode
        Assert.DoesNotContain("Вибрано:", cut.Markup);

        Assert.Null(selectedId);
        Assert.Null(selectedDto);
    }

    [Fact]
    public void Render_with_SelectedId_should_show_selected_card_immediately()
    {
        var items = Items();

        var cut = Render<PositionUnitPicker>(p => p
            .Add(x => x.Items, items)
            .Add(x => x.SelectedId, items[2].Id)
        );

        Assert.Contains("Вибрано:", cut.Markup);
        Assert.Contains("AUX-777", cut.Markup);
        Assert.Contains("(м. Одеса)", cut.Markup);
        Assert.Empty(cut.FindAll("input.form-control"));
    }

    [Fact]
    public void Render_with_For_inside_EditForm_should_not_throw()
    {
        var items = Items();
        var model = new TestModel();
        var editContext = new EditContext(model);

        var cut = Render<EditForm>(p => p
            .Add(x => x.EditContext, editContext)
            .Add(x => x.ChildContent, _ => builder =>
            {
                builder.OpenComponent<PositionUnitPicker>(0);
                builder.AddAttribute(1, "Items", items);
                builder.AddAttribute(2, "SelectedId", model.PlannedPositionUnitId);
                builder.AddAttribute(3, "SelectedIdChanged",
                    EventCallback.Factory.Create<Guid?>(this, v => model.PlannedPositionUnitId = v));
                builder.AddAttribute(4, "For",
                    (Expression<Func<object>>)(() => model.PlannedPositionUnitId!));
                builder.CloseComponent();
            })
        );

        Assert.NotNull(cut);
    }

    private sealed class TestModel
    {
        public Guid? PlannedPositionUnitId { get; set; }
    }
}
