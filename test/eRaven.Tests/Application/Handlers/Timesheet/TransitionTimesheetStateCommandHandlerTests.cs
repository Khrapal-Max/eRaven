//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// TransitionTimesheetStateCommandHandler_SameDayReplace_Tests
//-----------------------------------------------------------------------------

using eRaven.Application.Commands.Timesheet;
using eRaven.Application.Handlers.Timesheet;
using eRaven.Domain.Entities;
using eRaven.Infrastructure;
using eRaven.Infrastructure.Repositories.TimesheetPolicyRepository;
using eRaven.Infrastructure.Repositories.TimesheetRepository;
using eRaven.Tests.Extensions;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Tests.Application.Handlers.Timesheet;

/// <summary>
/// Регресійні тести на "same-day replace" після Enroll:
/// Enroll створює дефолтний код "Т" на дату зарахування.
/// Далі оператор ставить "30" на ту ж дату — очікуємо, що "Т" буде замінено на "30" in-place
/// (без створення нового інтервалу), і що зміна реально запишеться в БД.
/// </summary>
public sealed class TransitionTimesheetStateCommandHandlerTests
{
    [Fact]
    public async Task Enroll_T_SameDaySet30_ReplacesEntryAndPersists_30()
    {
        // Arrange
        await using var testDb = new SqliteTestDb();
        var now = new DateTime(2026, 02, 18, 08, 00, 00, DateTimeKind.Utc);

        var personId = Guid.NewGuid();
        var enrollDate = new DateOnly(2026, 02, 18);

        // Seed мінімально потрібних кодів + політики.
        await using (var db = await testDb.Factory.CreateDbContextAsync())
        {
            await SeedCodesAndPolicyAsync(db, now);
        }

        var episodes = new TimesheetEpisodeRepository(testDb.Factory);
        var entries = new TimesheetEntryRepository(testDb.Factory);
        var policy = new TimesheetPolicyRepository(testDb.Factory);

        // ✅ Використовуємо реальний репозиторій агрегатів (TaskSpans).
        // У цьому тесті task-логіка не повинна активуватись (немає TaskSpan на дату).
        var aggregates = new TimesheetAggregateRepository(testDb.Factory);

        // 1) Enroll -> створює епізод + entry з кодом "Т" на enrollDate
        await episodes.OpenOnEnrollAsync(
            personId: personId,
            enrollDate: enrollDate,
            author: "tester",
            nowUtc: now);

        // Переконаємось, що на enrollDate справді "Т"
        var episodeOnEnroll = await episodes.GetEpisodeOnDateAsync(personId, enrollDate)
            ?? throw new InvalidOperationException("Seed failed: episode not created.");

        var before = await entries.GetActiveEntryOnDateAsync(episodeOnEnroll.Id, personId, enrollDate);
        Assert.NotNull(before);
        Assert.Equal(TimesheetSystemCodes.BaseState, before!.TimesheetCodeDefinition?.Code);

        // Підготуємо handler
        var handler = new TransitionTimesheetStateCommandHandler(
            episodes: episodes,
            entries: entries,
            policy: policy,
            aggregates: aggregates);

        // Візьмемо Id коду "30" з довідника
        Guid readyCodeId;
        await using (var db = await testDb.Factory.CreateDbContextAsync())
        {
            readyCodeId = await db.TimesheetCodes
                .AsNoTracking()
                .Where(x => x.IsActive)
                .Where(x => x.Code == TimesheetSystemCodes.ReadyToCombatTask)
                .Select(x => x.Id)
                .SingleAsync();
        }

        // 2) Same-day transition: 18.02 поставити "30"
        var cmd = new TransitionTimesheetStateCommand(
            PersonId: personId,
            AnchorDate: enrollDate,
            InputDate: enrollDate,
            NextCode: readyCodeId,
            Reference: null,
            Note: null,
            IsCorrection: false,
            Author: "tester",
            NowUtc: now.AddMinutes(1));

        var entryId = await handler.HandleAsync(cmd);

        // Assert (важливо: перевіряємо реальний стан БД)
        await using (var db = await testDb.Factory.CreateDbContextAsync())
        {
            var e = await db.TimesheetEntries
                .AsNoTracking()
                .SingleAsync(x => x.Id == entryId);

            // same-day replace => 1 запис, From = 18.02, To = null, код = "30"
            Assert.Equal(enrollDate, e.From);
            Assert.Null(e.To);
            Assert.Equal(episodeOnEnroll.Id, e.TimesheetId);
            Assert.Equal(personId, e.PersonId);

            var code = await db.TimesheetCodes
                .AsNoTracking()
                .Where(x => x.Id == e.TimesheetCodeDefinitionId)
                .Select(x => x.Code)
                .SingleAsync();

            Assert.Equal(TimesheetSystemCodes.ReadyToCombatTask, code);

            // Додатково: на enrollDate має бути рівно один "активний" entry
            var cnt = await db.TimesheetEntries
                .AsNoTracking()
                .Where(x => !x.IsDeleted)
                .Where(x => x.TimesheetId == episodeOnEnroll.Id)
                .Where(x => x.PersonId == personId)
                .Where(x => x.From <= enrollDate && (!x.To.HasValue || x.To.Value >= enrollDate))
                .CountAsync();

            Assert.Equal(1, cnt);
        }
    }

    //======================================================================
    // Seed helpers
    //======================================================================

    /// <summary>
    /// Сідає мінімальний набір кодів, потрібний handler'у:
    /// "Т" (BaseState), "30" (Ready), "100" (Task).
    /// Також сідає політика переходу "Т → 30" зі StartShiftDays=0,
    /// щоб same-day подія замінювала "Т" одразу на цю ж дату.
    /// </summary>
    private static async Task SeedCodesAndPolicyAsync(AppDbContext db, DateTime nowUtc)
    {
        await db.Database.EnsureCreatedAsync();

        // Якщо тести запускаються в одному процесі з переюзом БД — робимо seed ідемпотентно.
        var any = await db.TimesheetCodes.AsNoTracking().AnyAsync();
        if (any) return;

        var codeT = new TimesheetCodeDefinition
        {
            Id = Guid.NewGuid(),
            Code = TimesheetSystemCodes.BaseState,
            Title = "Base state (Enroll default)",
            Description = null,
            SortOrder = 0,
            Priority = 0,
            IsTerminal = false,
            IsActive = true,
            CreatedBy = "seed",
            CreatedAtUtc = nowUtc
        };

        var code30 = new TimesheetCodeDefinition
        {
            Id = Guid.NewGuid(),
            Code = TimesheetSystemCodes.ReadyToCombatTask,
            Title = "Ready",
            Description = null,
            SortOrder = 1,
            Priority = 0,
            IsTerminal = false,
            IsActive = true,
            CreatedBy = "seed",
            CreatedAtUtc = nowUtc
        };

        var code100 = new TimesheetCodeDefinition
        {
            Id = Guid.NewGuid(),
            Code = TimesheetSystemCodes.DoesTheCombatTask,
            Title = "On task",
            Description = null,
            SortOrder = 2,
            Priority = 0,
            IsTerminal = false,
            IsActive = true,
            CreatedBy = "seed",
            CreatedAtUtc = nowUtc
        };

        db.TimesheetCodes.AddRange(codeT, code30, code100);

        // Allowed transition: T -> 30 (same-day)
        db.TimesheetCodeTransitions.Add(new TimesheetCodeTransition
        {
            Id = Guid.NewGuid(),
            FromCodeId = codeT.Id,
            ToCodeId = code30.Id,
            StartShiftDays = 0,
            CreatedBy = "seed",
            CreatedAtUtc = nowUtc
        });

        await db.SaveChangesAsync();
    }
}
