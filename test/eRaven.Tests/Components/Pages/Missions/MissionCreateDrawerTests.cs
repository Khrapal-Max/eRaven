//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// MissionCreateDrawerTests
//-----------------------------------------------------------------------------

using Bunit;
using eRaven.Application.Catalogs.CombatTasks.Targets;
using eRaven.Application.Catalogs.CombatTasks.TypeDrones;
using eRaven.Application.Commands;
using eRaven.Application.Commands.Missions;
using eRaven.Components.Pages.Missions.Drawers;
using eRaven.Domain.Enums;
using eRaven.Presentation.Toasts;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace eRaven.Tests.Components.Pages.Missions;

public sealed class MissionCreateDrawerTests : BunitContext
{
    [Fact(DisplayName = "MissionCreateDrawer: вимагає DI (CreateMissionHandler + каталоги + Toasts), без них рендер падає")]
    public void Requires_DI_Services()
    {
        // Arrange: навмисно не реєструємо сервіси

        // Act + Assert
        Assert.Throws<InvalidOperationException>(() =>
            Render<MissionCreateDrawer>(ps => ps
                .Add(p => p.IsOpen, true)
                .Add(p => p.IsOpenChanged, EventCallback.Factory.Create<bool>(this, _ => { }))
            ));
    }

    [Fact(DisplayName = "MissionCreateDrawer: коли IsOpen=false — форма не рендериться, submit існує але disabled")]
    public void WhenClosed_DoesNotRenderForm_SubmitDisabled()
    {
        // Arrange
        Services.AddSingleton(new Mock<ICommandHandler<CreateMissionCommand, Guid>>(MockBehavior.Loose).Object);

        var typeCatalog = new Mock<ITypeDroneCatalog>(MockBehavior.Loose);
        typeCatalog.Setup(x => x.GetActive()).Returns([new("DJI")]);
        Services.AddSingleton(typeCatalog.Object);

        var targetCatalog = new Mock<ITargetCatalog>(MockBehavior.Loose);
        targetCatalog.Setup(x => x.GetActive()).Returns([new("Розвідка")]);
        Services.AddSingleton(targetCatalog.Object);

        Services.AddSingleton(new ToastService());

        // Act
        var cut = Render<MissionCreateDrawer>(ps => ps
            .Add(p => p.IsOpen, false)
            .Add(p => p.IsOpenChanged, EventCallback.Factory.Create<bool>(this, _ => { }))
        );

        // Assert: форми нема
        Assert.Throws<ElementNotFoundException>(() => cut.Find("form#mission-create-form"));

        // Assert: submit є, але disabled (бо PositionArea/Target пусті)
        var submit = cut.Find("button[type='submit']");
        Assert.True(submit.HasAttribute("disabled"));
    }

    [Fact(DisplayName = "MissionCreateDrawer: IsOpen=true — рендерить форму та підтягує опції з каталогів")]
    public void WhenOpen_RendersForm_AndCatalogOptions()
    {
        // Arrange
        Services.AddSingleton(new Mock<ICommandHandler<CreateMissionCommand, Guid>>(MockBehavior.Loose).Object);

        var typeCatalog = new Mock<ITypeDroneCatalog>(MockBehavior.Strict);
        typeCatalog.Setup(x => x.GetActive()).Returns(
        [
            new("DJI"),
            new("Mavic")
        ]);
        Services.AddSingleton(typeCatalog.Object);

        var targetCatalog = new Mock<ITargetCatalog>(MockBehavior.Strict);
        targetCatalog.Setup(x => x.GetActive()).Returns(
        [
            new("Розвідка"),
            new("Охорона")
        ]);
        Services.AddSingleton(targetCatalog.Object);

        Services.AddSingleton(new ToastService());

        // Act
        var cut = Render<MissionCreateDrawer>(ps => ps
            .Add(p => p.IsOpen, true)
            .Add(p => p.IsOpenChanged, EventCallback.Factory.Create<bool>(this, _ => { }))
        );

        // Assert: форма є
        cut.Find("form#mission-create-form");

        // Assert: є 3 select: Target, Mode, Drone
        var selects = cut.FindAll("select");
        Assert.Equal(3, selects.Count);

        // target options
        Assert.Contains("Розвідка", selects[0].InnerHtml);
        Assert.Contains("Охорона", selects[0].InnerHtml);

        // drone options
        Assert.Contains("DJI", selects[2].InnerHtml);
        Assert.Contains("Mavic", selects[2].InnerHtml);
    }

    [Fact(DisplayName = "MissionCreateDrawer: submit викликає handler (trim), OnCreated і IsOpenChanged(false)")]
    public async Task Submit_CallsHandler_InvokesCallbacks_AndCloses()
    {
        // Arrange
        CreateMissionCommand? captured = null;

        var handler = new Mock<ICommandHandler<CreateMissionCommand, Guid>>(MockBehavior.Strict);
        var returnedId = Guid.NewGuid();

        handler
            .Setup(h => h.HandleAsync(It.IsAny<CreateMissionCommand>(), It.IsAny<CancellationToken>()))
            .Callback<CreateMissionCommand, CancellationToken>((cmd, _) => captured = cmd)
            .ReturnsAsync(returnedId);

        Services.AddSingleton(handler.Object);

        var typeCatalog = new Mock<ITypeDroneCatalog>(MockBehavior.Loose);
        typeCatalog.Setup(x => x.GetActive()).Returns([new("DJI")]);
        Services.AddSingleton(typeCatalog.Object);

        var targetCatalog = new Mock<ITargetCatalog>(MockBehavior.Loose);
        targetCatalog.Setup(x => x.GetActive()).Returns([new("Розвідка")]);
        Services.AddSingleton(targetCatalog.Object);

        Services.AddSingleton(new ToastService());

        Guid createdId = Guid.Empty;
        bool? isOpenChanged = null;

        var cut = Render<MissionCreateDrawer>(ps => ps
            .Add(p => p.IsOpen, true)
            .Add(p => p.IsOpenChanged, EventCallback.Factory.Create<bool>(this, v => isOpenChanged = v))
            .Add(p => p.OnCreated, EventCallback.Factory.Create<Guid>(this, id => createdId = id))
        );

        // Inputs: 2x InputText => input.form-control.rounded-0
        var inputs = cut.FindAll("input.form-control.rounded-0");
        Assert.True(inputs.Count >= 2); // тепер селектор стабільний

        cut.Find("input[name='Model.PositionArea']").Change("  Район-1  ");// PositionArea
        cut.Find("input[name='Model.NamePoint']").Change("  Точка-А  ");   // NamePoint

        // Selects: Target / Mode / Drone (в DOM порядку)
        var selects = cut.FindAll("select.form-select.rounded-0");
        Assert.True(selects.Count >= 3);

        cut.Find("select[name='Model.Target']").Change("Розвідка"); // Target (Required)
        cut.Find("select[name='Model.DroneName']").Change("DJI");   // DroneName (optional)

        // Act: submit EditForm (важливо: OnValidSubmit => Submit, а не click по кнопці)
        await cut.InvokeAsync(() => cut.Find("form#mission-create-form").Submit());

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.NotNull(captured);

            Assert.Equal("Район-1", captured!.PositionArea);
            Assert.Equal("Точка-А", captured.NamePoint);
            Assert.Equal("Розвідка", captured.Target);
            Assert.Equal("DJI", captured.DroneName);

            // MissionMode лишився дефолтний Day (бо ми його не чіпали)
            Assert.Equal(MissionMode.Day, captured.MissionMode);

            Assert.Equal(returnedId, createdId);
            Assert.Equal(false, isOpenChanged);
        });

        handler.Verify(h => h.HandleAsync(It.IsAny<CreateMissionCommand>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}