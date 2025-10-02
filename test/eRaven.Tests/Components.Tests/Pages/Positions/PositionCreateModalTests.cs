//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// PositionCreateModalTests (no data-testid, safe for re-render)
//-----------------------------------------------------------------------------

using AngleSharp.Dom;
using Blazored.Toast.Services;
using Bunit;
using eRaven.Components.Pages.Positions.Modals;
using eRaven.Domain.Models;
using FluentValidation;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace eRaven.Tests.Components.Tests.Pages.Positions;

public class PositionCreateModalTests : TestContext
{
    private readonly Mock<IPositionService> _service = new();
    private readonly Mock<IToastService> _toast = new();

    public PositionCreateModalTests()
    {
        // DI: достатньо валідатора і сервісів
        Services.AddSingleton(_service.Object);
        Services.AddSingleton(_toast.Object);
        Services.AddTransient<IValidator<CreatePositionUnitViewModel>, CreatePositionUnitViewModelValidator>();
    }

    // ---------- helpers ------------------------------------------------------

    private static Task OpenAsync(IRenderedComponent<PositionCreateModal> cut)
        => cut.InvokeAsync(() => cut.Instance.Open());

    private static IHtmlCollection<IElement> Inputs(IRenderedComponent<PositionCreateModal> cut)
        => cut.Find("form").GetElementsByTagName("input");

    // для стабільності – кожен раз пере-знаходимо input-и
    private static Task SetCodeAsync(IRenderedComponent<PositionCreateModal> cut, string v)
        => cut.InvokeAsync(() => Inputs(cut)[0].Change(v));

    private static Task SetShortNameAsync(IRenderedComponent<PositionCreateModal> cut, string v)
        => cut.InvokeAsync(() => Inputs(cut)[1].Change(v));

    private static Task SetSpecialAsync(IRenderedComponent<PositionCreateModal> cut, string v)
        => cut.InvokeAsync(() => Inputs(cut)[2].Change(v));

    private static Task SetOrgPathAsync(IRenderedComponent<PositionCreateModal> cut, string v)
        => cut.InvokeAsync(() => Inputs(cut)[3].Change(v));

    private static async Task FillValidAsync(IRenderedComponent<PositionCreateModal> cut, string code = "A1")
    {
        await SetCodeAsync(cut, code);
        await SetShortNameAsync(cut, "Позиція");
        await SetSpecialAsync(cut, "12-345");
        await SetOrgPathAsync(cut, "Підрозділ / Відділ");
    }

    // ---------- tests --------------------------------------------------------

    [Fact]
    public async Task Submit_Valid_CallsService_AndOnCreated_ThenCloses()
    {
        // Arrange
        _service.Setup(s => s.CodeExistsActiveAsync("A1", default)).ReturnsAsync(false);
        _service.Setup(s => s.CreatePositionAsync(It.IsAny<PositionUnit>(), default))
                .ReturnsAsync(new PositionUnit
                {
                    Id = Guid.NewGuid(),
                    Code = "A1",
                    ShortName = "Позиція",
                    SpecialNumber = "12-345",
                    OrgPath = "Підрозділ / Відділ",
                    IsActived = true
                });

        bool onCloseCalled = false;
        PositionUnitViewModel? createdVm = null;

        var cut = RenderComponent<PositionCreateModal>(ps => ps
            .Add(p => p.OnClose, EventCallback.Factory.Create<bool>(this, _ => onCloseCalled = true))
            .Add(p => p.OnCreated, EventCallback.Factory.Create<PositionUnitViewModel>(this, vm => createdVm = vm))
        );

        await OpenAsync(cut);
        await FillValidAsync(cut);

        // Act
        await cut.InvokeAsync(() => cut.Find("form").Submit());

        // Assert
        cut.WaitForAssertion(() =>
        {
            _service.Verify(s => s.CreatePositionAsync(It.IsAny<PositionUnit>(), default), Times.Once);
            Assert.NotNull(createdVm);
            Assert.True(onCloseCalled);
            Assert.False(cut.Instance.IsOpen);
        });
    }

    [Fact]
    public async Task Submit_DuplicateCode_ShowsValidationError_And_DoesNotCallService()
    {
        // Arrange
        _service.Setup(s => s.CodeExistsActiveAsync("A1", default)).ReturnsAsync(true);

        var cut = RenderComponent<PositionCreateModal>();
        await OpenAsync(cut);
        await FillValidAsync(cut, code: "A1");

        // Act
        await cut.InvokeAsync(() => cut.Find("form").Submit());

        // Assert
        cut.WaitForAssertion(() =>
        {
            _service.Verify(s => s.CreatePositionAsync(It.IsAny<PositionUnit>(), default), Times.Never);
            Assert.True(cut.Instance.IsOpen);
            Assert.NotEmpty(cut.FindAll(".validation-message"));
        });
    }

    [Fact]
    public async Task Cancel_Click_Closes_And_Fires_OnCloseFalse()
    {
        // Arrange
        bool? closedArg = null;

        var cut = RenderComponent<PositionCreateModal>(ps => ps
            .Add(p => p.OnClose, EventCallback.Factory.Create<bool>(this, v => closedArg = v))
        );

        await OpenAsync(cut);

        // Act
        await cut.InvokeAsync(() => cut.Find("button.btn.btn-sm.btn-warning").Click());

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.False(cut.Instance.IsOpen);
            Assert.True(closedArg.HasValue);
            Assert.False(closedArg!.Value);
        });
    }

    [Fact]
    public async Task Submit_Invalid_DoesNotCallService_StaysOpen_ShowsErrors()
    {
        // Arrange: нехай сервіс каже, що код вільний — все одно має завалитись на клієнтській валідації
        _service.Setup(s => s.CodeExistsActiveAsync(It.IsAny<string>(), default)).ReturnsAsync(false);

        var cut = RenderComponent<PositionCreateModal>();
        await OpenAsync(cut);

        // Заповнюємо лише Code, решту обов'язкових залишаємо порожніми
        await SetCodeAsync(cut, "A1");

        // Act
        await cut.InvokeAsync(() => cut.Find("form").Submit());

        // Assert
        cut.WaitForAssertion(() =>
        {
            _service.Verify(s => s.CreatePositionAsync(It.IsAny<PositionUnit>(), default), Times.Never);
            Assert.True(cut.Instance.IsOpen);
            Assert.NotEmpty(cut.FindAll(".validation-message"));
        });
    }
}