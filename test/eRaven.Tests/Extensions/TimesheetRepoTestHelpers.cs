//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetRepoTestHelpers
//-----------------------------------------------------------------------------

using eRaven.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Tests.Extensions;

internal static class TimesheetRepoTestHelpers
{
    public static readonly DateTime NowUtc = new(2026, 02, 25, 12, 00, 00, DateTimeKind.Utc);
    public const string Author = "test";

    public static async Task SeedPolicyAsync(SqliteTestDb db, CancellationToken ct = default)
    {
        await using var ctx = await db.Factory.CreateDbContextAsync(ct);
        await TimesheetPolicySeed.EnsureSeedAsync(ctx, ct);
    }

    public static async Task<Guid> GetCodeIdAsync(SqliteTestDb db, string code, CancellationToken ct = default)
    {
        await using var ctx = await db.Factory.CreateDbContextAsync(ct);

        return await ctx.TimesheetCodes
            .AsNoTracking()
            .Where(x => x.IsActive)
            .Where(x => x.Code == code)
            .Select(x => x.Id)
            .SingleAsync(ct);
    }
}
