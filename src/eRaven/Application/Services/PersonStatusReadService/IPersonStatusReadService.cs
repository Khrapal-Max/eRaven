//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// IPersonStatusReadService
//-----------------------------------------------------------------------------

using eRaven.Domain.Models;

namespace eRaven.Application.Services.PersonStatusReadService;

public interface IPersonStatusReadService
{
    /// <summary>Повертає активний статус на конкретний момент (UTC).</summary>
    Task<PersonStatus?> GetActiveOnDateAsync(Guid personId, DateTime endOfDayUtc, CancellationToken ct = default);

    /// <summary>Повертає статус на дату (UTC) для кожної особи.</summary>
    Task<IReadOnlyDictionary<Guid, PersonStatus?>> ResolveOnDateAsync(
        IEnumerable<Guid> personIds, DateTime dayUtc, CancellationToken ct = default);

    /// <summary>Повертає статус на дату (UTC) для однієї особи.</summary>
    Task<PersonStatus?> ResolveOnDateAsync(
        Guid personId, DateTime dayUtc, CancellationToken ct = default);

    /// <summary>Повертає першу дату появи особи (присутність або призначення).</summary>
    Task<DateTime?> GetFirstPresenceUtcAsync(Guid personId, CancellationToken ct = default);

    /// <summary>Повертає довідковий статус за кодом (без урахування регістру).</summary>
    Task<StatusKind?> GetByCodeAsync(string code, CancellationToken ct = default);

    /// <summary>Повертає службовий статус «не присутній» (код «нб», без урахування регістру).</summary>
    Task<StatusKind?> ResolveNotPresentAsync(CancellationToken ct = default);
  
    /// <summary>Повертає впорядковану історію статусів для модальних переглядів.</summary>
    Task<IReadOnlyList<PersonStatus>> OrderForHistoryAsync(Guid personId, CancellationToken ct = default);
    /// <summary>
    /// Матриця для табеля: для кожної особи — масив статусів за всі дні місяця
    /// (довжина = daysInMonth; індекс 0 відповідає 1-му дню).
    /// Вхід — рік/місяць у “локалі машини” (UI робить локаль, сервіс працює від UTC).
    /// </summary>
    Task<IReadOnlyDictionary<Guid, PersonMonthStatus>> ResolveMonthAsync(
        IEnumerable<Guid> personIds, int yearLocal, int monthLocal, CancellationToken ct = default);
}