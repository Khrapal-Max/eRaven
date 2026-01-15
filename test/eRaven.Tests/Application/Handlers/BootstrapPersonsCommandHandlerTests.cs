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
using Microsoft.Extensions.Logging;
using Moq;

namespace eRaven.Tests.Application.Handlers;

public sealed class BootstrapPersonsCommandHandlerTests
{
    private static readonly DateTime NowUtc = new(2026, 01, 07, 12, 0, 0, DateTimeKind.Utc);

    private static PersonBootstrapRowDto Row(
        int rowNumber,
        string rnokpp,
        EnrollmentKind kind = EnrollmentKind.Unit)
        => new(
            RowNumber: rowNumber,
            Rnokpp: rnokpp,
            LastName: "Ivanov",
            FirstName: "Ivan",
            MiddleName: null,
            Kind: kind,
            Reference: "REF",
            EnrollDate: new DateOnly(2026, 01, 10),
            Reason: "r",
            Rank: "Солдат",
            PositionSort: 10,
            Position: "Стрілець",
            Bzvp: null,
            Weapon: null,
            Callsign: null);

    [Fact]
    public async Task HandleAsync_when_rows_empty_should_return_zero_and_not_call_repo()
    {
        // arrange
        var repo = new Mock<IPersonRepository>(MockBehavior.Strict);
        var log = new Mock<ILogger<BootstrapPersonsCommandHandler>>(MockBehavior.Loose);
        var sut = new BootstrapPersonsCommandHandler(repo.Object, log.Object);

        var cmd = new BootstrapPersonsCommand(
            Rows: [],
            Author: "tester",
            NowUtc: NowUtc);

        // act
        var result = await sut.HandleAsync(cmd);

        // assert
        Assert.Equal(0, result.TotalRows);
        Assert.Equal(0, result.CreatedCount);
        Assert.Equal(0, result.SkippedCount);
        Assert.Empty(result.Errors);

        repo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_when_author_blank_should_throw_argument_exception()
    {
        // arrange
        var repo = new Mock<IPersonRepository>(MockBehavior.Strict);
        var log = new Mock<ILogger<BootstrapPersonsCommandHandler>>(MockBehavior.Loose);
        var sut = new BootstrapPersonsCommandHandler(repo.Object, log.Object);

        var cmd = new BootstrapPersonsCommand(
            Rows: [Row(1, "123")],
            Author: "   ",
            NowUtc: NowUtc);

        // act
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => sut.HandleAsync(cmd));

        // assert
        Assert.Contains("Author is required", ex.Message);
        repo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_should_skip_duplicates_in_file_and_report_error_for_each_duplicate_row()
    {
        // arrange
        var repo = new Mock<IPersonRepository>(MockBehavior.Strict);
        var log = new Mock<ILogger<BootstrapPersonsCommandHandler>>(MockBehavior.Loose);
        var sut = new BootstrapPersonsCommandHandler(repo.Object, log.Object);

        // two rows with same rnokpp after trim => both skipped as duplicates
        var r1 = Row(1, " 1234567890 ");
        var r2 = Row(2, "1234567890");

        // candidates list will be empty => handler still calls GetExistingRnokppsAsync with empty array
        repo.Setup(x => x.GetExistingRnokppsAsync(
                It.Is<IReadOnlyCollection<string>>(s => s.Count == 0),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<string>(StringComparer.Ordinal));

        var cmd = new BootstrapPersonsCommand(
            Rows: [r1, r2],
            Author: " tester ",
            NowUtc: NowUtc);

        // act
        var result = await sut.HandleAsync(cmd);

        // assert
        Assert.Equal(2, result.TotalRows);
        Assert.Equal(0, result.CreatedCount);
        Assert.Equal(2, result.SkippedCount);
        Assert.Equal(2, result.Errors.Count);
        Assert.All(result.Errors, e => Assert.Equal("Дублікат РНОКПП у файлі.", e.Message));

        repo.Verify(x => x.GetExistingRnokppsAsync(
            It.Is<IReadOnlyCollection<string>>(s => s.Count == 0),
            It.IsAny<CancellationToken>()), Times.Once);

        repo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_should_skip_rows_that_already_exist_in_system()
    {
        // arrange
        var repo = new Mock<IPersonRepository>(MockBehavior.Strict);
        var log = new Mock<ILogger<BootstrapPersonsCommandHandler>>(MockBehavior.Loose);
        var sut = new BootstrapPersonsCommandHandler(repo.Object, log.Object);

        var r1 = Row(1, " 1234567890 ");

        repo.Setup(x => x.GetExistingRnokppsAsync(
                It.Is<IReadOnlyCollection<string>>(s => s.Count == 1 && s.Contains("1234567890")),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<string>(StringComparer.Ordinal) { "1234567890" });

        var cmd = new BootstrapPersonsCommand(
            Rows: [r1],
            Author: "tester",
            NowUtc: NowUtc);

        // act
        var result = await sut.HandleAsync(cmd);

        // assert
        Assert.Equal(1, result.TotalRows);
        Assert.Equal(0, result.CreatedCount);
        Assert.Equal(1, result.SkippedCount);
        Assert.Single(result.Errors);
        Assert.Equal("РНОКПП вже існує в системі.", result.Errors[0].Message);

        repo.Verify(x => x.GetExistingRnokppsAsync(
            It.Is<IReadOnlyCollection<string>>(s => s.Count == 1 && s.Contains("1234567890")),
            It.IsAny<CancellationToken>()), Times.Once);

        repo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_should_create_rows_and_collect_db_and_generic_errors()
    {
        // arrange
        var repo = new Mock<IPersonRepository>(MockBehavior.Strict);
        var log = new Mock<ILogger<BootstrapPersonsCommandHandler>>(MockBehavior.Loose);
        var sut = new BootstrapPersonsCommandHandler(repo.Object, log.Object);

        var ok = Row(1, "111");
        var dbFail = Row(2, "222");
        var fail = Row(3, "333");

        repo.Setup(x => x.GetExistingRnokppsAsync(
                It.Is<IReadOnlyCollection<string>>(s =>
                    s.Count == 3 && s.Contains("111") && s.Contains("222") && s.Contains("333")),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<string>(StringComparer.Ordinal));

        // success
        repo.Setup(x => x.BootstrapCreateAndEnrollAsync(ok, "tester", NowUtc, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid());

        // DbUpdateException
        repo.Setup(x => x.BootstrapCreateAndEnrollAsync(dbFail, "tester", NowUtc, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateException("DB boom"));

        // generic exception
        repo.Setup(x => x.BootstrapCreateAndEnrollAsync(fail, "tester", NowUtc, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("bad row"));

        var cmd = new BootstrapPersonsCommand(
            Rows: [ok, dbFail, fail],
            Author: "  tester ", // trimmed
            NowUtc: NowUtc);

        // act
        var result = await sut.HandleAsync(cmd);

        // assert
        Assert.Equal(3, result.TotalRows);
        Assert.Equal(1, result.CreatedCount);
        Assert.Equal(2, result.SkippedCount);
        Assert.Equal(2, result.Errors.Count);

        Assert.Contains(result.Errors, e =>
            e.RowNumber == 2 &&
            e.Rnokpp == "222" &&
            e.Message.StartsWith("Помилка БД:", StringComparison.Ordinal));

        Assert.Contains(result.Errors, e =>
            e.RowNumber == 3 &&
            e.Rnokpp == "333" &&
            e.Message == "bad row");

        repo.Verify(x => x.GetExistingRnokppsAsync(
            It.Is<IReadOnlyCollection<string>>(s =>
                s.Count == 3 && s.Contains("111") && s.Contains("222") && s.Contains("333")),
            It.IsAny<CancellationToken>()), Times.Once);

        repo.Verify(x => x.BootstrapCreateAndEnrollAsync(ok, "tester", NowUtc, It.IsAny<CancellationToken>()), Times.Once);
        repo.Verify(x => x.BootstrapCreateAndEnrollAsync(dbFail, "tester", NowUtc, It.IsAny<CancellationToken>()), Times.Once);
        repo.Verify(x => x.BootstrapCreateAndEnrollAsync(fail, "tester", NowUtc, It.IsAny<CancellationToken>()), Times.Once);

        repo.VerifyNoOtherCalls();

        // optional: verify we logged warnings for the two failures
        VerifyWarningLogCalled(log, Times.Exactly(2));
    }

    private static void VerifyWarningLogCalled(
        Mock<ILogger<BootstrapPersonsCommandHandler>> log,
        Times times)
    {
        log.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) =>
                    v.ToString()!.Contains("Bootstrap import", StringComparison.OrdinalIgnoreCase)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            times);
    }
}
