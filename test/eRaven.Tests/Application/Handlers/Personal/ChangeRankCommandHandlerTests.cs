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
        var personId = Guid.NewGuid();

        var cmd = new ChangeRankCommand(
            PersonId: personId,
            EffectiveDate: new DateOnly(2026, 1, 10),
            Rank: "Солдат",
            Note: "n",
            Author: "system",
            NowUtc: DateTime.UtcNow
        );

        var repo = new Mock<IPersonRepository>(MockBehavior.Strict);

        repo.Setup(x => x.ChangeRankAsync(cmd.PersonId,
            cmd.EffectiveDate,
            cmd.Rank,
            cmd.Note,
            cmd.Author,
            cmd.NowUtc,
            default))
            .Returns(Task.CompletedTask);

        var h = new ChangeRankCommandHandler(repo.Object);

        await h.HandleAsync(cmd);

        repo.Verify(x => x.ChangeRankAsync(
            cmd.PersonId,
            cmd.EffectiveDate,
            cmd.Rank,
            cmd.Note,
            cmd.Author,
            cmd.NowUtc,
            default), Times.Once);
    }
}