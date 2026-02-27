//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// GetTimesheetPersonMonthQueryHandlerTests
//-----------------------------------------------------------------------------

using eRaven.Application.Abstractions.PersonRepository;
using eRaven.Application.Abstractions.TimesheetPolicyRepository;
using eRaven.Application.Abstractions.TimesheetRepository;
using eRaven.Application.Abstractions.TimesheetRepository.ReadModels;
using eRaven.Application.Handlers.Timesheets;
using eRaven.Application.Queries.Timesheets;
using eRaven.Domain.Entities;
using eRaven.Domain.Enums;
using Moq;

namespace eRaven.Tests.Application.Handlers.Timesheets;

public sealed class GetTimesheetPersonMonthQueryHandlerTests
{
    private readonly Mock<ITimesheetViewRepository> _viewRepo;
    private readonly Mock<ITimesheetEntryQueryRepository> _entryRepo;
    private readonly Mock<ITimesheetPolicyRepository> _policyRepo;
    private readonly Mock<IPersonRepository> _personsRepo;

    public GetTimesheetPersonMonthQueryHandlerTests()
    {
        _viewRepo = new(MockBehavior.Strict);
        _entryRepo = new(MockBehavior.Strict);
        _policyRepo = new(MockBehavior.Strict);
        _personsRepo = new(MockBehavior.Strict);
    }

    [Fact]
    public async Task HandleAsync_WhenPeriodMissing_ReturnsNull_AndDoesNotLoadPersonOrEntries()
    {
        // arrange
        var personId = Guid.NewGuid();
        var query = new GetTimesheetPersonMonthQuery(personId, Year: 2026, Month: 2);

        _viewRepo.Setup(r => r.GetTimesheetPersonMonthAsync(personId, 2026, 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TimesheetPeriodRm?)null);

        var sut = new GetTimesheetPersonMonthQueryHandler(_viewRepo.Object, _entryRepo.Object, _policyRepo.Object, _personsRepo.Object);

        // act
        var dto = await sut.HandleAsync(query);

        // assert
        Assert.Null(dto);
        _viewRepo.VerifyAll();
        _entryRepo.VerifyNoOtherCalls();
        _policyRepo.VerifyNoOtherCalls();
        _personsRepo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_WhenPersonMissing_ReturnsNull_AndDoesNotLoadEntries()
    {
        // arrange
        var personId = Guid.NewGuid();
        var query = new GetTimesheetPersonMonthQuery(personId, Year: 2026, Month: 2);

        _viewRepo.Setup(r => r.GetTimesheetPersonMonthAsync(personId, 2026, 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePeriod(personId, 2026, 2));

        _personsRepo.Setup(r => r.GetByIdAsync(personId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PersonReadModel?)null);

        var sut = new GetTimesheetPersonMonthQueryHandler(_viewRepo.Object, _entryRepo.Object, _policyRepo.Object, _personsRepo.Object);

        // act
        var dto = await sut.HandleAsync(query);

        // assert
        Assert.Null(dto);
        _viewRepo.VerifyAll();
        _personsRepo.VerifyAll();
        _entryRepo.VerifyNoOtherCalls();
        _policyRepo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_WhenNoEntries_ReturnsEmptyEntries_AndSkipsPolicyLookup()
    {
        // arrange
        var personId = Guid.NewGuid();
        var query = new GetTimesheetPersonMonthQuery(personId, Year: 2026, Month: 2);

        _viewRepo.Setup(r => r.GetTimesheetPersonMonthAsync(personId, 2026, 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePeriod(personId, 2026, 2));

        _personsRepo.Setup(r => r.GetByIdAsync(personId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePerson(personId, "Alpha", positionSort: 10));

        _entryRepo.Setup(r => r.GetEntriesForPersonAsync(
                personId,
                new DateOnly(2026, 2, 1),
                new DateOnly(2026, 2, 28),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var sut = new GetTimesheetPersonMonthQueryHandler(_viewRepo.Object, _entryRepo.Object, _policyRepo.Object, _personsRepo.Object);

        // act
        var dto = await sut.HandleAsync(query);

        // assert
        Assert.NotNull(dto);
        Assert.Equal(personId, dto!.Person.PersonId);
        Assert.Empty(dto.Entries);
        Assert.Equal(DateTime.MinValue, dto.UpdatedAtUtc);
        Assert.Equal(28, dto.Days.Count);

        _viewRepo.VerifyAll();
        _personsRepo.VerifyAll();
        _entryRepo.VerifyAll();

        // IMPORTANT: no _policy lookup when there are no _entries.
        _policyRepo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_MapsEntries_UsesPolicyCodes_TrimsFields_OrdersByFromThenId_AndSetsUpdatedAtUtc()
    {
        // arrange
        var personId = Guid.NewGuid();
        var query = new GetTimesheetPersonMonthQuery(personId, Year: 2026, Month: 2);

        _viewRepo.Setup(r => r.GetTimesheetPersonMonthAsync(personId, 2026, 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePeriod(personId, 2026, 2));

        _personsRepo.Setup(r => r.GetByIdAsync(personId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePerson(personId, "Alpha", positionSort: 2));

        var codeId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        var e1Id = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var e2Id = Guid.Parse("00000000-0000-0000-0000-000000000002");

        var createdAt1 = new DateTime(2026, 02, 10, 10, 0, 0, DateTimeKind.Utc);
        var updatedAt1 = new DateTime(2026, 02, 10, 12, 0, 0, DateTimeKind.Utc);
        var createdAt2 = new DateTime(2026, 02, 09, 10, 0, 0, DateTimeKind.Utc);

        // Intentionally unsorted (and same From) to validate ordering by From then Id.
        var _entries = new List<TimesheetEntry>
        {
            new()
            {
                Id = e2Id,
                PersonId = personId,
                TimesheetCodeDefinitionId = codeId,
                From = new DateOnly(2026, 02, 10),
                To = new DateOnly(2026, 02, 11),
                Reference = "  REF-2  ",
                Note = "  NOTE-2  ",
                CreatedAtUtc = createdAt2,
                UpdatedAtUtc = null,
            },
            new()
            {
                Id = e1Id,
                PersonId = personId,
                TimesheetCodeDefinitionId = codeId,
                From = new DateOnly(2026, 02, 10),
                To = null,
                Reference = "REF-1",
                Note = null,
                CreatedAtUtc = createdAt1,
                UpdatedAtUtc = updatedAt1,
            }
        };

        _entryRepo.Setup(r => r.GetEntriesForPersonAsync(
                personId,
                new DateOnly(2026, 2, 1),
                new DateOnly(2026, 2, 28),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(_entries);

        _policyRepo.Setup(r => r.GetCodesAsync(includeInactive: true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new() { Id = codeId, Code = "  Т  " }
            ]);

        var sut = new GetTimesheetPersonMonthQueryHandler(_viewRepo.Object, _entryRepo.Object, _policyRepo.Object, _personsRepo.Object);

        // act
        var dto = await sut.HandleAsync(query);

        // assert
        Assert.NotNull(dto);
        Assert.Equal(personId, dto!.Person.PersonId);

        // UpdatedAtUtc: max(UpdatedAtUtc ?? CreatedAtUtc)
        Assert.Equal(updatedAt1, dto.UpdatedAtUtc);

        Assert.Equal(2, dto.Entries.Count);

        // ordering by From then Id (same From -> stable order)
        Assert.Equal("Т", dto.Entries[0].Code);
        Assert.Equal("REF-1", dto.Entries[0].Reference);
        Assert.Null(dto.Entries[0].Note);

        Assert.Equal("Т", dto.Entries[1].Code);
        Assert.Equal("REF-2", dto.Entries[1].Reference);
        Assert.Equal("NOTE-2", dto.Entries[1].Note);

        _viewRepo.VerifyAll();
        _personsRepo.VerifyAll();
        _entryRepo.VerifyAll();
        _policyRepo.VerifyAll();
    }

    private static TimesheetPeriodRm CreatePeriod(Guid personId, int year, int month)
    {
        var days = new List<TimesheetDayRm>();
        var daysInMonth = DateTime.DaysInMonth(year, month);

        for (var d = 1; d <= daysInMonth; d++)
        {
            // default: derived NB
            days.Add(new TimesheetDayRm(
                TimesheetId: Guid.Empty,
                DateOfDay: new DateOnly(year, month, d),
                CodeId: null,
                Code: "НБ",
                Reference: null,
                Note: null,
                IsDerived: true,
                IsChangePoint: false,
                UiStyle: TimesheetUiStyle.NotInTimesheet));
        }

        return new TimesheetPeriodRm(personId, days);
    }

    private static PersonReadModel CreatePerson(Guid id, string fullName, int? positionSort)
        => new()
        {
            Id = id,
            Lifecycle = PersonLifecycle.Enrolled,
            EnrollmentKind = EnrollmentKind.Unit,
            EnrollmentReference = string.Empty,
            Rnokpp = "1234567891",
            LastName = "LastName",
            FirstName = "FirstName",
            MiddleName = "MiddleName",
            FullName = fullName,
            Rank = "soldier",
            PositionSort = positionSort,
            Position = "commander",
            Bzvp = "bzvp",
            Weapon = "Weapon"
        };
}
