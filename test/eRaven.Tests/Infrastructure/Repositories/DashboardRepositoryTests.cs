//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// DashboardRepositoryTests
//-----------------------------------------------------------------------------

using eRaven.Domain.Entities;
using eRaven.Domain.Enums;
using eRaven.Infrastructure.Repositories.DashboardRepository;
using eRaven.Tests.Extensions;

namespace eRaven.Tests.Infrastructure.Repositories;

public sealed class DashboardRepositoryTests
{
    [Fact]
    public async Task GetPersonnelDashboardAsync_counts_only_enrolled_and_splits_by_enrollment_kind()
    {
        // Arrange
        await using var db = new SqliteTestDb();

        await SeedPersonsAsync(db);

        var repo = new DashboardRepository(db.Factory);

        // Act
        var snap = await repo.GetPersonnelDashboardAsync(CancellationToken.None);

        // Assert
        // Enrolled: 5 total (2 Unit, 1 AttachedByList, 1 AttachedByOrder, 1 with null kind [if allowed])
        Assert.Equal(5, snap.TotalInTimesheet);
        Assert.Equal(2, snap.TimesheetUnit);
        Assert.Equal(1, snap.TimesheetOrder);
        Assert.Equal(1, snap.TimesheetBr);
    }

    [Fact]
    public async Task GetPersonnelDashboardAsync_returns_zeros_when_no_enrolled()
    {
        // Arrange
        await using var db = new SqliteTestDb();

        // Seed only Reserved
        await using (var ctx = await db.Factory.CreateDbContextAsync())
        {
            ctx.PersonRead.Add(new PersonReadModel
            {
                Id = Guid.NewGuid(),
                FullName = "Reserved One",
                Rnokpp = "R1",
                Lifecycle = PersonLifecycle.Reserved,
                EnrollmentKind = EnrollmentKind.Unit
            });

            ctx.PersonRead.Add(new PersonReadModel
            {
                Id = Guid.NewGuid(),
                FullName = "Reserved Two",
                Rnokpp = "R2",
                Lifecycle = PersonLifecycle.Reserved,
                EnrollmentKind = EnrollmentKind.AttachedByOrder
            });

            await ctx.SaveChangesAsync();
        }

        var repo = new DashboardRepository(db.Factory);

        // Act
        var snap = await repo.GetPersonnelDashboardAsync(CancellationToken.None);

        // Assert
        Assert.Equal(0, snap.TotalInTimesheet);
        Assert.Equal(0, snap.TimesheetUnit);
        Assert.Equal(0, snap.TimesheetOrder);
        Assert.Equal(0, snap.TimesheetBr);
    }

    private static async Task SeedPersonsAsync(SqliteTestDb db)
    {
        await using var ctx = await db.Factory.CreateDbContextAsync();

        // NOTE:
        // Якщо у вашого PersonReadModel є обов'язкові поля (NOT NULL) — додайте їх тут.

        // 2 x Reserved (не мають потрапити в "В табелі")
        ctx.PersonRead.Add(new PersonReadModel
        {
            Id = Guid.NewGuid(),
            FullName = "Reserved One",
            Rnokpp = "R1",
            Lifecycle = PersonLifecycle.Reserved,
            EnrollmentKind = EnrollmentKind.Unit
        });

        ctx.PersonRead.Add(new PersonReadModel
        {
            Id = Guid.NewGuid(),
            FullName = "Reserved Two",
            Rnokpp = "R2",
            Lifecycle = PersonLifecycle.Reserved,
            EnrollmentKind = EnrollmentKind.AttachedByOrder
        });

        // 5 x Enrolled (мають рахуватись)
        ctx.PersonRead.Add(new PersonReadModel
        {
            Id = Guid.NewGuid(),
            FullName = "Enrolled Unit 1",
            Rnokpp = "E1",
            Lifecycle = PersonLifecycle.Enrolled,
            EnrollmentKind = EnrollmentKind.Unit
        });

        ctx.PersonRead.Add(new PersonReadModel
        {
            Id = Guid.NewGuid(),
            FullName = "Enrolled Unit 2",
            Rnokpp = "E2",
            Lifecycle = PersonLifecycle.Enrolled,
            EnrollmentKind = EnrollmentKind.Unit
        });

        ctx.PersonRead.Add(new PersonReadModel
        {
            Id = Guid.NewGuid(),
            FullName = "Enrolled By List",
            Rnokpp = "E3",
            Lifecycle = PersonLifecycle.Enrolled,
            EnrollmentKind = EnrollmentKind.AttachedByList
        });

        ctx.PersonRead.Add(new PersonReadModel
        {
            Id = Guid.NewGuid(),
            FullName = "Enrolled By Order",
            Rnokpp = "E4",
            Lifecycle = PersonLifecycle.Enrolled,
            EnrollmentKind = EnrollmentKind.AttachedByOrder
        });

        // Якщо EnrollmentKind nullable у вашій моделі:
        ctx.PersonRead.Add(new PersonReadModel
        {
            Id = Guid.NewGuid(),
            FullName = "Enrolled Null Kind",
            Rnokpp = "E5",
            Lifecycle = PersonLifecycle.Enrolled,
            EnrollmentKind = null
        });

        await ctx.SaveChangesAsync();
    }
}
