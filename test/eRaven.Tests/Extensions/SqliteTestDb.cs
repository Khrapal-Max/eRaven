//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// SqliteTestDb
//-----------------------------------------------------------------------------

using eRaven.Infrastructure;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Tests.Extensions;

public sealed class SqliteTestDb : IAsyncDisposable
{
    private readonly SqliteConnection _connection;
    public IDbContextFactory<AppDbContext> Factory { get; }

    public SqliteTestDb()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .EnableSensitiveDataLogging()
            .Options;

        Factory = new SimpleDbContextFactory(options);

        using var db = Factory.CreateDbContext();
        db.Database.EnsureCreated();
    }

    public ValueTask DisposeAsync()
    {
        _connection.Close();
        _connection.Dispose();
        return ValueTask.CompletedTask;
    }

    private sealed class SimpleDbContextFactory(DbContextOptions<AppDbContext> options)
        : IDbContextFactory<AppDbContext>
    {
        private readonly DbContextOptions<AppDbContext> _options = options;

        public AppDbContext CreateDbContext() => new AppDbContext(_options);
    }
}
