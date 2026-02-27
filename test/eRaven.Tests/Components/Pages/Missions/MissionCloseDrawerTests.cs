//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// MissionCloseDrawerTests
//-----------------------------------------------------------------------------

using Bunit;
using eRaven.Application.Commands;
using eRaven.Application.Commands.Missions;
using eRaven.Application.DTOs.Enums;
using eRaven.Application.DTOs.Missions;
using eRaven.Components.Pages.Missions.Drawers;
using eRaven.Presentation.Toasts;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace eRaven.Tests.Components.Pages.Missions;

public sealed class MissionCloseDrawerTests : BunitContext
{
    [Fact(DisplayName = "MissionCloseDrawer: вимагає DI (CloseMission + Toasts), без них рендер падає")]
    public void Requires_DI_Services()
    {
        // Arrange: НЕ реєструємо сервіси

        // Act + Assert
        Assert.Throws<InvalidOperationException>(() =>
            Render<MissionCloseDrawer>(ps => ps
                .Add(p => p.IsOpen, true)
                .Add(p => p.IsOpenChanged, EventCallback.Factory.Create<bool>(this, _ => { }))
            ));
    }

    [Fact(DisplayName = "MissionCloseDrawer: IsOpen=true + Mission=null — показує warning і не рендерить форму")]
    public void WhenOpen_WithoutMission_RendersWarning_AndNoForm()
    {
        // Arrange
        Services.AddSingleton(new Mock<ICommandHandler<CloseMissionCommand>>(MockBehavior.Loose).Object);
        Services.AddSingleton(new ToastService());

        // Act
        var cut = Render<MissionCloseDrawer>(ps => ps
            .Add(p => p.IsOpen, true)
            .Add(p => p.IsOpenChanged, EventCallback.Factory.Create<bool>(this, _ => { }))
            .Add(p => p.Mission, null)
        );

        // Assert: warning є
        cut.Markup.Contains("Місію не вибрано.");

        // Assert: форми нема (бо Mission=null)
        Assert.Throws<ElementNotFoundException>(() => cut.Find("form#mission-close-form"));

        // submit є у Footer, але disabled (бо Mission=null)
        var submit = cut.Find("button[type='submit']");
        Assert.True(submit.HasAttribute("disabled"));
    }

    [Fact(DisplayName = "MissionCloseDrawer: IsOpen=true + Mission задано — рендерить шапку та форму")]
    public void WhenOpen_WithMission_RendersHeader_AndForm()
    {
        // Arrange
        Services.AddSingleton(new Mock<ICommandHandler<CloseMissionCommand>>(MockBehavior.Loose).Object);
        Services.AddSingleton(new ToastService());

        var mission = new MissionDto(
            MissionId: Guid.NewGuid(),
            PositionArea: "Район-1",
            NamePoint: "Точка-А",
            Target: "Розвідка",
            MissionMode: MissionModeDto.Day,
            DroneName: "DJI",
            DisplayMisssion: "Район-1 Точка-А Розвідка DJI",
            CreatedAt: new DateOnly(2026, 01, 10),
            ClosedAt: null,
            IsOpen: true);

        // Act
        var cut = Render<MissionCloseDrawer>(ps => ps
            .Add(p => p.IsOpen, true)
            .Add(p => p.IsOpenChanged, EventCallback.Factory.Create<bool>(this, _ => { }))
            .Add(p => p.Mission, mission)
        );

        // Assert: є шапка з даними місії
        cut.Markup.Contains("Район-1");
        cut.Markup.Contains("Точка-А");
        cut.Markup.Contains("Розвідка");

        // Assert: форма існує
        cut.Find("form#mission-close-form");

        // submit має бути enabled (бо Mission != null і _busy=false)
        var submit = cut.Find("button[type='submit']");
        Assert.False(submit.HasAttribute("disabled"));
    }

    [Fact(DisplayName = "MissionCloseDrawer: submit викликає handler, OnClosed і IsOpenChanged(false)")]
    public async Task Submit_CallsHandler_InvokesCallbacks_AndCloses()
    {
        // Arrange
        CloseMissionCommand? captured = null;

        var handler = new Mock<ICommandHandler<CloseMissionCommand>>(MockBehavior.Strict);
        handler
            .Setup(h => h.HandleAsync(It.IsAny<CloseMissionCommand>(), It.IsAny<CancellationToken>()))
            .Callback<CloseMissionCommand, CancellationToken>((cmd, _) => captured = cmd)
            .Returns(Task.CompletedTask);

        Services.AddSingleton(handler.Object);
        Services.AddSingleton(new ToastService());

        bool? isOpenChanged = null;
        var closedCalled = 0;

        var mission = new MissionDto(
            MissionId: Guid.NewGuid(),
            PositionArea: "Район-1",
            NamePoint: "Точка-А",
            Target: "Розвідка",
            MissionMode: MissionModeDto.Day,
            DroneName: "DJI",
            DisplayMisssion: "Район-1 Точка-А Розвідка DJI",
            CreatedAt: new DateOnly(2026, 01, 01),
            ClosedAt: null,
            IsOpen: true);

        var cut = Render<MissionCloseDrawer>(ps => ps
            .Add(p => p.IsOpen, true)
            .Add(p => p.IsOpenChanged, EventCallback.Factory.Create<bool>(this, v => isOpenChanged = v))
            .Add(p => p.Mission, mission)
            .Add(p => p.OnClosed, EventCallback.Factory.Create(this, () => closedCalled++))
        );

        // виставляємо дату закриття
        cut.Find("input[type='date']").Change("2026-01-10");

        // Act: submit форми
        await cut.InvokeAsync(() => cut.Find("form#mission-close-form").Submit());

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.NotNull(captured);
            Assert.Equal(mission.MissionId, captured!.MissionId);
            Assert.Equal(new DateOnly(2026, 01, 10), captured.ClosedAt);

            Assert.Equal(1, closedCalled);
            Assert.Equal(false, isOpenChanged);
        });

        handler.Verify(
            h => h.HandleAsync(It.IsAny<CloseMissionCommand>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
