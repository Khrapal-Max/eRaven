//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// MigrationExtension
//-----------------------------------------------------------------------------

using eRaven.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Extensions;

public static class MigrationExtension
{
    public static async Task AddMigrationDb(this WebApplication app)
    {
        const int maxRetries = 10;
        var delay = TimeSpan.FromSeconds(2);

        Exception? lastError = null;

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            using var scope = app.Services.CreateScope();
            var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            try
            {
                var pending = await db.Database.GetPendingMigrationsAsync();
                logger.LogInformation("Pending migrations: {Migrations}", string.Join(", ", pending));

                await db.Database.MigrateAsync();
                logger.LogInformation("✅ Database migrated successfully.");

                await TimesheetPolicySeed.EnsureSeedAsync(db, CancellationToken.None);
                logger.LogInformation("✅ Timesheet policy seeded.");

                // await RankSeed.EnsureSeededAsync(db, CancellationToken.None);
                // logger.LogInformation("✅ Rank dictionary seeded.");
                return;
            }
            catch (Exception ex) when (
                ex is Npgsql.PostgresException ||
                ex is Npgsql.NpgsqlException)
            {
                lastError = ex;
                logger.LogWarning(ex, "DB migrate failed (attempt {Attempt}/{Max}). Retrying in {Delay}s...",
                    attempt, maxRetries, delay.TotalSeconds);
            }
            catch (Exception ex)
            {
                lastError = ex;
                logger.LogWarning(ex, "Unexpected migrate error (attempt {Attempt}/{Max}). Retrying in {Delay}s...",
                    attempt, maxRetries, delay.TotalSeconds);
            }

            if (attempt < maxRetries)
            {
                await Task.Delay(delay);
                delay += TimeSpan.FromSeconds(1);
            }
        }

        throw new InvalidOperationException(
            $"Database migration failed after {maxRetries} attempts.",
            lastError ?? new Exception("Unknown migration error"));
    }
}