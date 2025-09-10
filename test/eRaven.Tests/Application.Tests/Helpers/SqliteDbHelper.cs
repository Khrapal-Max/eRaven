//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// SqliteDbHelper
//-----------------------------------------------------------------------------

using eRaven.Infrastructure;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Tests.Application.Tests.Helpers;

internal sealed class SqliteDbHelper : IDisposable
{
    private readonly SqliteConnection _conn;
    public AppDbContext Db { get; }

    public SqliteDbHelper()
    {
        _conn = new SqliteConnection("Filename=:memory:");
        _conn.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_conn)
            .EnableSensitiveDataLogging()
            .Options;

        Db = new AppDbContext(options);
        Db.Database.EnsureCreated();

        // 🔧 SQLite не підтримує filtered index → видаляємо тестовий сурогат,
        // аби можна було мати кілька записів на одну особу
        Db.Database.ExecuteSqlRaw(
            "DROP INDEX IF EXISTS ix_person_statuses_active_unique_per_person;");

    }

    public void Dispose()
    {
        Db.Dispose();
        _conn.Dispose();
    }
}