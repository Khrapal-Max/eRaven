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
        var repo = new Mock<ITimesheetMonthGridRepository>(MockBehavior.Strict);

        var year = 2026;
        var month = 1;

        var main = new string[31];
        var task = new string[31];

        for (var i = 0; i < 31; i++)
        {
            main[i] = "30";
            task[i] = "";
        }

        // Day 1: both lanes visible in same cell
        task[0] = "ПБД";

        var row = new TimesheetMonthPersonRowDto(
            PersonId: Guid.NewGuid(),
            FullName: "Іванов Іван",
            RNOKPP: "1234567890",
            Rank: "Солдат",
            Position: "Стрілець",
            EnrollmentKind: EnrollmentKind.Unit,
            EnrolledAt: new DateOnly(2026, 1, 1),
            ExcludedAt: null,
            MainCodes: main,
            TaskCodes: task);

        var grid = new TimesheetMonthGridDto(
            Year: year,
            Month: month,
            DaysInMonth: 31,
            Rows: [row]);

        repo.Setup(x => x.GetTimesheetMonthAsync(
                year,
                month,
                "ivanov", // trimmed
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(grid);

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

        using (var ms = new MemoryStream(bytes))
        using (var wb = new XLWorkbook(ms))
        {
            Assert.Single(wb.Worksheets);

            var ws = wb.Worksheets.First();
            Assert.Equal("Табель Січ 2026", ws.Name);

            // Header
            Assert.Equal("Тип", ws.Cell(1, 1).GetString());
            Assert.Equal("ПІБ", ws.Cell(1, 4).GetString());
            Assert.Equal("1 Січ", ws.Cell(1, 6).GetString()); // first day column

            // First row body: EnrollmentKind.Unit => "ШТ"
            Assert.Equal("ШТ", ws.Cell(2, 1).GetString());

            // Day 1 cell = "30\nПБД"
            var day1 = ws.Cell(2, 6).GetString();
            Assert.Contains("30", day1);
            Assert.Contains("ПБД", day1);
        }

        repo.Verify(x => x.GetTimesheetMonthAsync(
            year, month, "ivanov", It.IsAny<CancellationToken>()), Times.Once);

        repo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_when_year_out_of_range_should_throw()
    {
        var repo = new Mock<ITimesheetMonthGridRepository>(MockBehavior.Strict);
        var sut = new ExportTimesheetMonthQueryHandler(repo.Object);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            sut.HandleAsync(new ExportTimesheetMonthQuery(Year: 1999, Month: 1, Search: null)));

        repo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_when_month_out_of_range_should_throw()
    {
        var repo = new Mock<ITimesheetMonthGridRepository>(MockBehavior.Strict);
        var sut = new ExportTimesheetMonthQueryHandler(repo.Object);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            sut.HandleAsync(new ExportTimesheetMonthQuery(Year: 2026, Month: 13, Search: null)));

        repo.VerifyNoOtherCalls();
    }
}
