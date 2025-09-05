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
        using (var scope = app.Services.CreateScope())
        {
            var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            const int maxRetries = 10;
            var delay = TimeSpan.FromSeconds(2);

            Exception? lastError = null;
            bool migrated = false;

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    await db.Database.MigrateAsync();
                    logger.LogInformation("✅ Database migrated successfully.");
                    migrated = true;
                    break;
                }
                catch (Npgsql.PostgresException ex)
                {
                    lastError = ex;
                    logger.LogWarning(ex, "PostgresException on migrate (attempt {Attempt}/{Max}). Retrying in {Delay}s...",
                        attempt, maxRetries, delay.TotalSeconds);
                }
                catch (Npgsql.NpgsqlException ex)
                {
                    lastError = ex;
                    logger.LogWarning(ex, "NpgsqlException on migrate (attempt {Attempt}/{Max}). Retrying in {Delay}s...",
                        attempt, maxRetries, delay.TotalSeconds);
                }
                catch (Exception ex)
                {
                    lastError = ex;
                    if (attempt < maxRetries)
                    {
                        logger.LogWarning(ex, "Unexpected exception on migrate (attempt {Attempt}/{Max}). Retrying in {Delay}s...",
                            attempt, maxRetries, delay.TotalSeconds);
                    }
                }

                if (attempt < maxRetries)
                {
                    await Task.Delay(delay);
                    delay += TimeSpan.FromSeconds(1);
                }
            }

            if (!migrated)
            {
                logger.LogCritical(lastError, "❌ Failed to migrate database after {Max} attempts.", maxRetries);
                // Варіант 1 (рекомендовано): впасти з осмисленим винятком — Docker перезапустить
                throw new InvalidOperationException(
                    $"Database migration failed after {maxRetries} attempts.",
                    lastError ?? new Exception("Unknown migration error"));
                // Варіант 2 (м'яко зупинити додаток):
                // scope.ServiceProvider.GetRequiredService<IHostApplicationLifetime>().StopApplication();
            }
        }
    }
}
