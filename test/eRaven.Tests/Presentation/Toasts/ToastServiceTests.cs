//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ToastServiceTests -> ToastService
//-----------------------------------------------------------------------------

using eRaven.Presentation.Toasts;

namespace eRaven.Tests.Presentation.Toasts;

public sealed class ToastServiceTests
{
    [Fact]
    public void Show_ShouldRaiseOnShowEvent_AndPassSameMessage()
    {
        // Arrange
        var service = new ToastService();

        ToastMessage? received = null;
        service.OnShow += msg => received = msg;

        var message = new ToastMessage(
            Kind: ToastKind.Info,
            Title: "Hello",
            Body: "World",
            AutoHideMs: 1234);

        // Act
        service.Show(message);

        // Assert
        Assert.NotNull(received);
        Assert.Same(message, received);
        Assert.Equal(ToastKind.Info, received!.Kind);
        Assert.Equal("Hello", received.Title);
        Assert.Equal("World", received.Body);
        Assert.Equal(1234, received.AutoHideMs);
    }

    [Fact]
    public void Show_WhenNoSubscribers_ShouldNotThrow()
    {
        // Arrange
        var service = new ToastService();
        var message = new ToastMessage(ToastKind.Info, "T", "B", 1);

        // Act
        var ex = Record.Exception(() => service.Show(message));

        // Assert
        Assert.Null(ex);
    }

    [Fact]
    public void Show_ShouldNotifyAllSubscribers()
    {
        // Arrange
        var service = new ToastService();
        var calls = 0;

        service.OnShow += _ => calls++;
        service.OnShow += _ => calls++;

        // Act
        service.Show(new ToastMessage(ToastKind.Info, "t"));

        // Assert
        Assert.Equal(2, calls);
    }

    [Fact]
    public void Info_ShouldRaiseOnShow_WithInfoKindAndGivenTitleAndBody()
    {
        // Arrange
        var service = new ToastService();
        ToastMessage? received = null;

        service.OnShow += msg => received = msg;

        // Act
        service.Info("Title", "Body");

        // Assert
        Assert.NotNull(received);
        Assert.Equal(ToastKind.Info, received!.Kind);
        Assert.Equal("Title", received.Title);
        Assert.Equal("Body", received.Body);
        Assert.True(received.AutoHideMs > 0);
    }

    [Fact]
    public void Success_ShouldRaiseOnShow_WithSuccessKind()
    {
        // Arrange
        var service = new ToastService();
        ToastMessage? received = null;

        service.OnShow += msg => received = msg;

        // Act
        service.Success("OK", "Done");

        // Assert
        Assert.NotNull(received);
        Assert.Equal(ToastKind.Success, received!.Kind);
        Assert.Equal("OK", received.Title);
        Assert.Equal("Done", received.Body);
    }

    [Fact]
    public void Warning_ShouldRaiseOnShow_WithWarningKind()
    {
        // Arrange
        var service = new ToastService();
        ToastMessage? received = null;

        service.OnShow += msg => received = msg;

        // Act
        service.Warning("Warn", "Be careful");

        // Assert
        Assert.NotNull(received);
        Assert.Equal(ToastKind.Warning, received!.Kind);
        Assert.Equal("Warn", received.Title);
        Assert.Equal("Be careful", received.Body);
    }

    [Fact]
    public void Error_ShouldRaiseOnShow_WithErrorKind_AndLongerDefaultAutoHide()
    {
        // Arrange
        var service = new ToastService();
        ToastMessage? received = null;

        service.OnShow += msg => received = msg;

        // Act
        service.Error("Err", "Boom");

        // Assert
        Assert.NotNull(received);
        Assert.Equal(ToastKind.Error, received!.Kind);
        Assert.Equal("Err", received.Title);
        Assert.Equal("Boom", received.Body);

        // Якщо в сервісі Error() виставляє довше автоприховування (наприклад 7000)
        Assert.True(received.AutoHideMs >= 4000);
    }
}
