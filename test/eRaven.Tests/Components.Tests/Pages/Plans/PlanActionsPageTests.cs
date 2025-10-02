//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// PlanActionsPageTests
//-----------------------------------------------------------------------------

using Blazored.Toast.Services;
using Bunit;
using eRaven.Application.Services.PersonService;
using eRaven.Application.Services.PersonStatusService;
using eRaven.Application.Services.PlanActionService;
using eRaven.Application.Services.StatusKindService;
using eRaven.Application.ViewModels.PlanActionViewModels;
using eRaven.Components.Pages.PlanActions;
using eRaven.Components.Pages.PlanActions.Modals;
using eRaven.Domain.Enums;
using eRaven.Domain.Models;
using eRaven.Domain.Person;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Linq.Expressions;

namespace eRaven.Tests.Components.Tests.Pages.Plans;

public class PlanActionsPageTests : IDisposable
{
    private readonly TestContext _ctx;
    private readonly Mock<IPlanActionService> _planActionSvc = new();
    private readonly Mock<IPersonService> _personSvc = new();
    private readonly Mock<IPersonStatusService> _personStatusSvc = new();
    private readonly Mock<IStatusKindService> _statusKindSvc = new();
    private readonly Mock<IToastService> _toast = new();

    public PlanActionsPageTests()
    {
        _ctx = new TestContext();

        // DI
        _ctx.Services.AddSingleton(_planActionSvc.Object);
        _ctx.Services.AddSingleton(_personSvc.Object);
        _ctx.Services.AddSingleton(_personStatusSvc.Object);
        _ctx.Services.AddSingleton(_statusKindSvc.Object);
        _ctx.Services.AddSingleton(_toast.Object);

        // Типові стаби завантаження
        _personSvc
            .Setup(s => s.SearchAsync(
                It.IsAny<Expression<Func<Person, bool>>?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Person>());

        _statusKindSvc
            .Setup(s => s.GetAllAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]); // не критично для тестів нижче

        _planActionSvc
            .Setup(s => s.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]); // порожньо за замовчуванням
    }

    public void Dispose() => GC.SuppressFinalize(this);

    private static PlanAction MakeAction(MoveType move) => new()
    {
        Id = Guid.NewGuid(),
        PersonId = Guid.NewGuid(),
        EffectiveAtUtc = DateTime.SpecifyKind(new DateTime(2025, 9, 21, 10, 0, 0), DateTimeKind.Utc),
        PlanActionName = "R-77/25",
        MoveType = move,
        ActionState = ActionState.PlanAction,
        // решта полів для відображення не критичні
    };

    [Fact]
    public void Renders_Toolbar_Create_Disabled_Without_SelectedPerson()
    {
        // Arrange
        var cut = _ctx.RenderComponent<PlanActionsPage>();

        // Act
        var createBtn = cut.FindAll("button")
            .FirstOrDefault(b => b.TextContent.Contains("Створити", StringComparison.OrdinalIgnoreCase));

        // Assert
        Assert.NotNull(createBtn);
        Assert.True(createBtn!.HasAttribute("disabled"),
            "Кнопка 'Створити' має бути відключена, коли особу не вибрано.");
    }

    [Fact]
    public async Task Approve_Flow_Dispatch_Sets_StatusKindId_2_And_Calls_Approve()
    {
        // Arrange
        var action = MakeAction(MoveType.Dispatch);
        var cut = _ctx.RenderComponent<PlanActionsPage>();

        // Знаходимо дочірню модалку апрува і відкриваємо її з нашим action
        var approveModal = cut.FindComponent<ApproveModal>();
        await cut.InvokeAsync(() => approveModal.Instance.Open(action));

        // Вводимо номер наказу й сабмітимо форму
        var input = approveModal.Find("#order-input");
        input.Change("  БР-10/25  "); // з пробілами для перевірки Trim
        approveModal.Find("form").Submit();

        // Assert: перевіряємо виклики сервісів з очікуваними параметрами
        _personStatusSvc.Verify(s => s.SetStatusAsync(
                It.Is<PersonStatus>(ps =>
                    ps.PersonId == action.PersonId
                    && ps.OpenDate == action.EffectiveAtUtc
                    && ps.StatusKindId == 2                 // Dispatch -> В БР
                    && ps.Note == "БР-10/25"                // Trim застосовано всередині модалки
                    && ps.IsActive
                ),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _planActionSvc.Verify(s => s.ApproveAsync(
                It.Is<ApprovePlanActionViewModel>(vm =>
                    vm.Id == action.Id
                    && vm.PersonId == action.PersonId
                    && vm.EffectiveAtUtc == action.EffectiveAtUtc
                    && vm.Order == "БР-10/25"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Approve_Flow_Return_Sets_StatusKindId_1_And_Calls_Approve()
    {
        // Arrange
        var action = MakeAction(MoveType.Return);
        var cut = _ctx.RenderComponent<PlanActionsPage>();

        var approveModal = cut.FindComponent<ApproveModal>();
        await cut.InvokeAsync(() => approveModal.Instance.Open(action));

        // Вводимо наказ і сабмітимо
        approveModal.Find("#order-input").Change("BR-2/25");
        approveModal.Find("form").Submit();

        // Assert
        _personStatusSvc.Verify(s => s.SetStatusAsync(
                It.Is<PersonStatus>(ps =>
                    ps.PersonId == action.PersonId
                    && ps.OpenDate == action.EffectiveAtUtc
                    && ps.StatusKindId == 1     // Return -> В районі
                    && ps.Note == "BR-2/25"
                    && ps.IsActive),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _planActionSvc.Verify(s => s.ApproveAsync(
                It.Is<ApprovePlanActionViewModel>(vm =>
                    vm.Id == action.Id &&
                    vm.Order == "BR-2/25"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ViewModal_Open_From_Page_Renders_Readonly_Data()
    {
        // Тут ми не клікаємо по таблиці (вона кастомна), а відкриваємо перегляд напряму через дочірню модалку
        var action = MakeAction(MoveType.Dispatch);
        action.FullName = "Тест Іван";
        action.Location = "Sector B";

        var cut = _ctx.RenderComponent<PlanActionsPage>();

        var view = cut.FindComponent<ViewPlanActionModal>();
        await cut.InvokeAsync(() => view.Instance.Open(action));

        // Перевіряємо, що модалка показує наші дані (readonly інпути містять value)
        cut.WaitForAssertion(() =>
        {
            var inputs = view.FindAll("input.form-control");
            Assert.Contains(inputs, i => (i.GetAttribute("value") ?? "").Contains("Тест Іван"));
            Assert.Contains(inputs, i => (i.GetAttribute("value") ?? "").Contains("Sector B"));
        });
    }
}
