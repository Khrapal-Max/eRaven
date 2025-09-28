//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// Tests: StatusTransitionsPage (bUnit + Moq + xUnit)
//-----------------------------------------------------------------------------

using Blazored.Toast.Services;
using Bunit;
using eRaven.Application.Services.ConfirmService;
using eRaven.Application.Services.StatusKindService;
using eRaven.Application.Services.StatusTransitionService;
using eRaven.Components.Pages.StatusTransitions;
using eRaven.Domain.Models;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace eRaven.Tests.Components.Tests.Pages.StatusTransitions;

public class StatusTransitionsPageTests : TestContext
{
    // ---- Моки сервісів (DI) ------------------------------------------------
    private readonly Mock<IStatusKindService> _kindSvc = new(MockBehavior.Strict);
    private readonly Mock<IStatusTransitionService> _transitionSvc = new(MockBehavior.Strict);
    private readonly Mock<IToastService> _toast = new(MockBehavior.Loose);
    private readonly Mock<IConfirmService> _confirmMock = new(MockBehavior.Loose);

    // ---- Тестові дані ------------------------------------------------------
    private readonly List<StatusKind> _kinds =
    [
        new StatusKind { Id = 1, Name = "Готовий", Code = "RDY", Order = 1, IsActive = false },
        new StatusKind { Id = 2, Name = "Відпустка", Code = "VAC", Order = 2, IsActive = true },
        new StatusKind { Id = 3, Name = "Хворіє",   Code = "SICK",Order = 3, IsActive = true },
    ];

    public StatusTransitionsPageTests()
    {
        // DI
        Services.AddSingleton(_kindSvc.Object);
        Services.AddSingleton(_transitionSvc.Object);
        Services.AddSingleton(_toast.Object);
        Services.AddSingleton(_confirmMock.Object);
    }

    private IRenderedComponent<StatusTransitionsPage> RenderWithData(
        IEnumerable<StatusKind>? kinds = null,
        Dictionary<int, HashSet<int>>? toMap = null)
    {
        kinds ??= _kinds;

        _kindSvc.Reset();
        _transitionSvc.Reset();

        _kindSvc
            .Setup(s => s.GetAllAsync(true, It.IsAny<CancellationToken>()))
            .ReturnsAsync([.. kinds]);

        toMap ??= [];
        _transitionSvc
            .Setup(s => s.GetToIdsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int fromId, CancellationToken _) =>
                toMap.TryGetValue(fromId, out var hs) ? [.. hs] : []);

        // ВАЖЛИВО: тепер повертаємо bool
        _kindSvc
            .Setup(s => s.SetActiveAsync(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _transitionSvc
            .Setup(s => s.SaveAllowedAsync(It.IsAny<int>(), It.IsAny<HashSet<int>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        return RenderComponent<StatusTransitionsPage>();
    }

    // ---------------------------------------------------------------------
    // Рендер порожнього списку
    // ---------------------------------------------------------------------
    [Fact(DisplayName = "Render: when no kinds -> shows empty state")]
    public void Render_NoKinds_ShowsEmpty()
    {
        var cut = RenderWithData(kinds: []);

        cut.WaitForState(() => cut.Markup.Contains("Немає статусів"));

        _kindSvc.Verify(s => s.GetAllAsync(true, It.IsAny<CancellationToken>()), Times.Once);
        _transitionSvc.Verify(s => s.GetToIdsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ---------------------------------------------------------------------
    // Рендер з даними + автоселект першого From
    // ---------------------------------------------------------------------
    [Fact(DisplayName = "Init: loads kinds and selects the first as From")]
    public void Init_LoadsAndSelectsFirst()
    {
        var toMap = new Dictionary<int, HashSet<int>> { [1] = [2] };
        var cut = RenderWithData(toMap: toMap);

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("з: Готовий (RDY)", cut.Markup);
        });

        _kindSvc.Verify(s => s.GetAllAsync(true, It.IsAny<CancellationToken>()), Times.Once);
        _transitionSvc.Verify(s => s.GetToIdsAsync(1, It.IsAny<CancellationToken>()), Times.Once);
    }

    // ---------------------------------------------------------------------
    // Клік по іншому статусу зліва змінює вибір і тягне нові allowed
    // ---------------------------------------------------------------------
    [Fact(DisplayName = "Select From: click row changes current and loads ToIds")]
    public void SelectFrom_ClickRow_LoadsNewToIds()
    {
        var toMap = new Dictionary<int, HashSet<int>>
        {
            [1] = [2],
            [2] = [3]
        };

        var cut = RenderWithData(toMap: toMap);

        // Клік по другому рядку (Відпустка)
        var leftRows = cut.FindAll("tbody tr").Where(tr => tr.TextContent.Contains("Готовий") == false).ToList();
        // В таблиці зліва кожен рядок має всю інформацію; знайдемо саме той, що містить "Відпустка"
        var vacationRow = cut.FindAll("tbody tr").First(tr => tr.TextContent.Contains("Відпустка"));
        vacationRow.Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("з: Відпустка (VAC)", cut.Markup);
        });

        _transitionSvc.Verify(s => s.GetToIdsAsync(2, default), Times.Once);
    }

    // ---------------------------------------------------------------------
    // Права панель: self-перехід — чекбокс вимкнений
    // ---------------------------------------------------------------------
    [Fact(DisplayName = "Right: self transition checkbox is disabled")]
    public void Right_SelfCheckbox_Disabled()
    {
        var cut = RenderWithData();

        // Дочекаймося, поки права панель відрендериться з обраним From
        cut.WaitForAssertion(() =>
            Assert.Contains("Дозволені переходи", cut.Markup));

        // 1) Знаходимо ПРАВУ картку ("Дозволені переходи")
        var rightCard = cut.FindAll("div.card")
                           .First(div => div.TextContent.Contains("Дозволені переходи"));

        // 2) В ній знаходимо рядок із self ("Готовий" — це _selectedFromId за замовчуванням)
        var selfRow = rightCard.QuerySelectorAll("tbody tr")
                               .First(tr => tr.TextContent.Contains("Готовий"));

        // 3) Перевіряємо, що інпут disabled
        var selfCheckbox = selfRow.QuerySelector("input.form-check-input");
        Assert.NotNull(selfCheckbox);
        Assert.True(selfCheckbox!.HasAttribute("disabled"));
    }

    // ---------------------------------------------------------------------
    // Сервіси з DI були викликані у процесі (загальна sanity-перевірка)
    // ---------------------------------------------------------------------
    [Fact(DisplayName = "Services: DI services are used on init")]
    public void Services_Used_OnInit()
    {
        var cut = RenderWithData();
        _kindSvc.Verify(s => s.GetAllAsync(true, It.IsAny<CancellationToken>()), Times.Once);
        _transitionSvc.Verify(s => s.GetToIdsAsync(1, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(DisplayName = "Left: toggle active calls KindService.SetActiveAsync when confirm=YES")]
    public void Left_ToggleActive_ConfirmYes_CallsService()
    {
        // Arrange
        _confirmMock.Setup(c => c.AskAsync(It.IsAny<string>())).ReturnsAsync(true);

        _kindSvc.Setup(s => s.GetAllAsync(true, It.IsAny<CancellationToken>()))
                .ReturnsAsync([new StatusKind { Id = 1, Name = "Готовий", Code = "RDY", IsActive = false }]);
        _kindSvc.Setup(s => s.SetActiveAsync(1, true, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

        // викор. helper, щоб мати сетапи на TransitionService
        var cut = RenderWithData(kinds: [new StatusKind { Id = 1, Name = "Готовий", Code = "RDY", IsActive = false }],
                                 toMap: new());

        // Дочекаємось, що ліва картка відрендерилась
        cut.WaitForAssertion(() =>
            Assert.Contains("З якого статусу", cut.Markup));

        // 1) Знайдемо ЛІВУ картку ("З якого статусу")
        var leftCard = cut.FindAll("div.card")
                          .First(div => div.TextContent.Contains("З якого статусу"));

        // 2) В ній знайдемо рядок "Готовий" та його чекбокс (це active-toggle)
        var leftRow = leftCard.QuerySelectorAll("tbody tr")
                              .First(tr => tr.TextContent.Contains("Готовий"));
        var checkbox = leftRow.QuerySelector("input.form-check-input");
        Assert.NotNull(checkbox);

        // Act: клік по чекбоксу -> ConfirmService.AskAsync(true) -> SaveActiveAsync
        checkbox!.Click();

        // Assert
        _confirmMock.Verify(c => c.AskAsync(It.Is<string>(msg => msg.Contains("Готовий"))), Times.Once);
        _kindSvc.Verify(s => s.SetActiveAsync(1, true, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(DisplayName = "Left: toggle active cancelled when confirm=NO")]
    public void Left_ToggleActive_ConfirmNo_Cancels()
    {
        // Arrange
        _confirmMock.Setup(c => c.AskAsync(It.IsAny<string>())).ReturnsAsync(false);

        _kindSvc.Setup(s => s.GetAllAsync(true, It.IsAny<CancellationToken>()))
                .ReturnsAsync([new StatusKind { Id = 1, Name = "Готовий", Code = "RDY" }]);

        // ✅ використовуємо RenderWithData
        var cut = RenderWithData(kinds: [new StatusKind { Id = 1, Name = "Готовий", Code = "RDY" }],
                                 toMap: []);

        // Act
        var row = cut.Find("tbody tr");
        var checkbox = row.QuerySelector("input.form-check-input")!;
        checkbox.Click();

        // Assert
        _kindSvc.Verify(s => s.SetActiveAsync(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
        _confirmMock.Verify(c => c.AskAsync(It.Is<string>(msg => msg.Contains("Готовий"))), Times.Once);
    }
}
