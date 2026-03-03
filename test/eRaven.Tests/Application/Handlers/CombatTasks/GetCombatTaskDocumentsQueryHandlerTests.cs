//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// GetCombatTaskDocumentsQueryHandlerTests
//-----------------------------------------------------------------------------

using eRaven.Application.Abstractions.CombatTaskRepository;
using eRaven.Application.DTOs.Enums;
using eRaven.Application.Handlers.CombatTasks;
using eRaven.Application.Queries.CombatTasks;
using eRaven.Domain.Entities;
using eRaven.Domain.Enums;

namespace eRaven.Tests.Application.Handlers.CombatTasks;

public sealed class GetCombatTaskDocumentsQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_TrimsSearch_MapsStatusAndFields_AndPassesFilterParamsToRepository()
    {
        // Arrange
        var repo = new FakeRepo
        {
            Result =
            [
                new CombatTaskDocument
                {
                    Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    Status = DocumentStatus.Active,
                    OrderTitle = "Наказ №1",
                    Description = "desc",
                    RecordedAt = new DateOnly(2026, 3, 1),
                    CanceledReason = null,
                },
                new CombatTaskDocument
                {
                    Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    Status = DocumentStatus.Canceled,
                    OrderTitle = "Наказ №2",
                    Description = null,
                    RecordedAt = new DateOnly(2026, 3, 2),
                    CanceledReason = "Причина",
                },
            ]
        };

        var sut = new GetCombatTaskDocumentsQueryHandler(repo);

        using var cts = new CancellationTokenSource();
        var query = new GetCombatTaskDocumentsQuery(
            Year: 2026,
            Month: 3,
            Status: null,
            Search: "   test   ");

        // Act
        var result = await sut.HandleAsync(query, cts.Token);

        // Assert - repository call
        Assert.Equal(1, repo.CallCount);
        Assert.Equal(2026, repo.Year);
        Assert.Equal(3, repo.Month);
        Assert.Null(repo.Status); // important: null must stay null ("All")
        Assert.Equal("test", repo.Search);
        Assert.Equal(cts.Token, repo.Token);

        // Assert - mapping
        Assert.Equal(2, result.Count);

        var first = result[0];
        Assert.Equal(Guid.Parse("11111111-1111-1111-1111-111111111111"), first.DocumentId);
        Assert.Equal("Наказ №1", first.OrderTitle);
        Assert.Equal("desc", first.Description);
        Assert.Equal(DocumentStatusDto.Active, first.Status);
        Assert.Equal(new DateOnly(2026, 3, 1), first.RecordedAt);
        Assert.Equal(string.Empty, first.CanceledReason); // null -> empty

        var second = result[1];
        Assert.Equal(Guid.Parse("22222222-2222-2222-2222-222222222222"), second.DocumentId);
        Assert.Equal(DocumentStatusDto.Canceled, second.Status);
        Assert.Equal("Причина", second.CanceledReason);
    }

    [Fact]
    public async Task HandleAsync_SearchWhitespace_PassesNullToRepository()
    {
        // Arrange
        var repo = new FakeRepo { Result = [] };
        var sut = new GetCombatTaskDocumentsQueryHandler(repo);

        var query = new GetCombatTaskDocumentsQuery(
            Year: 2026,
            Month: 3,
            Status: DocumentStatusDto.Active,
            Search: "   ");

        // Act
        _ = await sut.HandleAsync(query);

        // Assert
        Assert.Equal(1, repo.CallCount);
        Assert.Equal(DocumentStatus.Active, repo.Status);
        Assert.Null(repo.Search);
    }

    [Fact]
    public async Task HandleAsync_SearchNull_PassesNullToRepository()
    {
        // Arrange
        var repo = new FakeRepo { Result = [] };
        var sut = new GetCombatTaskDocumentsQueryHandler(repo);

        var query = new GetCombatTaskDocumentsQuery(
            Year: 2026,
            Month: 3,
            Status: DocumentStatusDto.Canceled,
            Search: null);

        // Act
        _ = await sut.HandleAsync(query);

        // Assert
        Assert.Equal(1, repo.CallCount);
        Assert.Equal(DocumentStatus.Canceled, repo.Status);
        Assert.Null(repo.Search);
    }

    private sealed class FakeRepo : ICombatTaskDocumentRepository
    {
        public int CallCount { get; private set; }

        public int? Year { get; private set; }
        public int? Month { get; private set; }
        public DocumentStatus? Status { get; private set; }
        public string? Search { get; private set; }
        public CancellationToken Token { get; private set; }

        public IReadOnlyList<CombatTaskDocument> Result { get; init; } = [];

        public Task<IReadOnlyList<CombatTaskDocument>> GetDocumentsAsync(
            int? year,
            int? month,
            DocumentStatus? status,
            string? search,
            CancellationToken ct = default)
        {
            CallCount++;
            Year = year;
            Month = month;
            Status = status;
            Search = search;
            Token = ct;
            return Task.FromResult(Result);
        }

        public Task<Guid> CreateAsync(string orderTitle, DateOnly recordedAt, string? description, string author, DateTime nowUtc, CancellationToken ct = default)
            => throw new NotSupportedException("Not needed for handler tests");

        public Task CancelAsync(Guid documentId, string? reason, string author, DateTime nowUtc, CancellationToken ct = default)
            => throw new NotSupportedException("Not needed for handler tests");
    }
}
