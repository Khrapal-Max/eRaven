//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CreateCombatTaskDocumentCommandHandlerTests
//-----------------------------------------------------------------------------

using eRaven.Application.Abstractions.CombatTaskRepository;
using eRaven.Application.Commands.CombatTasks;
using eRaven.Application.Handlers.CombatTasks;
using eRaven.Domain.Entities;
using eRaven.Domain.Enums;

namespace eRaven.Tests.Application.Handlers.CombatTasks;

public sealed class CreateCombatTaskDocumentCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_ValidCommand_TrimsAndNormalizes_AndPassesArgsToRepository()
    {
        // Arrange
        var expectedId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var repo = new FakeRepo(expectedId);
        var sut = new CreateCombatTaskDocumentCommandHandler(repo);

        using var cts = new CancellationTokenSource();
        var nowUtc = new DateTime(2026, 3, 3, 8, 10, 0, DateTimeKind.Utc);

        var command = new CreateCombatTaskDocumentCommand(
            OrderTitle: "  Наказ №1  ",
            RecordedAt: new DateOnly(2026, 3, 3),
            Description: "  Опис  ",
            Author: "  Автор  ",
            NowUtc: nowUtc);

        // Act
        var id = await sut.HandleAsync(command, cts.Token);

        // Assert
        Assert.Equal(expectedId, id);

        Assert.Equal(1, repo.CallCount);
        Assert.Equal("Наказ №1", repo.OrderTitle);
        Assert.Equal(new DateOnly(2026, 3, 3), repo.RecordedAt);
        Assert.Equal("Опис", repo.Description);
        Assert.Equal("Автор", repo.Author);
        Assert.Equal(nowUtc, repo.NowUtc);
        Assert.Equal(cts.Token, repo.Token);
    }

    [Fact]
    public async Task HandleAsync_DescriptionWhitespace_PassesNullToRepository()
    {
        // Arrange
        var repo = new FakeRepo(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));
        var sut = new CreateCombatTaskDocumentCommandHandler(repo);

        var command = new CreateCombatTaskDocumentCommand(
            OrderTitle: "Наказ №2",
            RecordedAt: new DateOnly(2026, 3, 3),
            Description: "   ",
            Author: "Автор",
            NowUtc: new DateTime(2026, 3, 3, 9, 0, 0, DateTimeKind.Utc));

        // Act
        _ = await sut.HandleAsync(command);

        // Assert
        Assert.Equal(1, repo.CallCount);
        Assert.Null(repo.Description);
    }

    [Fact]
    public async Task HandleAsync_OrderTitleWhitespace_Throws()
    {
        // Arrange
        var repo = new FakeRepo(Guid.NewGuid());
        var sut = new CreateCombatTaskDocumentCommandHandler(repo);

        var command = new CreateCombatTaskDocumentCommand(
            OrderTitle: "   ",
            RecordedAt: new DateOnly(2026, 3, 3),
            Description: null,
            Author: "Автор",
            NowUtc: new DateTime(2026, 3, 3, 9, 0, 0, DateTimeKind.Utc));

        // Act
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => sut.HandleAsync(command));

        // Assert
        Assert.Contains("OrderTitle", ex.Message);
        Assert.Equal(0, repo.CallCount);
    }

    [Fact]
    public async Task HandleAsync_AuthorWhitespace_Throws()
    {
        // Arrange
        var repo = new FakeRepo(Guid.NewGuid());
        var sut = new CreateCombatTaskDocumentCommandHandler(repo);

        var command = new CreateCombatTaskDocumentCommand(
            OrderTitle: "Наказ",
            RecordedAt: new DateOnly(2026, 3, 3),
            Description: null,
            Author: "  ",
            NowUtc: new DateTime(2026, 3, 3, 9, 0, 0, DateTimeKind.Utc));

        // Act
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => sut.HandleAsync(command));

        // Assert
        Assert.Contains("Author", ex.Message);
        Assert.Equal(0, repo.CallCount);
    }

    [Fact]
    public async Task HandleAsync_RecordedAtDefault_Throws()
    {
        // Arrange
        var repo = new FakeRepo(Guid.NewGuid());
        var sut = new CreateCombatTaskDocumentCommandHandler(repo);

        var command = new CreateCombatTaskDocumentCommand(
            OrderTitle: "Наказ",
            RecordedAt: default,
            Description: null,
            Author: "Автор",
            NowUtc: new DateTime(2026, 3, 3, 9, 0, 0, DateTimeKind.Utc));

        // Act
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => sut.HandleAsync(command));

        // Assert
        Assert.Contains("RecordedAt", ex.Message);
        Assert.Equal(0, repo.CallCount);
    }

    [Fact]
    public async Task HandleAsync_NowUtcNotUtc_Throws()
    {
        // Arrange
        var repo = new FakeRepo(Guid.NewGuid());
        var sut = new CreateCombatTaskDocumentCommandHandler(repo);

        var notUtc = DateTime.SpecifyKind(new DateTime(2026, 3, 3, 9, 0, 0), DateTimeKind.Local);

        var command = new CreateCombatTaskDocumentCommand(
            OrderTitle: "Наказ",
            RecordedAt: new DateOnly(2026, 3, 3),
            Description: null,
            Author: "Автор",
            NowUtc: notUtc);

        // Act
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => sut.HandleAsync(command));

        // Assert
        Assert.Contains("NowUtc", ex.Message);
        Assert.Equal(0, repo.CallCount);
    }

    private sealed class FakeRepo(Guid idToReturn) : ICombatTaskDocumentRepository
    {
        public int CallCount { get; private set; }

        public string? OrderTitle { get; private set; }
        public DateOnly RecordedAt { get; private set; }
        public string? Description { get; private set; }
        public string? Author { get; private set; }
        public DateTime NowUtc { get; private set; }
        public CancellationToken Token { get; private set; }

        public Task<Guid> CreateAsync(
            string orderTitle,
            DateOnly recordedAt,
            string? description,
            string author,
            DateTime nowUtc,
            CancellationToken ct = default)
        {
            CallCount++;
            OrderTitle = orderTitle;
            RecordedAt = recordedAt;
            Description = description;
            Author = author;
            NowUtc = nowUtc;
            Token = ct;
            return Task.FromResult(idToReturn);
        }

        public Task<IReadOnlyList<CombatTaskDocument>> GetDocumentsAsync(int? year, int? month, DocumentStatus? status, string? search, CancellationToken ct = default)
            => throw new NotSupportedException();

        public Task CancelAsync(Guid documentId, string? reason, string author, DateTime nowUtc, CancellationToken ct = default)
            => throw new NotSupportedException();
    }
}
