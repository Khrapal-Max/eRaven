//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetPolicyConfiguratorTests
//-----------------------------------------------------------------------------

using Bunit;
using eRaven.Application.Commands;
using eRaven.Application.Commands.Timesheets;
using eRaven.Application.DTOs.Enums;
using eRaven.Application.DTOs.Timesheets.Policy;
using eRaven.Application.Queries;
using eRaven.Application.Queries.Timesheets;
using eRaven.Components.Pages.Timesheets.Policy;
using eRaven.Presentation.Toasts;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace eRaven.Tests.Components.Pages.Timesheets.Policy;

public sealed class TimesheetPolicyConfiguratorTests : BunitContext
{
    private readonly ToastService _toastMock = new();
    private readonly Mock<IQueryHandler<GetTimesheetPolicyCodesQuery, IReadOnlyList<TimesheetCodeDto>>> _codesHandlerMock = new(MockBehavior.Strict);
    private readonly Mock<IQueryHandler<GetTimesheetPolicyForCodeQuery, TimesheetPolicyEditorDto?>> _policyHandlerMock = new(MockBehavior.Strict);
    private readonly Mock<ICommandHandler<SaveTimesheetPolicyCommand>> _saveMock = new(MockBehavior.Strict);

    // Nested drawers exist in markup => their handlers must be registered.
    private readonly Mock<ICommandHandler<AddTimesheetCodeCommand, Guid>> _addMock = new(MockBehavior.Loose);
    private readonly Mock<ICommandHandler<CloseTimesheetCodeCommand>> _closeMock = new(MockBehavior.Loose);

    public TimesheetPolicyConfiguratorTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        Services.AddSingleton(_toastMock);
        Services.AddSingleton(_codesHandlerMock.Object);
        Services.AddSingleton(_policyHandlerMock.Object);
        Services.AddSingleton(_saveMock.Object);
        Services.AddSingleton(_addMock.Object);
        Services.AddSingleton(_closeMock.Object);
    }

    [Fact]
    public void Render_ShowsAllRequiredControlsForWork()
    {
        var a = new TimesheetCodeDto(Guid.NewGuid(), "30", "Готовність", null, 10, 1, false, true, RoleCodeDto.TransitionCode, TimesheetUiStyleDto.Ready);
        var b = new TimesheetCodeDto(Guid.NewGuid(), "Т", "Перехід", null, 20, 1, false, true, RoleCodeDto.TransitionCode, TimesheetUiStyleDto.Warning);
        var c = new TimesheetCodeDto(Guid.NewGuid(), "Ф100", "Форс-мажор", null, 30, 10, false, true, RoleCodeDto.EmergencyCode, TimesheetUiStyleDto.Danger);

        var codes = (IReadOnlyList<TimesheetCodeDto>)[a, b, c];

        _codesHandlerMock
            .Setup(x => x.HandleAsync(It.IsAny<GetTimesheetPolicyCodesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(codes);

        _policyHandlerMock
            .Setup(x => x.HandleAsync(It.Is<GetTimesheetPolicyForCodeQuery>(q => q.CodeId == a.Id), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TimesheetPolicyEditorDto(
                Code: a,
                AllowedTransitions:
                [
                    new(b.Id, 0),
                    new(c.Id, 1)
                ]));

        var cut = Render<TimesheetPolicyConfigurator>();

        // Wait for initial load of codes.
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Політика переходів", cut.Markup);
            Assert.Contains("Додати код", cut.Markup);
            Assert.Contains("Закрити код", cut.Markup);
            Assert.Contains("Зберегти", cut.Markup);

            var listItems = cut.FindAll("button.list-group-item");
            Assert.Equal(3, listItems.Count);
        });

        // Select the first code to open editor UI.
        cut.FindAll("button.list-group-item")[0].Click();

        cut.WaitForAssertion(() =>
        {
            // Inputs required for work.
            var inputs = cut.FindAll("input.form-control");
            Assert.True(inputs.Count >= 4, "Expected inputs for Title/Description/SortOrder/Priority.");

            // Terminal checkbox.
            Assert.NotNull(cut.Find("input.form-check-input[type='checkbox']"));

            // Allowed transitions list (checkboxes for each other code).
            var transitionCheckboxes = cut.FindAll("div.border.rounded-2 input.form-check-input[type='checkbox']");
            Assert.True(transitionCheckboxes.Count >= 2);

            // Shift select should be rendered for enabled transitions.
            Assert.NotEmpty(cut.FindAll("select.form-select"));
        });

        // Verify query handlers were invoked.
        _codesHandlerMock.Verify(x => x.HandleAsync(It.IsAny<GetTimesheetPolicyCodesQuery>(), It.IsAny<CancellationToken>()), Times.Once);
        _policyHandlerMock.Verify(x => x.HandleAsync(It.Is<GetTimesheetPolicyForCodeQuery>(q => q.CodeId == a.Id), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void InjectedHandlers_AreUsed_OnSelectAndSave()
    {
        var a = new TimesheetCodeDto(Guid.NewGuid(), "30", "Готовність", null, 10, 1, false, true, RoleCodeDto.TransitionCode, TimesheetUiStyleDto.Ready);
        var b = new TimesheetCodeDto(Guid.NewGuid(), "Т", "Перехід", null, 20, 1, false, true, RoleCodeDto.TransitionCode, TimesheetUiStyleDto.Warning);

        var codes = (IReadOnlyList<TimesheetCodeDto>)[a, b];

        // Initial load + reload after save.
        _codesHandlerMock
            .Setup(x => x.HandleAsync(It.IsAny<GetTimesheetPolicyCodesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(codes);

        // Selection is invoked initially and again after save (refresh selected view).
        _policyHandlerMock
            .Setup(x => x.HandleAsync(It.Is<GetTimesheetPolicyForCodeQuery>(q => q.CodeId == a.Id), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TimesheetPolicyEditorDto(
                Code: a,
                AllowedTransitions: [new(b.Id, 0)]));

        _saveMock
            .Setup(x => x.HandleAsync(It.IsAny<SaveTimesheetPolicyCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var cut = Render<TimesheetPolicyConfigurator>();

        // Select code.
        cut.WaitForAssertion(() => Assert.Equal(2, cut.FindAll("button.list-group-item").Count));
        cut.FindAll("button.list-group-item")[0].Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("30", cut.Markup);
            Assert.Contains("Готовність", cut.Markup);
        });

        // Change title => enable Save.
        var titleInput = cut.FindAll("input.form-control.form-control-sm")
            .First(x => x.GetAttribute("class")!.Contains("form-control"));

        titleInput.Input("  Нова назва  ");

        // Click save.
        var saveBtn = cut.FindAll("button")
            .First(bn => bn.TextContent.Contains("Зберегти", StringComparison.OrdinalIgnoreCase));

        saveBtn.Click();

        cut.WaitForAssertion(() =>
        {
            _saveMock.Verify(x => x.HandleAsync(
                It.Is<SaveTimesheetPolicyCommand>(cmd =>
                    cmd.CodeId == a.Id &&
                    cmd.Title == "Нова назва" &&
                    cmd.AllowedTransitions.Any(t => t.ToCodeId == b.Id && t.StartShiftDays == 0)
                ),
                It.IsAny<CancellationToken>()
            ), Times.Once);

            // After save, the page reloads list and re-selects.
            _policyHandlerMock.Verify(x => x.HandleAsync(It.Is<GetTimesheetPolicyForCodeQuery>(q => q.CodeId == a.Id), It.IsAny<CancellationToken>()), Times.AtLeast(2));
            _codesHandlerMock.Verify(x => x.HandleAsync(It.IsAny<GetTimesheetPolicyCodesQuery>(), It.IsAny<CancellationToken>()), Times.AtLeast(2));
        });
    }
}
