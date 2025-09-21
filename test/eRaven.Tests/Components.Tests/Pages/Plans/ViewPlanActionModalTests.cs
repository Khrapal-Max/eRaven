//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// ViewPlanActionModalTests
//-----------------------------------------------------------------------------

using Bunit;
using eRaven.Components.Pages.PlanActions.Modals;
using eRaven.Domain.Enums;
using eRaven.Domain.Models;

namespace eRaven.Tests.Components.Tests.Pages.Plans;

public class ViewPlanActionModalTests : IDisposable
{
    private readonly TestContext _ctx = new();

    public void Dispose() => GC.SuppressFinalize(this);

    // ---------- helpers ----------
    private static PlanAction MakePlanAction(MoveType move = MoveType.Dispatch, ActionState state = ActionState.PlanAction)
    {
        return new PlanAction
        {
            Id = Guid.NewGuid(),
            PersonId = Guid.NewGuid(),
            PlanActionName = "R-007/25",
            EffectiveAtUtc = DateTime.SpecifyKind(new DateTime(2025, 9, 21, 10, 30, 0), DateTimeKind.Utc),
            ToStatusKindId = null,
            Order = state == ActionState.ApprovedOrder ? "БР-1/25" : null,
            ActionState = state,
            MoveType = move,
            Location = "Sector B",
            GroupName = "Alpha",
            CrewName = "Crew-1",
            Note = "Test note",
            Rnokpp = "1234567890",
            FullName = "Іванов Іван Іванович",
            RankName = "Сержант",
            PositionName = "Відділення розвідки",
            BZVP = "БЗ-42",
            Weapon = "АК-74",
            Callsign = "Сокіл",
            StatusKindOnDate = "В строю 21.09.2025 09:00"
        };
    }

    // ---------- tests ----------

    [Fact]
    public void Initially_Hidden()
    {
        // Arrange
        var cut = _ctx.RenderComponent<ViewPlanActionModal>();

        // Act
        var modal = cut.Find("div.modal");

        // Assert
        Assert.DoesNotContain("show", modal.ClassList);
        Assert.DoesNotContain("d-block", modal.ClassList);
    }

    [Fact]
    public async Task Open_ShowsModal_And_RendersPersonSnapshot()
    {
        // Arrange
        var pa = MakePlanAction(move: MoveType.Dispatch, state: ActionState.PlanAction);
        var cut = _ctx.RenderComponent<ViewPlanActionModal>();

        // Act (на Dispatcher)
        await cut.InvokeAsync(() => cut.Instance.Open(pa));

        // Assert
        cut.WaitForAssertion(() =>
        {
            var modal = cut.Find("div.modal");
            Assert.Contains("show", modal.ClassList);
            Assert.Contains("d-block", modal.ClassList);

            // лівий (снапшот): перевіримо кілька ключових полів
            Assert.Contains(pa.Rnokpp, cut.Markup);
            Assert.Contains(pa.FullName, cut.Markup);
            Assert.Contains(pa.PositionName, cut.Markup);
            Assert.Contains(pa.StatusKindOnDate, cut.Markup);
        });
    }

    [Fact]
    public async Task Renders_Badges_By_MoveType_And_ActionState()
    {
        // Arrange: Dispatch + PlanAction → «Відрядити» (зелений), «Рапорт» (secondary)
        var pa = MakePlanAction(move: MoveType.Dispatch, state: ActionState.PlanAction);
        var cut = _ctx.RenderComponent<ViewPlanActionModal>();

        // Act
        await cut.InvokeAsync(() => cut.Instance.Open(pa));

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("badge bg-success", cut.Markup);       // Відрядити
            Assert.Contains(">Відрядити<", cut.Markup);

            Assert.Contains("badge bg-secondary", cut.Markup);     // Рапорт
            Assert.Contains(">Рапорт<", cut.Markup);

            // Цільовий статус для Dispatch — «В БР»
            Assert.Contains("value=\"В БР\"", cut.Markup);
        });

        // Arrange 2: Return + ApprovedOrder → «Повернути» (жовтий), «Бойове розпорядження» (primary)
        var pa2 = MakePlanAction(move: MoveType.Return, state: ActionState.ApprovedOrder);
        cut = _ctx.RenderComponent<ViewPlanActionModal>();
        await cut.InvokeAsync(() => cut.Instance.Open(pa2));

        // Assert 2
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("badge bg-warning", cut.Markup);       // Повернути
            Assert.Contains(">Повернути<", cut.Markup);

            Assert.Contains("badge bg-primary", cut.Markup);       // Бойове розпорядження
            Assert.Contains(">Бойове розпорядження<", cut.Markup);

            // Цільовий статус для Return — «В районі»
            Assert.Contains("value=\"В районі\"", cut.Markup);
        });
    }

    [Fact]
    public async Task Shows_All_Main_Action_Fields_As_ReadOnly()
    {
        // Arrange
        var pa = MakePlanAction();
        var cut = _ctx.RenderComponent<ViewPlanActionModal>();

        // Act
        await cut.InvokeAsync(() => cut.Instance.Open(pa));

        // Assert: ключові поля правої колонки
        cut.WaitForAssertion(() =>
        {
            Assert.Contains(pa.PlanActionName, cut.Markup);                     // Номер рапорта
            Assert.Contains(pa.Order is null ? "-" : pa.Order, cut.Markup);     // Номер наказу
            Assert.Contains(pa.Location, cut.Markup);
            Assert.Contains(pa.GroupName, cut.Markup);
            Assert.Contains(pa.CrewName, cut.Markup);
            Assert.Contains(pa.Note, cut.Markup);

            // формат дати "dd.MM.yyyy HH:mm"
            Assert.Contains(pa.EffectiveAtUtc.ToString("dd.MM.yyyy HH:mm"), cut.Markup);
        });
    }

    [Fact]
    public async Task Close_HidesModal()
    {
        // Arrange
        var pa = MakePlanAction();
        var cut = _ctx.RenderComponent<ViewPlanActionModal>();

        await cut.InvokeAsync(() => cut.Instance.Open(pa));
        cut.WaitForAssertion(() =>
        {
            var modal = cut.Find("div.modal");
            Assert.Contains("show", modal.ClassList);
        });

        // Act
        await cut.InvokeAsync(() =>
        {
            // натискаємо кнопку «Закрити» у футері
            cut.Find("button.btn.btn-secondary.btn-sm").Click();
        });

        // Assert
        cut.WaitForAssertion(() =>
        {
            var modal = cut.Find("div.modal");
            Assert.DoesNotContain("show", modal.ClassList);
            Assert.DoesNotContain("d-block", modal.ClassList);
        });
    }
}
