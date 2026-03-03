//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CreateDocumentDrawerTests
//-----------------------------------------------------------------------------

using AngleSharp.Dom;
using Bunit;
using eRaven.Application.Commands;
using eRaven.Application.Commands.CombatTasks;
using eRaven.Components.Pages.CombatTasks.Drawers;
using eRaven.Presentation.Toasts;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace eRaven.Tests.Components.Pages.CombatTasks;

public sealed class CreateDocumentDrawerTests : BunitContext
{
    private readonly Mock<ICommandHandler<CreateCombatTaskDocumentCommand, Guid>> _handler;

    public CreateDocumentDrawerTests()
    {
        // Drawer focuses itself on open; let JS calls pass without explicit setups.
        JSInterop.Mode = JSRuntimeMode.Loose;

        _handler = new(MockBehavior.Strict);
        // Arrange
        Services.AddSingleton(new ToastService());
        Services.AddSingleton(_handler.Object);
    }

    [Fact]
    public void Render_Closed_DoesNotRenderFormBody()
    {
        // Act
        var cut = Render<CreateDocumentDrawer>(ps =>
        {
            ps.Add(p => p.IsOpen, false);
            ps.Add(p => p.IsOpenChanged, EventCallback.Factory.Create<bool>(this, _ => { }));
        });

        // Assert
        Assert.Contains("Документ бойових завдань", cut.Markup);
        Assert.DoesNotContain("Назва/номер документа", cut.Markup); // body is behind `if (IsOpen)`
        Assert.Empty(cut.FindAll("form"));

        Assert.NotNull(cut.Instance.CreateCombatTaskDocumentCommandHandler);
        Assert.NotNull(cut.Instance.ToastService);
    }

    [Fact]
    public void Render_Open_RendersFormAndActions()
    {
        // Act
        var cut = Render<CreateDocumentDrawer>(ps =>
        {
            ps.Add(p => p.IsOpen, true);
            ps.Add(p => p.IsOpenChanged, EventCallback.Factory.Create<bool>(this, _ => { }));
        });

        // Assert
        Assert.Contains("Назва/номер документа", cut.Markup);
        Assert.Single(cut.FindAll("form"));
        Assert.Contains("Скасувати", cut.Markup);
        Assert.Contains("Створити", cut.Markup);
    }

    [Fact]
    public void Cancel_Click_InvokesIsOpenChangedFalse()
    {
        // Arrange
        bool? openChanged = null;

        var cut = Render<CreateDocumentDrawer>(ps =>
        {
            ps.Add(p => p.IsOpen, true);
            ps.Add(p => p.IsOpenChanged, EventCallback.Factory.Create<bool>(this, v => openChanged = v));
        });

        // Act
        var cancelBtn = cut.FindAll("button").Single(b => b.TextContent.Contains("Скасувати"));
        cancelBtn.Click();

        // Assert
        Assert.False(openChanged);
    }

    [Fact]
    public void Submit_ValidForm_CallsHandler_RaisesOnCreated_AndClosesDrawer()
    {
        // Arrange
        var toast = new ToastService();
        ToastMessage? lastToast = null;
        toast.OnShow += m => lastToast = m;
        Services.AddSingleton(toast);

        var createdId = Guid.NewGuid();

        CreateCombatTaskDocumentCommand? captured = null;
        _handler
            .Setup(x => x.HandleAsync(It.IsAny<CreateCombatTaskDocumentCommand>(), It.IsAny<CancellationToken>()))
            .Callback<CreateCombatTaskDocumentCommand, CancellationToken>((cmd, _) => captured = cmd)
            .ReturnsAsync(createdId);

        Guid? onCreated = null;
        bool? openChanged = null;

        var cut = Render<CreateDocumentDrawer>(ps =>
        {
            ps.Add(p => p.IsOpen, true);
            ps.Add(p => p.OnCreated, EventCallback.Factory.Create<Guid>(this, id => onCreated = id));
            ps.Add(p => p.IsOpenChanged, EventCallback.Factory.Create<bool>(this, v => openChanged = v));
        });

        // Helper: always re-query from the current render tree
        IElement GetTitleInput()
            => cut.FindAll("input")
                .Where(i => i.GetAttribute("type") != "date")
                .ElementAt(0);

        IElement GetDescriptionInput()
            => cut.FindAll("input")
                .Where(i => i.GetAttribute("type") != "date")
                .ElementAt(1);

        IElement GetDateInput()
            => cut.Find("input[type=date]");

        // Act: fill form (re-query inputs after each change to avoid stale elements)
        GetTitleInput().Change("  План №12  ");
        GetDescriptionInput().Change("  Опис  ");
        GetDateInput().Change("2026-01-02");

        // submit (more reliable than clicking external button)
        cut.Find("form").Submit();

        // Assert
        cut.WaitForAssertion(() =>
        {
            _handler.Verify(
                x => x.HandleAsync(It.IsAny<CreateCombatTaskDocumentCommand>(), It.IsAny<CancellationToken>()),
                Times.Once);

            Assert.NotNull(captured);
            Assert.Equal("План №12", captured!.OrderTitle);
            Assert.Equal("Опис", captured.Description); // normalized
            Assert.Equal(new DateOnly(2026, 1, 2), captured.RecordedAt);
            Assert.Equal("ui", captured.Author);
            Assert.Equal(DateTimeKind.Utc, captured.NowUtc.Kind);

            Assert.Equal(createdId, onCreated);
            Assert.False(openChanged);

            Assert.NotNull(lastToast);
            Assert.Equal(ToastKind.Success, lastToast!.Kind);
            Assert.Contains("Документ створено", lastToast.Title);
        });
    }

    [Fact]
    public void Submit_WhenHandlerThrows_ShowsErrorToast_AndDoesNotCloseOrInvokeOnCreated()
    {
        // Arrange
        var toast = new ToastService();
        ToastMessage? lastToast = null;
        toast.OnShow += m => lastToast = m;
        Services.AddSingleton(toast);

        _handler
             .Setup(x => x.HandleAsync(It.IsAny<CreateCombatTaskDocumentCommand>(), It.IsAny<CancellationToken>()))
             .ThrowsAsync(new InvalidOperationException("boom"));

        Guid? onCreated = null;
        bool? openChanged = null;

        var cut = Render<CreateDocumentDrawer>(ps =>
        {
            ps.Add(p => p.IsOpen, true);
            ps.Add(p => p.OnCreated, EventCallback.Factory.Create<Guid>(this, id => onCreated = id));
            ps.Add(p => p.IsOpenChanged, EventCallback.Factory.Create<bool>(this, v => openChanged = v));
        });

        var inputs = cut.FindAll("input");
        var titleInput = inputs.First(i => i.GetAttribute("type") != "date");
        titleInput.Change("План");

        // Act
        cut.Find("form").Submit();

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Null(onCreated);
            Assert.Null(openChanged);
            Assert.NotNull(lastToast);
            Assert.Equal(ToastKind.Error, lastToast!.Kind);
            Assert.Contains("boom", lastToast.Title);
        });
    }
}
