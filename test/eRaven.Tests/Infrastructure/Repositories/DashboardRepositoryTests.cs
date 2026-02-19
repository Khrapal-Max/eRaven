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

/// <summary>
/// Тести для <see cref="DashboardRepository"/>.
///
/// <para>Фіксуємо контракт:</para>
/// <list type="bullet">
/// <item><description>в дашборд потрапляють лише особи з <see cref="PersonLifecycle.Enrolled"/>.</description></item>
/// </list>
/// </summary>
public sealed class DashboardRepositoryTests
{
    [Fact]
    public async Task GetPersonnelDashboardAsync_ReturnsOnlyEnrolled()
    {
        await using var tdb = new SqliteTestDb();

        var enrolled1 = NewPerson(lifecycle: PersonLifecycle.Enrolled, kind: EnrollmentKind.Unit);
        var enrolled2 = NewPerson(lifecycle: PersonLifecycle.Enrolled, kind: EnrollmentKind.AttachedByList);

        var reserved1 = NewPerson(lifecycle: PersonLifecycle.Reserved, kind: null);
        var reserved2 = NewPerson(lifecycle: PersonLifecycle.Reserved, kind: null);

        await using (var db = await tdb.Factory.CreateDbContextAsync())
        {
            db.PersonRead.AddRange(enrolled1, enrolled2, reserved1, reserved2);
            await db.SaveChangesAsync();
        }

        var repo = new DashboardRepository(tdb.Factory);

        var rows = await repo.GetPersonnelDashboardAsync();

        Assert.Equal(2, rows.Count);
        Assert.All(rows, x => Assert.Equal(PersonLifecycle.Enrolled, x.Lifecycle));
    }

    //======================================================================
    // Helpers
    //======================================================================

    /// <summary>
    /// Створює мінімально валідний <see cref="PersonReadModel"/> для тестів.
    /// Заповнюємо лише те, що точно потрібно для EF/логіки.
    /// </summary>
    private static PersonReadModel NewPerson(PersonLifecycle lifecycle, EnrollmentKind? kind)
        => new()
        {
            Id = Guid.NewGuid(),
            Rnokpp = Guid.NewGuid().ToString("N")[..10],
            FullName = $"P-{Guid.NewGuid():N}"[..10],
            Lifecycle = lifecycle,
            EnrollmentKind = kind,

            // якщо у вас є required поля в EF — додай їх тут мінімально валідними значеннями
            Rank = null,
            Position = null,
            Weapon = null,
            Callsign = null
        };
}
