//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonEventRecord (Infrastructure / Persistence model)
//-----------------------------------------------------------------------------

namespace eRaven.Domain.Entities;

public sealed class PersonEventRecord
{
    public Guid EventId { get; set; }
    public Guid AggregateId { get; set; }

    /// <summary>Порядковий номер події для агрегата (монотонний)</summary>
    public long Version { get; set; }

    /// <summary>Дискретизатор типу події (наприклад "PersonRankChanged")</summary>
    public string EventType { get; set; } = string.Empty;

    public string PayloadJson { get; set; } = "{}";

    public string Author { get; set; } = string.Empty;
    public DateTime OccurredAtUtc { get; set; }

    /// <summary>
    /// Для швидких “стан на дату” по кар'єрним подіям.
    /// Null для подій без EffectiveDate (CandidateCreated, PersonalInfoUpdated, Voided, etc.)
    /// </summary>
    public DateOnly? EffectiveDate { get; set; }
}
