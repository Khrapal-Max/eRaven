//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// GetPersonalHistoriesQueryHandlers
//-----------------------------------------------------------------------------

using eRaven.Application.Abstractions.PersonRepository;
using eRaven.Application.DTOs.Person;
using eRaven.Application.EventJson;
using eRaven.Application.Presenter;
using eRaven.Application.Queries;
using eRaven.Application.Queries.Personal;
using eRaven.Domain.Events.PersonEvents.Info;

namespace eRaven.Application.Handlers.Personal;

/// <summary>
/// Повертає евенти (події) по картоці людини
/// </summary>
public sealed class GetPersonHistoryQueryHandler(
    IPersonRepository repo,
    IPersonEventPresenter presenter,
    IEventJson eventJson)
    : IQueryHandler<GetPersonHistoryQuery, IReadOnlyList<PersonEventListItemDto>>
{
    private readonly IPersonRepository _repo = repo;
    private readonly IPersonEventPresenter _presenter = presenter;
    private readonly IEventJson _eventJson = eventJson;

    /// <inheritdoc />
    public async Task<IReadOnlyList<PersonEventListItemDto>> HandleAsync(
        GetPersonHistoryQuery query,
        CancellationToken ct = default)
    {
        var raw = await _repo.GetHistoryAsync(query.PersonId, ct);

        if (raw.Count == 0)
            return [];

        var events = raw.Select(x => new PersonEventDto(
            x.Version,
            x.EventId,
            x.EventType,
            x.PayloadJson,
            x.Author,
            x.OccurredAtUtc,
            x.EffectiveDate))
        .ToList();

        // 1) зібрати всі TargetEventId, які були void
        var voidedTargets = new HashSet<Guid>();

        foreach (var e in events)
        {
            if (!IsVoidEvent(e))
                continue;

            var v = _eventJson.TryDeserialize<PersonEventVoided>(e.PayloadJson);
            if (v is null)
                continue;

            // якщо void-нули якусь подію — вона "неактивна" в кар'єрі
            voidedTargets.Add(v.TargetEventId);
        }

        // 2) сформувати список: void-події НЕ показуємо, але мітимо цільові як IsVoided=true
        var list = new List<PersonEventListItemDto>(raw.Count);

        foreach (var e in events)
        {
            if (IsVoidEvent(e))
                continue; // ✅ не показуємо саму подію відміни

            var item = _presenter.ToListItem(e);

            if (voidedTargets.Contains(e.EventId))
                item = item with { IsVoided = true };

            list.Add(item);
        }

        // UI зараз орієнтується на Version — залишимо так
        return [.. list.OrderByDescending(x => x.Version)];
    }

    private static bool IsVoidEvent(PersonEventDto e)
    => (e.EventType ?? string.Empty)
        .EndsWith(nameof(PersonEventVoided), StringComparison.OrdinalIgnoreCase);
}