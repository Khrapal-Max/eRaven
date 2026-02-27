//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// AddTimesheetCodeDrawerTests
//-----------------------------------------------------------------------------

using Bunit;
using eRaven.Application.Commands;
using eRaven.Application.Commands.Timesheets;
using eRaven.Application.DTOs.Enums;
using eRaven.Application.DTOs.Timesheets.Policy;
using eRaven.Application.Queries;
using eRaven.Application.Queries.Timesheets;
using eRaven.Components.Pages.Timesheets.Drawers;
using eRaven.Domain.Enums;
using eRaven.Presentation.Toasts;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace eRaven.Tests.Components.Pages.Timesheets.Drawers;

public sealed class AddTimesheetCodeDrawerTests : BunitContext
{
    private readonly ToastService _toastMock = new();
    private readonly Mock<ICommandHandler<AddTimesheetCodeCommand, Guid>> _handlerMock = new(MockBehavior.Strict);
    private readonly Mock<ICommandHandler<SaveTimesheetPolicyCommand>> _saveMock = new(MockBehavior.Strict);
    private readonly Mock<IQueryHandler<GetTimesheetPolicyCodesQuery, IReadOnlyList<TimesheetCodeDto>>> _codesHandlerMock = new(MockBehavior.Strict);

    public AddTimesheetCodeDrawerTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        Services.AddSingleton(_toastMock);
        Services.AddSingleton(_handlerMock.Object);
        Services.AddSingleton(_saveMock.Object);
        Services.AddSingleton(_codesHandlerMock.Object);
    }

    [Fact]
    public void Render_ShowsAllRequiredControlsForWork()
    {
        var cut = Render<AddTimesheetCodeDrawer>(ps => ps
            .Add(p => p.IsOpen, true));

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Створити табельний код", cut.Markup);
            Assert.Contains("Код", cut.Markup);
            Assert.Contains("Назва", cut.Markup);
            Assert.Contains("Опис", cut.Markup);
            Assert.Contains("Порядок", cut.Markup);
            Assert.Contains("Пріоритет", cut.Markup);
            Assert.Contains("Role", cut.Markup);
            Assert.Contains("UI Style", cut.Markup);

            // Inputs
            Assert.NotEmpty(cut.FindAll("input"));
            Assert.NotEmpty(cut.FindAll("select"));

            // Action buttons
            Assert.Contains("Скасувати", cut.Markup);
            Assert.Contains("Створити", cut.Markup);
        });
    }

    [Fact]
    public void InjectedServices_AndCallbacks_AreUsed_OnCreate()
    {
        var newId = Guid.NewGuid();
        _handlerMock
            .Setup(x => x.HandleAsync(It.IsAny<AddTimesheetCodeCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(newId);

        Guid? created = null;
        var receiver = new object();
        var onCreated = EventCallback.Factory.Create<Guid>(receiver, (Action<Guid>)(id => created = id));

        var cut = Render<AddTimesheetCodeDrawer>(ps => ps
            .Add(p => p.IsOpen, true)
            .Add(p => p.OnCreated, onCreated));

        // Fill required fields.
        var inputs = cut.FindAll("input");

        // Order: Code, Title, Description, SortOrder(number), Priority(number), IsTerminal(checkbox)
        var codeInput = inputs.First(i => (i.GetAttribute("class") ?? string.Empty).Contains("font-monospace"));
        codeInput.Change("  ЛХ  ");

        inputs = cut.FindAll("input");
        var titleInput = inputs.First(i => (i.GetAttribute("class") ?? string.Empty).Contains("form-control") && !(i.GetAttribute("class") ?? string.Empty).Contains("font-monospace"));
        titleInput.Change("  Легкий хід  ");

        // Numbers
        var numberInputs = cut.FindAll("input[type='number']");
        numberInputs[0].Change("5");
        numberInputs = cut.FindAll("input[type='number']");
        numberInputs[1].Change("2");

        // Select RoleCode and UiStyle (keep defaults; just ensure selects exist)
        var selects = cut.FindAll("select");
        Assert.True(selects.Count >= 2);
        selects[0].Change(RoleCode.TransitionCode.ToString());
        selects[1].Change(TimesheetUiStyle.Warning.ToString());

        // Click create
        var createBtn = cut.FindAll("button")
            .First(bn => bn.TextContent.Contains("Створити", StringComparison.OrdinalIgnoreCase));

        createBtn.Click();

        cut.WaitForAssertionAsync(() =>
        {
            _handlerMock.Verify(x => x.HandleAsync(
                It.Is<AddTimesheetCodeCommand>(cmd =>
                    cmd.Code == "ЛХ" &&
                    cmd.Title == "Легкий хід" &&
                    cmd.SortOrder == 5 &&
                    cmd.Priority == 2 &&
                    cmd.RoleCode == RoleCodeDto.TransitionCode &&
                    cmd.UiStyle == TimesheetUiStyleDto.Warning
                ),
                It.IsAny<CancellationToken>()
            ), Times.Once);

            Assert.Equal(newId, created);
        });
    }
}
