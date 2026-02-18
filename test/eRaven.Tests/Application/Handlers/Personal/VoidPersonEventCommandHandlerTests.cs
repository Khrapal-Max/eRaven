//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// VoidPersonEventCommandHandlerTests
//-----------------------------------------------------------------------------

using eRaven.Application.Abstractions.PersonRepository;
using eRaven.Application.Commands.PersonMove;
using eRaven.Application.Handlers.Personal;
using Moq;

namespace eRaven.Tests.Application.Handlers.Personal;

public sealed class VoidPersonEventCommandHandlerTests
{
    [Fact]
    public async Task CallsRepo_WithSameCommand()
    {
        var personId = Guid.NewGuid();
        var targetEventId = Guid.NewGuid();

        var cmd = new VoidPersonEventCommand(
            PersonId: personId,
            TargetEventId: targetEventId,
            Reason: " fix ",
            Author: "system",
            NowUtc: DateTime.UtcNow
        );

        var repo = new Mock<IPersonRepository>(MockBehavior.Strict);

        repo.Setup(x => x.VoidEventAsync(cmd.PersonId,
            cmd.TargetEventId,
            cmd.Reason,
            cmd.Author,
            cmd.NowUtc,
            default))
            .Returns(Task.CompletedTask);

        var h = new VoidPersonEventCommandHandler(repo.Object);

        await h.HandleAsync(cmd);

        repo.Verify(x => x.VoidEventAsync(cmd.PersonId,
            cmd.TargetEventId,
            cmd.Reason,
            cmd.Author,
            cmd.NowUtc,
            default), Times.Once);
    }
}
