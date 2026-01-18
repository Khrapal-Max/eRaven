//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonEventPresenter
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.Person;
using eRaven.Application.EventJson;
using eRaven.Domain.Enums;
using eRaven.Domain.Events.PersonEvents.Info;
using eRaven.Domain.Events.PersonEvents.Move;

namespace eRaven.Application.Presenter;

public sealed class PersonEventPresenter(IEventJson eventJson) : IPersonEventPresenter
{

    private readonly IEventJson _eventJson = eventJson;

    private readonly bool _isVoided = false;


    public PersonEventListItemDto ToListItem(PersonEventDto e)
    {
        var type = e.EventType ?? string.Empty;

        if (type.EndsWith(nameof(PersonCreated), StringComparison.Ordinal))
        {
            var ev = _eventJson.TryDeserialize<PersonCreated>(e.PayloadJson);
            var title = "Створення картки";
            var details = ev is null ? "" : $"РКОНПП: {ev.Personal.Rnokpp}. " +
                $"ПІБ: {ev.Personal.FullName}. " +
                $"Звання: {ev.Rank}. " +
                $"Планова посада: {ev.Position}.";
            return new PersonEventListItemDto(e.Version, e.EventId, e.EffectiveDate, title, details, e.Author, e.OccurredAtUtc,
            _isVoided);
        }

        if (type.EndsWith(nameof(PersonEnrolled), StringComparison.Ordinal))
        {
            var ev = _eventJson.TryDeserialize<PersonEnrolled>(e.PayloadJson);
            var title = "Доданий в табель";
            var details = ev is null ? "" :
                $"Тип зарахування: {EnrollmentKindLabel(ev.Kind)}. " +
                (string.IsNullOrWhiteSpace(ev.Reference) ? "" : $"Посилання: {ev.Reference}. ") +
                $"Підстава: {ev.Reason}. " +
                $"Звання: {ev.Rank}. " +
                $"Посада:: #{ev.PositionSort} {ev.Position}.";
            return new PersonEventListItemDto(e.Version, e.EventId, e.EffectiveDate, title, details, e.Author, e.OccurredAtUtc,
            _isVoided);
        }

        if (type.EndsWith(nameof(PersonExcluded), StringComparison.Ordinal))
        {
            var ev = _eventJson.TryDeserialize<PersonExcluded>(e.PayloadJson);
            var title = "Виключений з табеля";
            var details = ev is null ? "" :
                $"Підстава: {ev.Reason}.";
            return new PersonEventListItemDto(e.Version, e.EventId, e.EffectiveDate, title, details, e.Author, e.OccurredAtUtc,
            _isVoided);
        }

        if (type.EndsWith(nameof(PersonPersonalInfoUpdated), StringComparison.Ordinal))
        {
            var ev = _eventJson.TryDeserialize<PersonPersonalInfoUpdated>(e.PayloadJson);
            var title = "Оновлення персональної інфо";
            var details = ev is null ? "" : $"Оновлено на: {ev.Personal.Rnokpp} " +
                $"{ev.Personal.FullName}" + (string.IsNullOrWhiteSpace(ev.Note) ? "" : $". {ev.Note}");
            return new PersonEventListItemDto(e.Version, e.EventId, e.EffectiveDate, title, details, e.Author, e.OccurredAtUtc,
            _isVoided);
        }

        if (type.EndsWith(nameof(PersonRankChanged), StringComparison.Ordinal))
        {
            var ev = _eventJson.TryDeserialize<PersonRankChanged>(e.PayloadJson);
            var title = "Зміна звання";
            var details = ev is null ? "" : $"На: {ev.Rank}" + (string.IsNullOrWhiteSpace(ev.Note) ? "" : $". {ev.Note}");
            return new PersonEventListItemDto(e.Version, e.EventId, e.EffectiveDate, title, details, e.Author, e.OccurredAtUtc,
            _isVoided);
        }

        if (type.EndsWith(nameof(PersonPositionChanged), StringComparison.Ordinal))
        {
            var ev = _eventJson.TryDeserialize<PersonPositionChanged>(e.PayloadJson);
            var title = "Зміна посади";
            var details = ev is null ? "" : $"На: #{ev.PositionSort}. {(ev.Position ?? "—")}" + (string.IsNullOrWhiteSpace(ev.Note) ? "" : $". {ev.Note}");
            return new PersonEventListItemDto(e.Version, e.EventId, e.EffectiveDate, title, details, e.Author, e.OccurredAtUtc,
            _isVoided);
        }

        if (type.EndsWith(nameof(PersonBzvpChanged), StringComparison.Ordinal))
        {
            var ev = _eventJson.TryDeserialize<PersonBzvpChanged>(e.PayloadJson);
            var title = "Оновлення БЗВП (ВОС/УБД)";
            var details = ev is null ? "" : $"{ev.Bzvp}" + (string.IsNullOrWhiteSpace(ev.Note) ? "" : $". {ev.Note}");
            return new PersonEventListItemDto(e.Version, e.EventId, e.EffectiveDate, title, details, e.Author, e.OccurredAtUtc,
            _isVoided);
        }

        if (type.EndsWith(nameof(PersonWeaponChanged), StringComparison.Ordinal))
        {
            var ev = _eventJson.TryDeserialize<PersonWeaponChanged>(e.PayloadJson);
            var title = "Оновлення зброї";
            var details = ev is null ? "" : (string.IsNullOrWhiteSpace(ev.Weapon) ? "Очищено" : $"Видано: {ev.Weapon}");
            return new PersonEventListItemDto(e.Version, e.EventId, e.EffectiveDate, title, details, e.Author, e.OccurredAtUtc,
            _isVoided);
        }

        if (type.EndsWith(nameof(PersonCallsignChanged), StringComparison.Ordinal))
        {
            var ev = _eventJson.TryDeserialize<PersonCallsignChanged>(e.PayloadJson);
            var title = "Зміна позивного";
            var details = ev is null ? "" : (string.IsNullOrWhiteSpace(ev.Callsign) ? "Очищено" : $"Новий позивний: {ev.Callsign}");
            return new PersonEventListItemDto(e.Version, e.EventId, e.EffectiveDate, title, details, e.Author, e.OccurredAtUtc,
            _isVoided);
        }

        // fallback
        return new PersonEventListItemDto(
            e.Version, e.EventId, e.EffectiveDate,
            Title: "Подія",
            Details: type,
            e.Author,
            e.OccurredAtUtc,
            _isVoided);
    }

    private static string EnrollmentKindLabel(EnrollmentKind kind) => kind switch
    {
        EnrollmentKind.Unit => "Доданий в табель, згідно штату",
        EnrollmentKind.AttachedByList => "Доданий в табель, згідно наказу",
        EnrollmentKind.AttachedByOrder => "Доданий в табель, згідно БР",
        _ => kind.ToString()
    };
}