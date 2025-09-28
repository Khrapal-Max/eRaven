//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// SqliteDbHelper
//-----------------------------------------------------------------------------

using eRaven.Infrastructure;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace eRaven.Tests.Application.Tests.Helpers;

internal sealed class SqliteDbHelper : IDisposable
{
    private readonly SqliteConnection _conn;
    private readonly ServiceProvider _sp;

    /// <summary>
    /// Фабрика DbContext — використовуйте в сервісах, які тепер інʼєктять IDbContextFactory<AppDbContext>.
    /// </summary>
    public IDbContextFactory<AppDbContext> Factory { get; }

    /// <summary>
    /// Зручний довгоживучий контекст для послідовних Arrange/Assert у тестах.
    /// Якщо плануються паралельні звернення — створюйте окремі контексти через <see cref="CreateContext"/>.
    /// </summary>
    public AppDbContext Db { get; }

    public SqliteDbHelper()
    {
        // один in-memory коннект на всі контексти
        _conn = new SqliteConnection("Filename=:memory:");
        _conn.Open();

        // DI-контейнер з фабрикою контекстів
        var services = new ServiceCollection();
        services.AddDbContextFactory<AppDbContext>(options =>
        {
            options
                .UseSqlite(_conn)
                .EnableSensitiveDataLogging();
        });

        _sp = services.BuildServiceProvider();
        Factory = _sp.GetRequiredService<IDbContextFactory<AppDbContext>>();

        // 1) ініціалізуємо схему
        using (var db = Factory.CreateDbContext())
        {
            db.Database.EnsureCreated();

            // SQLite не підтримує filtered index → прибираємо тестовий сурогат
            db.Database.ExecuteSqlRaw(
                "DROP INDEX IF EXISTS ix_person_statuses_active_unique_per_person;");
        }

        // 2) зручний довгоживучий контекст для більшості тестів
        Db = Factory.CreateDbContext();
    }

    /// <summary>
    /// Створити окремий короткоживучий контекст (наприклад, для паралельних викликів або емуляції життєвого циклу сервісів).
    /// Викликати <c>Dispose()</c> на отриманому екземплярі обовʼязково (через using).
    /// </summary>
    public AppDbContext CreateContext() => Factory.CreateDbContext();

    public void Dispose()
    {
        Db.Dispose();
        _sp.Dispose();
        _conn.Dispose();
    }
}