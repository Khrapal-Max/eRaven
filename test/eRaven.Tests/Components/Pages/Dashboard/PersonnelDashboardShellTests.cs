//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonnelDashboardShellTests
//-----------------------------------------------------------------------------

using Bunit;
using eRaven.Application.DTOs.Dashboard;
using eRaven.Application.Queries;
using eRaven.Application.Queries.Dashboard;
using eRaven.Components.Pages.Dashboard;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace eRaven.Tests.Components.Pages.Dashboard;

public sealed class PersonnelDashboardShellTests : BunitContext
{
    [Fact]
    public void Renders_loading_state_initially()
    {
        // Arrange
        // зробимо handler який "висить", щоб компонент залишився в _loading = true
        var tcs = new TaskCompletionSource<PersonnelDashboardDto>();

        var q = new Mock<IQueryHandler<GetPersonnelDashboardQuery, PersonnelDashboardDto>>(MockBehavior.Strict);
        q.Setup(x => x.HandleAsync(It.IsAny<GetPersonnelDashboardQuery>(), It.IsAny<CancellationToken>()))
            .Returns(tcs.Task);

        Services.AddSingleton(q.Object);

        // Act
        var cut = Render<PersonnelDashboardShell>();

        // Assert
        Assert.Contains("Завантаження...", cut.Markup);
    }

    [Fact]
    public void Renders_four_cards_with_expected_hints_counts_and_badge_classes()
    {
        // Arrange
        var dto = new PersonnelDashboardDto(
            TotalInTimesheet: 100,
            TimesheetUnit: 60,
            TimesheetOrder: 25,
            TimesheetBr: 15,
            GeneratedAtUtc: DateTime.UtcNow
        );

        var q = new Mock<IQueryHandler<GetPersonnelDashboardQuery, PersonnelDashboardDto>>(MockBehavior.Strict);
        q.Setup(x => x.HandleAsync(It.IsAny<GetPersonnelDashboardQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        Services.AddSingleton(q.Object);

        // Act
        var cut = Render<PersonnelDashboardShell>();
        cut.WaitForState(() => cut.FindAll("div.card").Count >= 4);

        // Assert: заголовок
        Assert.Contains("Доска обліку.", cut.Markup);

        // Assert: 4 StatusSliceCard
        var cards = cut.FindComponents<StatusSliceCard>();
        Assert.Equal(4, cards.Count);

        // Перевіряємо hints/counts/classes в тому порядку, як у razor
        AssertCard(cards[0], hint: "В табелі (всього)", count: 100, badgeClass: "text-bg-success");
        AssertCard(cards[1], hint: "В табелі: штат", count: 60, badgeClass: "text-bg-success");
        AssertCard(cards[2], hint: "В табелі: по наказу", count: 25, badgeClass: "text-bg-warning");
        AssertCard(cards[3], hint: "В табелі: по БР", count: 15, badgeClass: "text-bg-warning");

        q.Verify(x => x.HandleAsync(It.IsAny<GetPersonnelDashboardQuery>(), It.IsAny<CancellationToken>()), Times.Once);
        q.VerifyNoOtherCalls();
    }

    [Fact]
    public void Renders_error_alert_when_query_throws()
    {
        // Arrange
        var q = new Mock<IQueryHandler<GetPersonnelDashboardQuery, PersonnelDashboardDto>>(MockBehavior.Strict);
        q.Setup(x => x.HandleAsync(It.IsAny<GetPersonnelDashboardQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        Services.AddSingleton(q.Object);

        // Act
        var cut = Render<PersonnelDashboardShell>();
        cut.WaitForAssertion(() =>
        {
            var alert = cut.Find("div.alert.alert-danger");
            Assert.Contains("boom", alert.TextContent);
        });
    }

    private static void AssertCard(IRenderedComponent<StatusSliceCard> card, string hint, int count, string badgeClass)
    {
        Assert.Equal(hint, card.Instance.Hint);
        Assert.Equal(count, card.Instance.Count);
        Assert.Equal(badgeClass, card.Instance.BadgeClass);
    }
}
