//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ExportTimesheetMonthQueryHandlerTests
//-----------------------------------------------------------------------------

using ClosedXML.Excel;
using eRaven.Application.DTOs.Excel;
using eRaven.Application.DTOs.Timesheet;
using eRaven.Application.Handlers.Timesheet;
using eRaven.Application.Queries.Timesheet;
using eRaven.Domain.Enums;
using eRaven.Infrastructure.Repositories.TimesheetRepository;
using Moq;

namespace eRaven.Tests.Application.Handlers.Timesheet;

public sealed class ExportTimesheetMonthQueryHandlerTests
{
    private static XLWorkbook LoadWorkbook(DownloadFileDto dto)
    {
        var bytes = Convert.FromBase64String(dto.Base64);
        using var ms = new MemoryStream(bytes);
        return new XLWorkbook(ms);
    }

    [Theory]
    [InlineData(1999, 1)]
    [InlineData(2101, 1)]
    [InlineData(2000, 0)]
    [InlineData(2000, 13)]
    public async Task HandleAsync_should_throw_when_year_or_month_out_of_range(int year, int month)
    {
        var repo = new Mock<ITimesheetRepository>(MockBehavior.Strict);
        var h = new ExportTimesheetMonthQueryHandler(repo.Object);

        var q = new ExportTimesheetMonthQuery(Year: year, Month: month, Search: null);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => h.HandleAsync(q));
    }

    [Fact]
    public async Task HandleAsync_should_call_repo_with_trimmed_search_and_return_expected_metadata()
    {
        var repo = new Mock<ITimesheetRepository>(MockBehavior.Strict);

        repo.Setup(x => x.GetMonthlyTimesheetAsync(
                2026, 1, "abc", It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var h = new ExportTimesheetMonthQueryHandler(repo.Object);

        var dto = await h.HandleAsync(new ExportTimesheetMonthQuery(
            Year: 2026,
            Month: 1,
            Search: "  abc  "));

        Assert.NotNull(dto);
        Assert.Equal(ExportTimesheetMonthQueryHandler.XlsxContentType, dto.ContentType);
        Assert.Equal("Timesheet_2026-01_Січ.xlsx", dto.FileName);
        Assert.False(string.IsNullOrWhiteSpace(dto.Base64));

        repo.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_should_build_valid_xlsx_with_expected_headers_and_cells()
    {
        // arrange
        var repo = new Mock<ITimesheetRepository>(MockBehavior.Strict);

        var p1 = Guid.NewGuid();
        var p2 = Guid.NewGuid();

        // days: day1 main=30 + task=BT => "30\nBT"
        // day2 main=НБ only
        var ts1 = new MonthlyTimesheetReadModelDto(
            PersonId: p1,
            Year: 2026,
            Month: 1,
            Days:
            [
                new MonthlyTimesheetDayDto(EntryId: null, Day: 1, Lane: TimesheetLane.Main, Code: "30"),
                new MonthlyTimesheetDayDto(EntryId: null, Day: 1, Lane: TimesheetLane.Task, Code: "BT"),
                new MonthlyTimesheetDayDto(EntryId: null, Day: 2, Lane: TimesheetLane.Main, Code: "НБ"),
            ],
            UpdatedAtUtc: DateTime.MinValue
        );

        var ts2 = new MonthlyTimesheetReadModelDto(
            PersonId: p2,
            Year: 2026,
            Month: 1,
            Days:
            [
                new MonthlyTimesheetDayDto(EntryId: null, Day: 1, Lane: TimesheetLane.Main, Code: "30"),
            ],
            UpdatedAtUtc: DateTime.MinValue
        );

        // ⚠️ Якщо у тебе DTO має інші назви полів (наприклад Rnokpp замість RNOKPP),
        // відкоригуй тут і в handler.
        var rows = new List<TimesheetMonthPerPersonDto>
        {
            new(
                PersonId: p1,
                FullName: "Іванов Іван Іванович",
                Rank: "Солдат",
                Position: "Стрілець",
                EnrolledAt: new DateOnly(2026, 01, 01),
                ExcludedAt: null,
                EnrollmentKind: EnrollmentKind.Unit,
                RNOKPP: "1111111111",
                Timesheet: ts1
            ),
            new(
                PersonId: p2,
                FullName: "Петров Петро Петрович",
                Rank: "Сержант",
                Position: "Навідник",
                EnrolledAt: new DateOnly(2026, 01, 01),
                ExcludedAt: null,
                EnrollmentKind: EnrollmentKind.AttachedByOrder,
                RNOKPP: "2222222222",
                Timesheet: ts2
            )
        };

        repo.Setup(x => x.GetMonthlyTimesheetAsync(
                2026, 1, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rows);

        var h = new ExportTimesheetMonthQueryHandler(repo.Object);

        // act
        var dto = await h.HandleAsync(new ExportTimesheetMonthQuery(2026, 1, null));

        // assert (dto)
        Assert.Equal(ExportTimesheetMonthQueryHandler.XlsxContentType, dto.ContentType);
        Assert.Equal("Timesheet_2026-01_Січ.xlsx", dto.FileName);

        // assert (xlsx)
        using var wb = LoadWorkbook(dto);
        var ws = wb.Worksheets.Single();

        Assert.Equal("Табель Січ 2026", ws.Name);

        // Header row
        Assert.Equal("Тип", ws.Cell(1, 1).GetString());
        Assert.Equal("Посада", ws.Cell(1, 2).GetString());
        Assert.Equal("Звання", ws.Cell(1, 3).GetString());
        Assert.Equal("ПІБ", ws.Cell(1, 4).GetString());
        Assert.Equal("РНКОПП", ws.Cell(1, 5).GetString());

        // day headers: "1 Січ", "2 Січ", ... up to 31
        Assert.Equal("1 Січ", ws.Cell(1, 6).GetString());
        Assert.Equal("2 Січ", ws.Cell(1, 7).GetString());
        Assert.Equal("31 Січ", ws.Cell(1, 36).GetString()); // 5 fixed cols + 31 days => last = 36

        // Row #1
        Assert.Equal("ШТ", ws.Cell(2, 1).GetString());         // EnrollmentKind.Unit => "ШТ"
        Assert.Equal("Стрілець", ws.Cell(2, 2).GetString());
        Assert.Equal("Солдат", ws.Cell(2, 3).GetString());
        Assert.Equal("Іванов Іван Іванович", ws.Cell(2, 4).GetString());
        Assert.Equal("1111111111", ws.Cell(2, 5).GetString());

        // Day1 cell => "30\nBT"
        Assert.Equal("30\nBT", ws.Cell(2, 6).GetString());

        // Day2 cell => "НБ" (only main)
        Assert.Equal("НБ", ws.Cell(2, 7).GetString());

        // Day3 cell empty
        Assert.Equal("", ws.Cell(2, 8).GetString());

        // Row #2
        Assert.Equal("БР", ws.Cell(3, 1).GetString());         // AttachedByOrder => "БР"
        Assert.Equal("Навідник", ws.Cell(3, 2).GetString());
        Assert.Equal("Сержант", ws.Cell(3, 3).GetString());
        Assert.Equal("Петров Петро Петрович", ws.Cell(3, 4).GetString());
        Assert.Equal("2222222222", ws.Cell(3, 5).GetString());

        // Day1 cell => "30" (no task)
        Assert.Equal("30", ws.Cell(3, 6).GetString());

        repo.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_when_repo_returns_empty_should_still_return_xlsx_with_header_only()
    {
        var repo = new Mock<ITimesheetRepository>(MockBehavior.Strict);

        repo.Setup(x => x.GetMonthlyTimesheetAsync(
                2026, 2, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var h = new ExportTimesheetMonthQueryHandler(repo.Object);

        var dto = await h.HandleAsync(new ExportTimesheetMonthQuery(2026, 2, null));

        using var wb = LoadWorkbook(dto);
        var ws = wb.Worksheets.Single();

        Assert.Equal("Табель Лют 2026", ws.Name);

        // header exists
        Assert.Equal("Тип", ws.Cell(1, 1).GetString());
        Assert.Equal("Посада", ws.Cell(1, 2).GetString());
        Assert.Equal("Звання", ws.Cell(1, 3).GetString());
        Assert.Equal("ПІБ", ws.Cell(1, 4).GetString());
        Assert.Equal("РНКОПП", ws.Cell(1, 5).GetString());

        // Feb 2026 -> 28 days (2026 не високосний)
        Assert.Equal("1 Лют", ws.Cell(1, 6).GetString());
        Assert.Equal("28 Лют", ws.Cell(1, 33).GetString()); // 5 + 28 = 33

        // no data rows
        Assert.True(ws.Row(2).IsEmpty());

        repo.VerifyAll();
    }
}
