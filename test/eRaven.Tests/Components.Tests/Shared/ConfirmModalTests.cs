//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ConfirmModalTests
//-----------------------------------------------------------------------------

using Bunit;
using eRaven.Components.Shared.ConfirmModal;
using Microsoft.AspNetCore.Components;

namespace eRaven.Tests.Components.Tests.Shared;

public sealed class ConfirmModalTests : TestContext
{
    private static IRenderedComponent<ConfirmModal> RenderModal(
        TestContext ctx,
        string? title = "Підтвердження",
        string? confirm = "Підтвердити",
        string? cancel = "Скасувати",
        string? size = "modal-sm",
        RenderFragment<ConfirmModal>? footer = null)
    {
        return ctx.RenderComponent<ConfirmModal>(ps => ps
            .Add(p => p.Title, title!)
            .Add(p => p.ConfirmText, confirm!)
            .Add(p => p.CancelText, cancel!)
            .Add(p => p.DialogSize, size!)
            .Add(p => p.FooterTemplate, footer)
        );
    }

    // ---------- Confirm mode ----------

    [Fact]
    public async Task ShowConfirmAsync_Confirm_ReturnsTrue_And_Hides()
    {
        // Arrange
        var cut = RenderModal(this);

        // Act
        var task = cut.Instance.ShowConfirmAsync("Ти впевнений?");
        cut.WaitForAssertion(() => Assert.Contains("modal-backdrop", cut.Markup));

        cut.Find("button.btn.btn-sm.btn-primary").Click(); // Confirm

        var result = await task;

        // Assert
        Assert.True(result);
        cut.WaitForAssertion(() => Assert.DoesNotContain("modal-backdrop", cut.Markup));
    }

    [Fact]
    public async Task ShowConfirmAsync_Cancel_ReturnsFalse_And_Hides()
    {
        // Arrange
        var cut = RenderModal(this);

        // Act
        var task = cut.Instance.ShowConfirmAsync("Скасувати дію?");
        cut.WaitForAssertion(() => Assert.Contains("modal-backdrop", cut.Markup));

        cut.Find("button.btn.btn-sm.btn-warning").Click(); // Cancel

        var result = await task;

        // Assert
        Assert.False(result);
        cut.WaitForAssertion(() => Assert.DoesNotContain("modal-backdrop", cut.Markup));
    }

    // ---------- Form mode ----------

    private sealed class CreateVm
    {
        public string Code { get; set; } = "";
        public string ShortName { get; set; } = "";
    }

    [Fact]
    public async Task ShowFormAsync_Confirm_ReturnsModel()
    {
        // Arrange
        var cut = RenderModal(this);

        var model = new CreateVm { Code = "A1", ShortName = "New pos" };
        RenderFragment body(CreateVm m) => b =>
        {
            b.OpenElement(0, "div");
            b.AddContent(1, $"Код: {m.Code}, Назва: {m.ShortName}");
            b.CloseElement();
        };

        // Act
        var task = cut.Instance.ShowFormAsync(model, body);

        // (переконаємось, що тіло відрендерилось)
        cut.WaitForAssertion(() => Assert.Contains("Код: A1", cut.Markup));

        // підтвердити
        cut.Find("button.btn.btn-sm.btn-primary").Click();

        var result = await task;

        // Assert
        Assert.NotNull(result);
        Assert.Equal("A1", result!.Code);
        Assert.Equal("New pos", result.ShortName);
        cut.WaitForAssertion(() => Assert.DoesNotContain("modal-backdrop", cut.Markup));
    }

    [Fact]
    public async Task ShowFormAsync_Cancel_ReturnsNull()
    {
        // Arrange
        var cut = RenderModal(this);

        var model = new CreateVm { Code = "X1", ShortName = "Tmp" };
        RenderFragment body(CreateVm m) => b => b.AddContent(0, $"Модель: {m.Code}");

        // Act
        var task = cut.Instance.ShowFormAsync(model, body);
        cut.WaitForAssertion(() => Assert.Contains("Модель: X1", cut.Markup));

        cut.Find("button.btn.btn-sm.btn-warning").Click(); // Cancel
        var result = await task;

        // Assert
        Assert.Null(result);
        cut.WaitForAssertion(() => Assert.DoesNotContain("modal-backdrop", cut.Markup));
    }

    // ---------- Custom footer ----------

    [Fact]
    public async Task FooterTemplate_CanCall_Confirm()
    {
        // Arrange: футер, що сам викликає Confirm()
        RenderFragment footer(ConfirmModal m) => b =>
        {
            b.OpenElement(0, "button");
            b.AddAttribute(1, "class", "btn btn-success btn-sm");
            b.AddAttribute(2, "onclick", EventCallback.Factory.Create(this, m.Confirm));
            b.AddContent(3, "OK");
            b.CloseElement();
        };

        var cut = RenderModal(this, footer: footer);

        // Act
        var task = cut.Instance.ShowConfirmAsync("custom footer");
        cut.WaitForAssertion(() => Assert.Contains("custom footer", cut.Markup));

        cut.Find("button.btn.btn-success.btn-sm").Click(); // наш кастомний OK
        var result = await task;

        // Assert
        Assert.True(result);
        cut.WaitForAssertion(() => Assert.DoesNotContain("modal-backdrop", cut.Markup));
    }

    // ---------- Reentrancy guard ----------

    [Fact]
    public async Task Show_Throws_When_AlreadyOpen()
    {
        // Arrange
        var cut = RenderModal(this);

        // Act
        var _ = cut.Instance.ShowConfirmAsync("first");

        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => cut.Instance.ShowConfirmAsync("second"));
    }

    // ---------- Basic render ----------

    [Fact]
    public void Parameters_AreRendered_Correctly()
    {
        // Arrange
        using var ctx = new TestContext();
        var cut = ctx.RenderComponent<ConfirmModal>(ps => ps
            .Add(p => p.Title, "Мій заголовок")
            .Add(p => p.ConfirmText, "ОК")
            .Add(p => p.CancelText, "Відміна")
            .Add(p => p.DialogSize, "modal-xl")
        );

        // Act
        var _ = cut.Instance.ShowConfirmAsync("Тестовий текст");

        // Assert
        cut.WaitForAssertion(() =>
        {
            // Заголовок з параметра
            Assert.Contains("Мій заголовок", cut.Markup);
            // Текст кнопок з параметрів
            Assert.Contains(">ОК<", cut.Markup);
            Assert.Contains(">Відміна<", cut.Markup);
            // Клас діалогу з параметра
            Assert.Contains("modal-xl", cut.Markup);
            // Тіло з виклику ShowConfirmAsync
            Assert.Contains("Тестовий текст", cut.Markup);
        });
    }

    [Fact]
    public void Renders_Title_BodyText_And_Size()
    {
        // Arrange
        var cut = RenderModal(this, title: "Заголовок", size: "modal-lg");

        // Act
        var _ = cut.Instance.ShowConfirmAsync("Тіло");

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Заголовок", cut.Markup);
            Assert.Contains("Тіло", cut.Markup);
            Assert.Contains("modal-lg", cut.Markup);
            Assert.Contains("btn btn-sm btn-warning", cut.Markup); // Cancel
            Assert.Contains("btn btn-sm btn-primary", cut.Markup); // Confirm
        });
    }
}
