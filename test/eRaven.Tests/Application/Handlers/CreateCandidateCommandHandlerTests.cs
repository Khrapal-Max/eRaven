//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CreateCandidateCommandHandlerTests
//-----------------------------------------------------------------------------

using eRaven.Application.Commands;
using eRaven.Application.Handlers;
using eRaven.Domain.Aggregates;
using eRaven.Domain.Events.PersonEvents;
using eRaven.Infrastructure.Repositories.PersonRepository;
using Moq;

namespace eRaven.Tests.Application.Handlers;

public sealed class CreateCandidateCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_should_create_candidate_aggregate_and_call_repo_save_with_expectedVersion_0()
    {
        // arrange
        var repo = new Mock<IPersonRepository>(MockBehavior.Strict);

        PersonAggregate? savedAgg = null;
        long savedExpectedVersion = -1;
        CancellationToken savedCt = default;

        repo.Setup(r => r.SaveAsync(
                It.IsAny<PersonAggregate>(),
                It.IsAny<long>(),
                It.IsAny<CancellationToken>()))
            .Callback<PersonAggregate, long, CancellationToken>((agg, expected, ct) =>
            {
                savedAgg = agg;
                savedExpectedVersion = expected;
                savedCt = ct;
            })
            .Returns(Task.CompletedTask);

        var sut = new CreateCandidateCommandHandler(repo.Object);

        var plannedUnitId = Guid.NewGuid();

        var cmd = new CreatePersonCandidateCommand(
            Rnokpp: " 1234567890 ",
            LastName: "  Ivanov ",
            FirstName: " Ivan ",
            MiddleName: "  Ivanovich ",
            PlannedPosition: "  Operator  ",
            PlannedPositionUnitId: plannedUnitId
        );

        using var cts = new CancellationTokenSource();
        var ct = cts.Token;

        // act
        var returnedId = await sut.HandleAsync(cmd, ct);

        // assert: repo called
        repo.Verify(r => r.SaveAsync(It.IsAny<PersonAggregate>(), 0, ct), Times.Once);

        Assert.NotNull(savedAgg);
        Assert.Equal(0, savedExpectedVersion);
        Assert.Equal(ct, savedCt);

        // assert: returned id == aggregate id
        Assert.Equal(returnedId, savedAgg!.Id);

        // assert: aggregate state
        Assert.NotNull(savedAgg.Personal);
        Assert.Equal("1234567890", savedAgg.Personal!.Rnokpp);
        Assert.Equal("Ivanov", savedAgg.Personal.LastName);
        Assert.Equal("Ivan", savedAgg.Personal.FirstName);
        Assert.Equal("Ivanovich", savedAgg.Personal.MiddleName);

        Assert.Equal("Operator", savedAgg.PlannedPosition);
        Assert.Equal(plannedUnitId, savedAgg.PlannedPositionUnitId);

        // assert: produced event
        var change = Assert.Single(savedAgg.GetUncommittedChanges());
        var created = Assert.IsType<PersonCandidateCreated>(change);

        Assert.Equal(savedAgg.Id, created.AggregateId);
        Assert.Equal("author", created.Author);
        Assert.Equal("Operator", created.PlannedPosition);
        Assert.Equal(plannedUnitId, created.PlannedPositionUnitId);
        Assert.NotEqual(default, created.OccurredAtUtc);
    }
}