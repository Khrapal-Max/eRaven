//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------

namespace eRaven.Domain.Events;

public sealed record PersonStatusSetEvent(
    Guid PersonId,
    Guid StatusId,
    int StatusKindId,
    DateTime OccurredAtUtc,
    short Sequence,
    string? Note,
    string? Author,
    Guid? SourceDocumentId,
    string? SourceDocumentType) : IPersonEvent;
