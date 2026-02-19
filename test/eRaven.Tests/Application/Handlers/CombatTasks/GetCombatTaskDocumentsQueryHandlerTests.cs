//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// GetCombatTaskDocumentsQueryHandlerTests
//-----------------------------------------------------------------------------

using eRaven.Application.Abstractions.CombatTaskRepository;
using eRaven.Application.Handlers.CombatTasks;
using eRaven.Application.Queries.CombatTasks;
using eRaven.Domain.Entities;
using eRaven.Domain.Enums;
using Moq;

namespace eRaven.Tests.Application.Handlers.CombatTasks;

public sealed class GetCombatTaskDocumentsQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_PassesNullSearch_WhenBlank()
    {
        // Arrange
        var repo = new Mock<ICombatTaskDocumentRepository>(MockBehavior.Strict);

        repo.Setup(x => x.GetDocumentsAsync(
                year: 0,
                month: 0,
                status: null,
                search: null,
                ct: It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var handler = new GetCombatTaskDocumentsQueryHandler(repo.Object);

        var query = new GetCombatTaskDocumentsQuery(
            Year: 0,
            Month: 0,
            Status: null,
            Search: "   ");

        // Act
        var res = await handler.HandleAsync(query, CancellationToken.None);

        // Assert
        Assert.NotNull(res);
        Assert.Empty(res);

        repo.Verify(x => x.GetDocumentsAsync(
                year: 0,
                month: 0,
                status: null,
                search: null,
                ct: It.IsAny<CancellationToken>()),
            Times.Once);

        repo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_TrimsSearch_PassesFilters_AndMapsToDto()
    {
        // Arrange
        var repo = new Mock<ICombatTaskDocumentRepository>(MockBehavior.Strict);

        var year = 2026;
        var month = 2;
        var status = DocumentStatus.Active;
        var search = "  alpha  ";
        var trimmed = "alpha";

        var doc1 = new CombatTaskDocument
        {
            Id = Guid.NewGuid(),
            OrderTitle = "A-1",
            Description = "Desc",
            Status = DocumentStatus.Active,
            RecordedAt = new DateOnly(2026, 02, 10),
            CanceledReason = null
        };

        var doc2 = new CombatTaskDocument
        {
            Id = Guid.NewGuid(),
            OrderTitle = "A-2",
            Description = null,
            Status = DocumentStatus.Canceled,
            RecordedAt = new DateOnly(2026, 02, 11),
            CanceledReason = "  reason  "
        };

        repo.Setup(x => x.GetDocumentsAsync(
                year: year,
                month: month,
                status: status,
                search: trimmed,
                ct: It.IsAny<CancellationToken>()))
            .ReturnsAsync([doc1, doc2]);

        var handler = new GetCombatTaskDocumentsQueryHandler(repo.Object);

        var query = new GetCombatTaskDocumentsQuery(
            Year: year,
            Month: month,
            Status: status,
            Search: search);

        // Act
        var res = await handler.HandleAsync(query, CancellationToken.None);

        // Assert
        Assert.Equal(2, res.Count);

        var r1 = res.Single(x => x.DocumentId == doc1.Id);
        Assert.Equal(doc1.OrderTitle, r1.OrderTitle);
        Assert.Equal(doc1.Description, r1.Description);
        Assert.Equal(doc1.Status, r1.Status);
        Assert.Equal(doc1.RecordedAt, r1.RecordedAt);
        Assert.Equal(string.Empty, r1.CanceledReason);

        var r2 = res.Single(x => x.DocumentId == doc2.Id);
        Assert.Equal(doc2.OrderTitle, r2.OrderTitle);
        Assert.Null(r2.Description);
        Assert.Equal(doc2.Status, r2.Status);
        Assert.Equal(doc2.RecordedAt, r2.RecordedAt);
        Assert.Equal("  reason  ", r2.CanceledReason); // handler не trim'ить reason

        repo.Verify(x => x.GetDocumentsAsync(year, month, status, trimmed, It.IsAny<CancellationToken>()), Times.Once);
        repo.VerifyNoOtherCalls();
    }
}
