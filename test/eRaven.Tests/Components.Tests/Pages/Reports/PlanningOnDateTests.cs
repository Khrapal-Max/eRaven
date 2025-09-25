// -----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
// -----------------------------------------------------------------------------
// PlanningOnDateTests
// -----------------------------------------------------------------------------

using Blazored.Toast.Services;
using Bunit;
using eRaven.Application.Services.ExcelService;
using eRaven.Application.Services.PlanActionService;
using eRaven.Components.Pages.Reports;
using eRaven.Domain.Enums;
using eRaven.Domain.Models;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace eRaven.Tests.Components.Tests.Pages.Reports;

public class PlanningOnDateTests : TestContext
{
    private readonly Mock<IPlanActionService> _svcMock;
    private readonly Mock<IExcelService> _excelMock;
    private readonly Mock<IToastService> _toastMock;

    public PlanningOnDateTests()
    {
        _svcMock = new();
        _excelMock = new();
        _toastMock = new();

        Services.AddSingleton(_svcMock.Object);
        Services.AddSingleton(_excelMock.Object);
        Services.AddSingleton(_toastMock.Object);
    }


    private static IReadOnlyList<PlanAction> SampleActions()
    {
        var t = new DateTime(2025, 09, 24, 8, 30, 0, DateTimeKind.Utc);
        return
        [
            new PlanAction { Id=Guid.NewGuid(), PersonId=Guid.NewGuid(), MoveType=MoveType.Dispatch,
                ActionState=ActionState.ApprovedOrder, EffectiveAtUtc=t, Location="Alpha", GroupName="G-1", CrewName="Crew-A",
                FullName="Іван Іваненко", RankName="Сержант", Callsign="Сокіл" },
            new PlanAction { Id=Guid.NewGuid(), PersonId=Guid.NewGuid(), MoveType=MoveType.Dispatch,
                ActionState=ActionState.ApprovedOrder, EffectiveAtUtc=t.AddMinutes(10), Location="Alpha", GroupName="G-1", CrewName="Crew-B",
                FullName="Петро Петренко", RankName="Солдат", Callsign="Буря" },
        ];
    }

    [Fact]
    public void Build_Renders_Cards_Using_Mocked_Service()
    {
        // mock PlanActionService → повертає 2 записи
        _svcMock.Setup(s => s.GetActiveDispatchOnDateAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(SampleActions());

        var cut = RenderComponent<PlanningOnDate>();

        // клік “Побудувати”
        cut.FindAll("button").First(b => b.TextContent.Contains("Побудувати")).Click();

        // перевірка DOM
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Crew-A", cut.Markup);
            Assert.Contains("Crew-B", cut.Markup);
            Assert.Contains("Іван Іваненко", cut.Markup);
            Assert.Contains("Петро Петренко", cut.Markup);
        });

        // перевірка, що сервіс викликали рівно один раз
        _svcMock.Verify(s => s.GetActiveDispatchOnDateAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void Empty_Shows_EmptyMessage()
    {
        _svcMock.Setup(s => s.GetActiveDispatchOnDateAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync([]);

        var cut = RenderComponent<PlanningOnDate>();
        cut.FindAll("button").First(b => b.TextContent.Contains("Побудувати")).Click();

        cut.WaitForAssertion(() =>
            Assert.Contains("Порожньо (на цю дату немає записів планування типу «Відрядження»).", cut.Markup)
        );

        _svcMock.Verify(s => s.GetActiveDispatchOnDateAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
