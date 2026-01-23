//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ITimesheetLifecycleRepository
//-----------------------------------------------------------------------------


namespace eRaven.Infrastructure.Repositories.TimesheetRepository;

public interface ITimesheetLifecycleRepository
{
    // створити шкали (Main/Task) + перший запис Main=30 (open-ended), якщо ще нема
    Task OpenOnEnrollAsync(Guid personId, DateOnly enrollDate, string author, DateTime nowUtc, CancellationToken ct = default);

    // тільки перевірка (без змін): на дату closeTo Main має бути 30 або РОЗПОР
    Task ValidateCanCloseOnExcludeAsync(Guid personId, DateOnly closeTo, CancellationToken ct = default);

    // закрити шкали на closeTo + обрізати записи до closeTo + soft-delete майбутні
    Task CloseOnExcludeAsync(Guid personId, DateOnly closeTo, string reason, string author, DateTime nowUtc, CancellationToken ct = default);
}
