//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ExportTimesheetMonthQueryHandlerTests
//-----------------------------------------------------------------------------

using ClosedXML.Excel;
using eRaven.Application.DTOs.Timesheet;
using eRaven.Application.Handlers.Timesheet;
using eRaven.Application.Queries.Timesheet;
using eRaven.Domain.Enums;
using eRaven.Infrastructure.Repositories.TimesheetRepository;
using Moq;

namespace eRaven.Tests.Application.Handlers.Timesheet;

public sealed class ExportTimesheetMonthQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_should_call_repo_trim_search_and_return_valid_xlsx()
    {
        // arrange
        var repo = new Mock<ITimesheetMonthRepository>(MockBehavior.Strict);

        var year = 2026;
        var month = 1;

        var dayCode = new string[31];
        var referenses = new string?[31];

        for (var i = 0; i < 31; i++)
        {
            dayCode[i] = "30";
            referenses[i] = null;  // no alert refs
        }

        var row = new TimesheetPersonMonthRowDto(
            PersonId: Guid.NewGuid(),
            FullName: "Іванов Іван",
            RNOKPP: "1234567890",
            Rank: "Солдат",
            Position: "Стрілець",
            EnrollmentKind: EnrollmentKind.Unit,
            EnrolledAt: new DateOnly(2026, 1, 1),
            ExcludedAt: null,
            Codes: dayCode,
            Referenses: referenses);

        IReadOnlyList<TimesheetPersonMonthRowDto> rows = [row];

        repo.Setup(x => x.GetTimesheetMonthAsync(
                year,
                month,
                "ivanov", // trimmed
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(rows);

        var sut = new ExportTimesheetMonthQueryHandler(repo.Object);

        var query = new ExportTimesheetMonthQuery(
            Year: year,
            Month: month,
            Search: "  ivanov  ");

        // act
        var file = await sut.HandleAsync(query, CancellationToken.None);

        // assert (dto)
        Assert.Equal("Timesheet_2026-01_Січ.xlsx", file.FileName);
        Assert.Equal(ExportTimesheetMonthQueryHandler.XlsxContentType, file.ContentType);
        Assert.False(string.IsNullOrWhiteSpace(file.Base64));

        // decode and verify workbook is valid
        var bytes = Convert.FromBase64String(file.Base64);
        Assert.True(bytes.Length > 0);

        using var ms = new MemoryStream(bytes);
        using var wb = new XLWorkbook(ms);

        Assert.Single(wb.Worksheets);

        var ws = wb.Worksheets.First();
        Assert.Equal("Табель Січ 2026", ws.Name);

        // Header
        Assert.Equal("Тип", ws.Cell(1, 1).GetString());
        Assert.Equal("ПІБ", ws.Cell(1, 4).GetString());
        Assert.Equal("1 Січ", ws.Cell(1, 6).GetString()); // first day column

        // First row body: EnrollmentKind.Unit => "ШТ"
        Assert.Equal("ШТ", ws.Cell(2, 1).GetString());

        // Day 1 cell must contain ONLY main code (task ignored)
        var day1 = ws.Cell(2, 6).GetString();
        Assert.Equal("30", day1);
        Assert.DoesNotContain("ПБД", day1);

        repo.Verify(x => x.GetTimesheetMonthAsync(
            year, month, "ivanov", It.IsAny<CancellationToken>()), Times.Once);

        repo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_should_put_ref_to_cell_comment_when_main_is_100()
    {
        // arrange
        var repo = new Mock<ITimesheetMonthRepository>(MockBehavior.Strict);

        var year = 2026;
        var month = 1;

        var dayCode = new string[31];
        var referenses = new string?[31];

        for (var i = 0; i < 31; i++)
        {
            dayCode[i] = "30";
            referenses[i] = null;  // no alert refs
        }

        // Day 1 => 100 with reference
        dayCode[0] = "100";
        referenses[0] = "REF-ABC";

        var row = new TimesheetPersonMonthRowDto(
            PersonId: Guid.NewGuid(),
            FullName: "Іванов Іван",
            RNOKPP: "1234567890",
            Rank: "Солдат",
            Position: "Стрілець",
            EnrollmentKind: EnrollmentKind.Unit,
            EnrolledAt: new DateOnly(2026, 1, 1),
            ExcludedAt: null,
            Codes: dayCode,
            Referenses: referenses);

        IReadOnlyList<TimesheetPersonMonthRowDto> rows = [row];

        repo.Setup(x => x.GetTimesheetMonthAsync(
                year,
                month,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(rows);

        var sut = new ExportTimesheetMonthQueryHandler(repo.Object);

        var query = new ExportTimesheetMonthQuery(
            Year: year,
            Month: month,
            Search: null);

        // act
        var file = await sut.HandleAsync(query, CancellationToken.None);

        // assert workbook comment
        var bytes = Convert.FromBase64String(file.Base64);
        using var ms = new MemoryStream(bytes);
        using var wb = new XLWorkbook(ms);

        var ws = wb.Worksheets.First();

        // Day 1 => column 6, first data row => row 2
        var cell = ws.Cell(2, 6);

        Assert.Equal("100", cell.GetString());

        // ClosedXML: comment should exist and contain ref text
        Assert.True(cell.HasComment);
        var txt = cell.GetComment().Text; // safe for newer ClosedXML
        Assert.Contains("REF-ABC", txt);

        repo.Verify(x => x.GetTimesheetMonthAsync(
            year, month, null, It.IsAny<CancellationToken>()), Times.Once);

        repo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_when_year_out_of_range_should_throw()
    {
        var repo = new Mock<ITimesheetMonthRepository>(MockBehavior.Strict);
        var sut = new ExportTimesheetMonthQueryHandler(repo.Object);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            sut.HandleAsync(new ExportTimesheetMonthQuery(Year: 1999, Month: 1, Search: null)));

        repo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_when_month_out_of_range_should_throw()
    {
        var repo = new Mock<ITimesheetMonthRepository>(MockBehavior.Strict);
        var sut = new ExportTimesheetMonthQueryHandler(repo.Object);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            sut.HandleAsync(new ExportTimesheetMonthQuery(Year: 2026, Month: 13, Search: null)));

        repo.VerifyNoOtherCalls();
    }
}
