//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ChangeRankCommandHandlerTests
//-----------------------------------------------------------------------------

using eRaven.Application.Commands.PersonInfo;
using eRaven.Application.Handlers.Personal;
using eRaven.Infrastructure.Repositories.PersonRepository;
using Moq;

namespace eRaven.Tests.Application.Handlers.Personal;

public sealed class ChangeRankCommandHandlerTests
{
    [Fact]
    public async Task CallsRepo_AndReturnsPersonId()
    {
        var repo = new Mock<IPersonRepository>(MockBehavior.Strict);

        repo.Setup(x => x.ChangeRankAsync(It.IsAny<ChangeRankCommand>(), default))
            .Returns(Task.CompletedTask);

        var h = new ChangeRankCommandHandler(repo.Object);

        var personId = Guid.NewGuid();

        var cmd = new ChangeRankCommand(
            PersonId: personId,
            EffectiveDate: new DateOnly(2026, 1, 10),
            Rank: "Солдат",
            Note: "n",
            Author: "system",
            NowUtc: DateTime.UtcNow
        );

        await h.HandleAsync(cmd);

        repo.Verify(x => x.ChangeRankAsync(It.Is<ChangeRankCommand>(c =>
            c.PersonId == personId &&
            c.Rank == "Солдат"
        ), default), Times.Once);
    }
}