using eRaven.Application.Commands;
using eRaven.Application.Commands.Excel;
using eRaven.Infrastructure.Repositories.PersonRepository;
using Microsoft.EntityFrameworkCore;

public sealed class BootstrapPersonsCommandHandler(
    IPersonRepository repo,
    ITimesheetLifecycleRepository timesheetRepo,
    ILogger<BootstrapPersonsCommandHandler> log)
        : ICommandHandler<BootstrapPersonsCommand, BootstrapPersonsResult>
{
    private readonly IPersonRepository _repo = repo;
    private readonly ITimesheetLifecycleRepository _timesheet = timesheetRepo;
    private readonly ILogger<BootstrapPersonsCommandHandler> _log = log;

    public async Task<BootstrapPersonsResult> HandleAsync(
        BootstrapPersonsCommand command,
        CancellationToken ct = default)
    {
        if (command.Rows is null || command.Rows.Count == 0)
            return new BootstrapPersonsResult(0, 0, 0, []);

        var author = (command.Author ?? "").Trim();
        if (string.IsNullOrWhiteSpace(author))
            throw new ArgumentException($"Author is required.{nameof(command.Author)}");

        // 1) Дублікати РНОКПП у файлі — не імпортуємо ці рядки
        var dupSet = command.Rows
            .GroupBy(r => NormalizeRnokpp(r.Rnokpp))
            .Where(g => g.Key.Length > 0 && g.Count() > 1)
            .Select(g => g.Key)
            .ToHashSet(StringComparer.Ordinal);

        var errors = new List<BootstrapPersonsError>();
        var created = 0;
        var skipped = 0;

        if (dupSet.Count > 0)
        {
            foreach (var row in command.Rows.Where(r => dupSet.Contains(NormalizeRnokpp(r.Rnokpp))))
            {
                skipped++;
                errors.Add(new BootstrapPersonsError(row.RowNumber, row.Rnokpp, "Дублікат РНОКПП у файлі."));
            }
        }

        var candidates = command.Rows
            .Where(r => !dupSet.Contains(NormalizeRnokpp(r.Rnokpp)))
            .ToList();

        // 2) Pre-check: що вже є в системі
        var rnokpps = candidates
            .Select(r => NormalizeRnokpp(r.Rnokpp))
            .Where(x => x.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        var existing = await _repo.GetExistingRnokppsAsync(rnokpps, ct);

        // 3) Імпорт: по рядках
        foreach (var row in candidates)
        {
            var rn = NormalizeRnokpp(row.Rnokpp);

            if (existing.Contains(rn))
            {
                skipped++;
                errors.Add(new BootstrapPersonsError(row.RowNumber, row.Rnokpp, "РНОКПП вже існує в системі."));
                continue;
            }

            try
            {
                // 1) створили персону + зарахували
                var personId = await _repo.BootstrapCreateAndEnrollAsync(row, author, command.NowUtc, ct);

                // 2) відкрили табель: шкала(и) + дефолтний Main=30 з дати зарахування
                await _timesheet.OpenOnEnrollAsync(personId, row.EnrollDate, author, command.NowUtc, ct);

                created++;
            }
            catch (DbUpdateException ex)
            {
                skipped++;
                errors.Add(new BootstrapPersonsError(row.RowNumber, row.Rnokpp, "Помилка БД: " + ex.GetBaseException().Message));
                _log.LogWarning(ex, "Bootstrap import DB error on row {Row} rnokpp {Rnokpp}", row.RowNumber, row.Rnokpp);
            }
            catch (Exception ex)
            {
                skipped++;
                errors.Add(new BootstrapPersonsError(row.RowNumber, row.Rnokpp, ex.Message));
                _log.LogWarning(ex, "Bootstrap import error on row {Row} rnokpp {Rnokpp}", row.RowNumber, row.Rnokpp);
            }
        }

        return new BootstrapPersonsResult(
            TotalRows: command.Rows.Count,
            CreatedCount: created,
            SkippedCount: skipped,
            Errors: errors
        );

        static string NormalizeRnokpp(string? s) => (s ?? "").Trim();
    }
}