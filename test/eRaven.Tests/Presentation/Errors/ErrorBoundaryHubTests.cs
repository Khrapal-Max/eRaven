//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ErrorBoundaryHubTests -> ErrorBoundaryHub
//-----------------------------------------------------------------------------

using eRaven.Presentation.Errors;

namespace eRaven.Tests.Presentation.Errors;

public sealed class ErrorBoundaryHubTests
{
    [Fact]
    public void RequestRecover_ShouldInvokeRecoverRequestedEvent()
    {
        // Arrange
        var hub = new ErrorBoundaryHub();
        var wasCalled = false;

        hub.RecoverRequested += () => wasCalled = true;

        // Act
        hub.RequestRecover();

        // Assert
        Assert.True(wasCalled);
    }

    [Fact]
    public void RequestRecover_WhenNoSubscribers_ShouldNotThrow()
    {
        // Arrange
        var hub = new ErrorBoundaryHub();

        // Act
        var exception = Record.Exception(() => hub.RequestRecover());

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public void RequestRecover_ShouldInvokeAllSubscribers()
    {
        // Arrange
        var hub = new ErrorBoundaryHub();
        var callCount = 0;

        hub.RecoverRequested += () => callCount++;
        hub.RecoverRequested += () => callCount++;

        // Act
        hub.RequestRecover();

        // Assert
        Assert.Equal(2, callCount);
    }
}
