//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CloseTimesheetCodeDrawerTests
//-----------------------------------------------------------------------------

using Bunit;
using eRaven.Application.Commands;
using eRaven.Application.Commands.Timesheets;
using eRaven.Application.DTOs.Timesheets.Policy;
using eRaven.Components.Pages.Timesheets.Drawers;
using eRaven.Domain.Enums;
using eRaven.Presentation.Toasts;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace eRaven.Tests.Components.Pages.Timesheets.Drawers;

public sealed class CloseTimesheetCodeDrawerTests : BunitContext
{
    private readonly ToastService _toastMock = new();
    private readonly Mock<ICommandHandler<CloseTimesheetCodeCommand>> _handlerMock = new(MockBehavior.Strict);

    public CloseTimesheetCodeDrawerTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        Services.AddSingleton(_toastMock);
        Services.AddSingleton(_handlerMock.Object);
    }

    [Fact]
    public void Render_ShowsRequiredControls_AndSelectedCode()
    {
        var code = new TimesheetCodeDto(Guid.NewGuid(), "Ф100", "Форс-мажор", null, 30, 10, true, true, RoleCode.EmergencyCode, TimesheetUiStyle.Danger);

        var cut = Render<CloseTimesheetCodeDrawer>(ps => ps
            .Add(p => p.IsOpen, true)
            .Add(p => p.Code, code));

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Закрити табельний код", cut.Markup);
            Assert.Contains("Ф100", cut.Markup);
            Assert.Contains("Форс-мажор", cut.Markup);

            Assert.Contains("Скасувати", cut.Markup);
            Assert.Contains("Закрити код", cut.Markup);
        });
    }

    [Fact]
    public void InjectedServices_AndCallbacks_AreUsed_OnConfirmClose()
    {
        _handlerMock
            .Setup(x => x.HandleAsync(It.IsAny<CloseTimesheetCodeCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var code = new TimesheetCodeDto(Guid.NewGuid(), "30", "Готовність", null, 10, 1, false, true, RoleCode.TransitionCode, TimesheetUiStyle.Ready);

        Guid? closedId = null;
        var receiver = new object();
        var onClosed = EventCallback.Factory.Create<Guid>(receiver, (Action<Guid>)(id => closedId = id));

        var cut = Render<CloseTimesheetCodeDrawer>(ps => ps
            .Add(p => p.IsOpen, true)
            .Add(p => p.Code, code)
            .Add(p => p.OnClosedCode, onClosed));

        var closeBtn = cut.FindAll("button")
            .First(bn => bn.TextContent.Contains("Закрити код", StringComparison.OrdinalIgnoreCase));

        closeBtn.Click();

        cut.WaitForAssertion(() =>
        {
            _handlerMock.Verify(x => x.HandleAsync(
                It.Is<CloseTimesheetCodeCommand>(cmd => cmd.CodeId == code.Id),
                It.IsAny<CancellationToken>()
            ), Times.Once);

            Assert.Equal(code.Id, closedId);
        });
    }
}
