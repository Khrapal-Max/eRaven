//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// Tests: TransitionToggle<TId>
//-----------------------------------------------------------------------------

using Bunit;
using eRaven.Components.Shared.TransitionToggle;
using Microsoft.AspNetCore.Components;
using Moq;

namespace eRaven.Tests.Components.Tests.Shared;

public sealed class TransitionToggleTests : TestContext
{
    // ---------------------------------------------------------------------
    // [Helper] Рендеримо компонент з необхідними параметрами
    // ---------------------------------------------------------------------
    private IRenderedComponent<TransitionToggle<int>> RenderIntToggle(
        bool initialChecked,
        bool disabled = false,
        Func<int, bool, Task<bool>>? confirm = null,
        Func<int, bool, Task>? save = null,
        Action<bool>? onCheckedChanged = null)
    {
        return RenderComponent<TransitionToggle<int>>(ps => ps
            .Add(p => p.FromId, 1)
            .Add(p => p.ToId, 2)
            .Add(p => p.ToName, "Test To")
            .Add(p => p.Checked, initialChecked)
            .Add(p => p.Disabled, disabled)
            .Add(p => p.ConfirmAsync, confirm)
            .Add(p => p.SaveAsync, save)
            .Add(p => p.CheckedChanged, EventCallback.Factory.Create<bool>(this, onCheckedChanged ?? (_ => { })))
        );
    }

    // ---------------------------------------------------------------------
    // [Positive] Confirm=true -> Save викликається -> CheckedChanged викликається
    // ---------------------------------------------------------------------
    [Fact(DisplayName = "Click: confirmed -> save called -> CheckedChanged(true) fired")]
    public void Click_Confirmed_SaveCalled_CheckedChangedFired()
    {
        // Arrange
        var saveCalled = false;
        bool? checkedChangedValue = null;

        var confirmMock = new Mock<Func<int, bool, Task<bool>>>();
        confirmMock.Setup(f => f(It.IsAny<int>(), It.IsAny<bool>())).ReturnsAsync(true);

        var saveMock = new Mock<Func<int, bool, Task>>();
        saveMock.Setup(f => f(It.IsAny<int>(), It.IsAny<bool>()))
                .Returns<int, bool>((_, __) => { saveCalled = true; return Task.CompletedTask; });

        var cut = RenderIntToggle(
            initialChecked: false,                   // стане true
            confirm: confirmMock.Object,
            save: saveMock.Object,
            onCheckedChanged: v => checkedChangedValue = v
        );

        // Act
        cut.Find("input").Click();

        // Assert
        Assert.True(saveCalled);
        Assert.True(checkedChangedValue == true);  // інверсія false -> true
        confirmMock.Verify(f => f(2, true), Times.Once);   // ToId=2, turnOn=true
        saveMock.Verify(f => f(2, true), Times.Once);
    }

    // ---------------------------------------------------------------------
    // [Negative] Confirm=false -> Save НЕ викликається -> CheckedChanged НЕ викликається
    // ---------------------------------------------------------------------
    [Fact(DisplayName = "Click: confirmation canceled -> no save -> no CheckedChanged")]
    public void Click_Canceled_NoSave_NoCheckedChanged()
    {
        // Arrange
        var saveCalled = false;
        var checkedChangedFired = false;

        var confirmMock = new Mock<Func<int, bool, Task<bool>>>();
        confirmMock.Setup(f => f(It.IsAny<int>(), It.IsAny<bool>())).ReturnsAsync(false);

        var saveMock = new Mock<Func<int, bool, Task>>();
        saveMock.Setup(f => f(It.IsAny<int>(), It.IsAny<bool>()))
                .Returns<int, bool>((_, __) => { saveCalled = true; return Task.CompletedTask; });

        var cut = RenderIntToggle(
            initialChecked: true,                    // клік на вимкнення -> turnOn=false
            confirm: confirmMock.Object,
            save: saveMock.Object,
            onCheckedChanged: _ => checkedChangedFired = true
        );

        // Act
        cut.Find("input").Click();

        // Assert
        Assert.False(saveCalled);
        Assert.False(checkedChangedFired);
        confirmMock.Verify(f => f(2, false), Times.Once);  // turnOn=false
        saveMock.Verify(f => f(It.IsAny<int>(), It.IsAny<bool>()), Times.Never);
    }

    // ---------------------------------------------------------------------
    // [Error] Save кидає виняток -> CheckedChanged НЕ викликається -> виняток «протікає» (стійка перевірка)
    // ---------------------------------------------------------------------
    [Fact(DisplayName = "Click: save throws -> CheckedChanged not fired -> exception bubbles (robust)")]
    public void Click_SaveThrows_CheckedChangedNotFired_ExceptionBubbles_Robust()
    {
        // Arrange
        var checkedChangedFired = false;

        var confirmMock = new Mock<Func<int, bool, Task<bool>>>();
        confirmMock.Setup(f => f(It.IsAny<int>(), It.IsAny<bool>())).ReturnsAsync(true);

        var saveMock = new Mock<Func<int, bool, Task>>();
        saveMock.Setup(f => f(It.IsAny<int>(), It.IsAny<bool>()))
                .ThrowsAsync(new InvalidOperationException("save failed"));

        var cut = RenderIntToggle(
            initialChecked: false,
            confirm: confirmMock.Object,
            save: saveMock.Object,
            onCheckedChanged: _ => checkedChangedFired = true
        );

        // Act + Assert
        // BUnit може кинути AggregateException або без InnerException.
        var ex = Record.Exception(() => cut.Find("input").Click());
        Assert.NotNull(ex);

        // Акуратно розпаковуємо
        Exception effective = ex is AggregateException agg && agg.InnerExceptions.Count > 0
            ? agg.Flatten().InnerExceptions[0]
            : ex;

        // Перевіряємо тип і повідомлення
        Assert.IsType<InvalidOperationException>(effective);
        Assert.Contains("save failed", effective.Message, StringComparison.OrdinalIgnoreCase);

        // Переконуємось, що зміна стану не відбулась
        Assert.False(checkedChangedFired);

        confirmMock.Verify(f => f(2, true), Times.Once);
        saveMock.Verify(f => f(2, true), Times.Once);
    }

    // ---------------------------------------------------------------------
    // [Guard] Disabled=true -> нічого не викликається
    // ---------------------------------------------------------------------
    [Fact(DisplayName = "Click: disabled -> no confirm, no save, no CheckedChanged")]
    public void Click_Disabled_Noops()
    {
        // Arrange
        var confirmCalled = false;
        var saveCalled = false;
        var checkedChangedFired = false;

        var cut = RenderIntToggle(
            initialChecked: false,
            disabled: true,
            confirm: (id, on) => { confirmCalled = true; return Task.FromResult(true); },
            save: (id, on) => { saveCalled = true; return Task.CompletedTask; },
            onCheckedChanged: _ => checkedChangedFired = true
        );

        // Act
        cut.Find("input").Click();

        // Assert
        Assert.False(confirmCalled);
        Assert.False(saveCalled);
        Assert.False(checkedChangedFired);
    }

    // ---------------------------------------------------------------------
    // [NoConfirm] Без ConfirmAsync: одразу Save + CheckedChanged
    // ---------------------------------------------------------------------
    [Fact(DisplayName = "Click: no confirm delegate -> save called -> CheckedChanged fired")]
    public void Click_NoConfirm_SaveCalled_CheckedChangedFired()
    {
        // Arrange
        var saveCalled = false;
        bool? changedValue = null;

        var saveMock = new Mock<Func<int, bool, Task>>();
        saveMock.Setup(f => f(It.IsAny<int>(), It.IsAny<bool>()))
                .Returns<int, bool>((_, __) => { saveCalled = true; return Task.CompletedTask; });

        var cut = RenderIntToggle(
            initialChecked: true,                    // стане false
            confirm: null,
            save: saveMock.Object,
            onCheckedChanged: v => changedValue = v
        );

        // Act
        cut.Find("input").Click();

        // Assert
        Assert.True(saveCalled);
        Assert.Equal(false, changedValue);
        saveMock.Verify(f => f(2, false), Times.Once);
    }

    // ---------------------------------------------------------------------
    // [Generic] Перевірка компіляції та базової взаємодії для Guid
    // ---------------------------------------------------------------------
    [Fact(DisplayName = "Generic Guid: renders and toggles with confirm/save")]
    public void GenericGuid_RendersAndToggles()
    {
        // Arrange
        var from = Guid.NewGuid();
        var to = Guid.NewGuid();
        var changed = false;

        using var ctx = new TestContext();
        var confirm = new Func<Guid, bool, Task<bool>>((_, __) => Task.FromResult(true));
        var save = new Func<Guid, bool, Task>((_, __) => Task.CompletedTask);

        var cut = ctx.RenderComponent<TransitionToggle<Guid>>(ps => ps
            .Add(p => p.FromId, from)
            .Add(p => p.ToId, to)
            .Add(p => p.ToName, "G-To")
            .Add(p => p.Checked, false)
            .Add(p => p.Disabled, false)
            .Add(p => p.ConfirmAsync, confirm)
            .Add(p => p.SaveAsync, save)
            .Add(p => p.CheckedChanged, EventCallback.Factory.Create<bool>(this, (bool v) => changed = v))
        );

        // Act
        cut.Find("input").Click();

        // Assert
        Assert.True(changed); // false -> true
    }
}