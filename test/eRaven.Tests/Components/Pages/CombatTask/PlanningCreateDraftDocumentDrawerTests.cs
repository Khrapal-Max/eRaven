//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PlanningCreateDraftDocumentDrawerTests
//-----------------------------------------------------------------------------

using Bunit;
using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using eRaven.Application.Commands;
using eRaven.Application.Commands.CombatTask;
using eRaven.Components.Pages.CombatTask.Drawers;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace eRaven.Tests.Components.Pages.CombatTask;

public sealed class PlanningCreateDraftDocumentDrawerTests : BunitContext
{
    [Fact(DisplayName = "Drawer: коли IsOpen=false — форма не рендериться")]
    public void WhenClosed_DoesNotRenderForm()
    {
        // Arrange
        var handler = new Mock<ICommandHandler<CreateCombatTaskPlanDraftDocumentCommand, Guid>>(MockBehavior.Loose);
        Services.AddSingleton(handler.Object);

        // Act
        var cut = Render<PlanningCreateDraftDocumentDrawer>(ps => ps
            .Add(p => p.IsOpen, false)
            .Add(p => p.IsOpenChanged, EventCallback.Factory.Create<bool>(this, _ => { }))
        );

        // Assert
        Assert.Throws<ElementNotFoundException>(() => cut.Find("form#planning-draft-form"));
        // Assert: submit є, але disabled (бо модель пуста)
        var submit = cut.Find("button[type='submit']");
        Assert.True(submit.HasAttribute("disabled"));
    }

    [Fact(DisplayName = "Drawer: submit викликає handler, OnCreated і IsOpenChanged(false)")]
    public async Task Submit_CallsHandler_InvokesCallbacks_AndCloses()
    {
        // Arrange
        CreateCombatTaskPlanDraftDocumentCommand? captured = null;

        var handler = new Mock<ICommandHandler<CreateCombatTaskPlanDraftDocumentCommand, Guid>>(MockBehavior.Strict);
        handler
            .Setup(h => h.HandleAsync(It.IsAny<CreateCombatTaskPlanDraftDocumentCommand>(), It.IsAny<CancellationToken>()))
            .Callback<CreateCombatTaskPlanDraftDocumentCommand, CancellationToken>((cmd, _) => captured = cmd)
            .ReturnsAsync(Guid.NewGuid());

        Services.AddSingleton(handler.Object);

        Guid createdId = Guid.Empty;
        bool? closedValue = null;

        var cut = Render<PlanningCreateDraftDocumentDrawer>(ps => ps
            .Add(p => p.IsOpen, true)
            .Add(p => p.IsOpenChanged, EventCallback.Factory.Create<bool>(this, v => closedValue = v))
            .Add(p => p.OnCreated, EventCallback.Factory.Create<Guid>(this, id => createdId = id))
        );

        // заповнюємо назву (перевіряємо trim)
        cut.Find("input[placeholder='Напр: План №12']").Change("  План №12  ");

        // Act: submit форми (а не click по кнопці)
        await cut.InvokeAsync(() => cut.Find("form#planning-draft-form").Submit());

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.NotNull(captured);

            Assert.Equal("План №12", captured!.PlanningDocTitle);
            Assert.NotEqual(Guid.Empty, captured.DocumentId);

            // OnCreated повертає той самий DocumentId
            Assert.Equal(captured.DocumentId, createdId);

            // drawer “просить” закритися
            Assert.Equal(false, closedValue);
        });

        handler.Verify(
            h => h.HandleAsync(It.IsAny<CreateCombatTaskPlanDraftDocumentCommand>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}