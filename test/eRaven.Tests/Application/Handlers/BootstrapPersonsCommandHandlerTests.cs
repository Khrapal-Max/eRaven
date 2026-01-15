//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// BootstrapPersonsCommandHandlerTests
//-----------------------------------------------------------------------------

using eRaven.Application.Commands.Excel;
using eRaven.Application.DTOs.Excel;
using eRaven.Application.Handlers;
using eRaven.Domain.Enums;
using eRaven.Infrastructure.Repositories.PersonRepository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace eRaven.Tests.Application.Handlers;

public class BootstrapPersonsCommandHandlerTests
{
    [Fact]
    public async Task ReturnsEmpty_WhenNoRows()
    {
        var repo = new Mock<IPersonRepository>(MockBehavior.Strict);

        var h = new BootstrapPersonsCommandHandler(repo.Object, NullLogger<BootstrapPersonsCommandHandler>.Instance);

        var res = await h.HandleAsync(new BootstrapPersonsCommand(
            Rows: [],
            Author: "system",
            NowUtc: DateTime.UtcNow
        ));

        Assert.Equal(0, res.TotalRows);
        Assert.Equal(0, res.CreatedCount);
        Assert.Equal(0, res.SkippedCount);
        Assert.Empty(res.Errors);
    }

    [Fact]
    public async Task DetectsDuplicateRnokpp_InFile_AndDoesNotCallRepo()
    {
        var repo = new Mock<IPersonRepository>(MockBehavior.Strict);

        var h = new BootstrapPersonsCommandHandler(repo.Object, NullLogger<BootstrapPersonsCommandHandler>.Instance);

        var rows = new[]
        {
            new PersonBootstrapRowDto(2,"1234567890","A","B",null,EnrollmentKind.Unit,null,new DateOnly(2026,1,1),"r","rk",1,"p",null,null,null),
            new PersonBootstrapRowDto(3,"1234567890","C","D",null,EnrollmentKind.Unit,null,new DateOnly(2026,1,1),"r","rk",1,"p",null,null,null),
        };

        var res = await h.HandleAsync(new BootstrapPersonsCommand(rows, "system", DateTime.UtcNow));

        Assert.Equal(2, res.TotalRows);
        Assert.True(res.Errors.Count > 0);
        Assert.Contains(res.Errors, e => e.Message.Contains("дублікати", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task DetectsExistingRnokpp_InDb_AndSkipsAll()
    {
        var repo = new Mock<IPersonRepository>(MockBehavior.Strict);

        repo.Setup(x => x.GetExistingRnokppsAsync(It.IsAny<IReadOnlyCollection<string>>(), default))
            .ReturnsAsync(new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "1234567890" });

        var h = new BootstrapPersonsCommandHandler(repo.Object, NullLogger<BootstrapPersonsCommandHandler>.Instance);

        var rows = new[]
        {
            new PersonBootstrapRowDto(2,"1234567890","A","B",null,EnrollmentKind.Unit,null,new DateOnly(2026,1,1),"r","rk",1,"p",null,null,null),
        };

        var res = await h.HandleAsync(new BootstrapPersonsCommand(rows, "system", DateTime.UtcNow));

        Assert.Equal(1, res.TotalRows);
        Assert.Equal(0, res.CreatedCount);
        Assert.Equal(0, res.SkippedCount);
        Assert.Contains(res.Errors, e => e.Message.Contains("вже існує", StringComparison.OrdinalIgnoreCase));

        repo.Verify(x => x.BootstrapCreateAndEnrollAsync(It.IsAny<PersonBootstrapRowDto>(), It.IsAny<string>(), It.IsAny<DateTime>(), default), Times.Never);
    }

    [Fact]
    public async Task WhenRepoThrows_DbUpdateException_AddsError_AndSkips()
    {
        var repo = new Mock<IPersonRepository>(MockBehavior.Strict);

        repo.Setup(x => x.GetExistingRnokppsAsync(It.IsAny<IReadOnlyCollection<string>>(), default))
            .ReturnsAsync(new HashSet<string>(StringComparer.OrdinalIgnoreCase));

        repo.Setup(x => x.BootstrapCreateAndEnrollAsync(It.IsAny<PersonBootstrapRowDto>(), It.IsAny<string>(), It.IsAny<DateTime>(), default))
            .ThrowsAsync(new DbUpdateException("boom"));

        var h = new BootstrapPersonsCommandHandler(repo.Object, NullLogger<BootstrapPersonsCommandHandler>.Instance);

        var rows = new[]
        {
            new PersonBootstrapRowDto(2,"1234567890","A","B",null,EnrollmentKind.Unit,null,new DateOnly(2026,1,1),"r","rk",1,"p",null,null,null),
        };

        var res = await h.HandleAsync(new BootstrapPersonsCommand(rows, "system", DateTime.UtcNow));

        Assert.Equal(1, res.TotalRows);
        Assert.Equal(0, res.CreatedCount);
        Assert.Equal(0, res.SkippedCount);
        Assert.Contains(res.Errors, e => e.Message.Contains("DB", StringComparison.OrdinalIgnoreCase) || e.Message.Contains("помилка", StringComparison.OrdinalIgnoreCase));
    }
}
