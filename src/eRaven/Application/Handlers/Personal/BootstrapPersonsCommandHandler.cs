//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// BootstrapPersonsCommandHandler
//-----------------------------------------------------------------------------

using eRaven.Application.Abstractions.PersonRepository;
using eRaven.Application.Abstractions.TimesheetRepository;
using eRaven.Application.Commands;
using eRaven.Application.Commands.Excel;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Application.Handlers.Personal;

/// <summary>
/// Command handler: імпорт (bootstrap) осіб з Excel.
///
/// <para>Операції:</para>
/// <list type="bullet">
/// <item><description>створення особи + зарахування</description></item>
/// <item><description>відкриття епізоду табеля (ідемпотентно) та стартовий main-код з дати зарахування</description></item>
/// </list>
/// </summary>
public sealed class BootstrapPersonsCommandHandler(
    IPersonRepository repo,
    ITimesheetEpisodeRepository timesheetRepo,
    ILogger<BootstrapPersonsCommandHandler> log)
        : ICommandHandler<BootstrapPersonsCommand, BootstrapPersonsResult>
{
    private readonly IPersonRepository _repo = repo;
    private readonly ITimesheetEpisodeRepository _timesheet = timesheetRepo;
    private readonly ILogger<BootstrapPersonsCommandHandler> _log = log;

    /// <inheritdoc />
    public async Task<BootstrapPersonsResult> HandleAsync(
        BootstrapPersonsCommand command,
        CancellationToken ct = default)
    {
        if (command.Rows is null || command.Rows.Count == 0)
            return new BootstrapPersonsResult(0, 0, 0, []);

        var author = (command.Author ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(author))
            throw new ArgumentException($"Author is required. {nameof(command.Author)}");

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

                // 2) відкрили табель: епізод + дефолтний main-код з дати зарахування
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
            Errors: errors);
    }

    private static string NormalizeRnokpp(string? s) => (s ?? string.Empty).Trim();
}
