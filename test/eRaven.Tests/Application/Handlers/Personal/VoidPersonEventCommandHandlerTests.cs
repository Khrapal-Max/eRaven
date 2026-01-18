//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// VoidPersonEventCommandHandlerTests
//-----------------------------------------------------------------------------

using eRaven.Application.Commands.PersonMove;
using eRaven.Application.Handlers.Personal;
using eRaven.Infrastructure.Repositories.PersonRepository;
using Moq;

namespace eRaven.Tests.Application.Handlers.Personal;

public sealed class VoidPersonEventCommandHandlerTests
{
    [Fact]
    public async Task CallsRepo_WithSameCommand()
    {
        var repo = new Mock<IPersonRepository>(MockBehavior.Strict);

        repo.Setup(x => x.VoidEventAsync(It.IsAny<VoidPersonEventCommand>(), default))
            .Returns(Task.CompletedTask);

        var h = new VoidPersonEventCommandHandler(repo.Object);

        var personId = Guid.NewGuid();
        var targetEventId = Guid.NewGuid();

        var cmd = new VoidPersonEventCommand(
            PersonId: personId,
            TargetEventId: targetEventId,
            Reason: " fix ",
            Author: "system",
            NowUtc: DateTime.UtcNow
        );

        await h.HandleAsync(cmd);

        repo.Verify(x => x.VoidEventAsync(It.Is<VoidPersonEventCommand>(c =>
            c.PersonId == personId &&
            c.TargetEventId == targetEventId &&
            c.Reason == " fix "
        ), default), Times.Once);
    }
}
