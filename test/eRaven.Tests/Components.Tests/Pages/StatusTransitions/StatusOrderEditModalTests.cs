//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// StatusOrderEditModalTests
//-----------------------------------------------------------------------------

using Bunit;
using eRaven.Components.Pages.StatusTransitions.Modals;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace eRaven.Tests.Components.Tests.Pages.StatusTransitions;

public class StatusOrderEditModalTests : TestContext
{
    [Fact(DisplayName = "Open() => модал відображається, дані заповнені")]
    public async Task Open_ShowsModal_WithModel()
    {
        // Arrange
        var kindSvcMock = new Mock<IStatusKindService>(MockBehavior.Strict);
        Services.AddSingleton(kindSvcMock.Object);

        var cut = RenderComponent<StatusOrderEditModal>();

        // Act (через Dispatcher!)
        await cut.InvokeAsync(() => cut.Instance.Open(id: 10, name: "Статус A", currentOrder: 5));

        // Assert
        Assert.True(cut.Instance.IsOpen);
        var inputs = cut.FindAll("input.form-control.form-control-sm");
        Assert.Contains("Статус A", inputs[0].GetAttribute("value"));
        Assert.Equal("5", inputs[1].GetAttribute("value")); // CurrentOrder readonly
    }

    [Fact(DisplayName = "Close (кнопка) => модал ховається")]
    public async Task Close_HidesModal_ByButton()
    {
        // Arrange
        var kindSvcMock = new Mock<IStatusKindService>(MockBehavior.Strict);
        Services.AddSingleton(kindSvcMock.Object);

        var cut = RenderComponent<StatusOrderEditModal>();
        await cut.InvokeAsync(() => cut.Instance.Open(10, "X", 1));

        // Act: клікаємо по хрестику у хедері модала
        cut.Find("button.btn-close").Click();

        // Assert
        Assert.False(cut.Instance.IsOpen);
    }

    [Fact(DisplayName = "Submit без змін => Close, без виклику UpdateOrderAsync")]
    public async Task Submit_NoChanges_JustCloses_NoServiceCall()
    {
        // Arrange
        var kindSvcMock = new Mock<IStatusKindService>(MockBehavior.Strict);
        Services.AddSingleton(kindSvcMock.Object);

        var cut = RenderComponent<StatusOrderEditModal>();
        await cut.InvokeAsync(() => cut.Instance.Open(7, "B", 3)); // NewOrder == CurrentOrder

        // Act
        cut.Find("form").Submit();

        // Assert
        Assert.False(cut.Instance.IsOpen);
        kindSvcMock.Verify(x => x.UpdateOrderAsync(It.IsAny<int>(), It.IsAny<int>(), default), Times.Never);
    }

    [Fact(DisplayName = "Submit зі зміною => викликає UpdateOrderAsync та OnSaved")]
    public async Task Submit_WithChange_CallsService_And_OnSaved()
    {
        // Arrange
        var kindSvcMock = new Mock<IStatusKindService>(MockBehavior.Strict);
        kindSvcMock.Setup(x => x.UpdateOrderAsync(11, 9, default)).ReturnsAsync(true);
        Services.AddSingleton(kindSvcMock.Object);

        (int Id, int NewOrder)? savedPayload = null;

        var cut = RenderComponent<StatusOrderEditModal>(ps => ps
            .Add(p => p.OnSaved, EventCallback.Factory.Create<(int, int)>(this, (val) => savedPayload = val))
        );

        await cut.InvokeAsync(() => cut.Instance.Open(11, "C", 5));
        await cut.InvokeAsync(() => cut.Instance.Model.NewOrder = 9); // змінюємо модель у Dispatcher

        // Act
        cut.Find("form").Submit();

        // Assert
        kindSvcMock.Verify(x => x.UpdateOrderAsync(11, 9, default), Times.Once);
        Assert.Equal((11, 9), savedPayload);
        Assert.False(cut.Instance.IsOpen); // модал закрито
    }

    [Fact(DisplayName = "Кнопка 'Скасувати' закриває модал")]
    public async Task CancelButton_ClosesModal()
    {
        var kindSvcMock = new Mock<IStatusKindService>(MockBehavior.Strict);
        Services.AddSingleton(kindSvcMock.Object);

        var cut = RenderComponent<StatusOrderEditModal>();
        await cut.InvokeAsync(() => cut.Instance.Open(1, "Z", 2));

        cut.Find("button.btn.btn-warning.btn-sm").Click();

        Assert.False(cut.Instance.IsOpen);
    }

    [Fact(DisplayName = "Busy блокує повторні дії під час збереження")]
    public async Task Busy_BlocksDuringSave()
    {
        // Arrange
        var tcs = new TaskCompletionSource<bool>();
        var kindSvcMock = new Mock<IStatusKindService>(MockBehavior.Strict);
        kindSvcMock
            .Setup(x => x.UpdateOrderAsync(21, 22, default))
            .Returns(() => tcs.Task); // важливо: функція, а не готовий Task
        Services.AddSingleton(kindSvcMock.Object);

        var cut = RenderComponent<StatusOrderEditModal>();
        await cut.InvokeAsync(() => cut.Instance.Open(21, "D", 20));
        await cut.InvokeAsync(() => cut.Instance.Model.NewOrder = 22);

        // Act: submit => Busy=true
        cut.Find("form").Submit();
        Assert.True(cut.Instance.Busy);

        // Розрішуємо "бек"
        tcs.SetResult(true);
        cut.Render();

        // Assert
        Assert.False(cut.Instance.Busy);
        Assert.False(cut.Instance.IsOpen);
    }
}
