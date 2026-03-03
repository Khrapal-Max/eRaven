//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CombatTaskEnumMapper
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.Enums;
using eRaven.Domain.Enums;

namespace eRaven.Application.Mapper;

internal static class CombatTaskEnumMapper
{
    public static DocumentStatusDto MapStatus(DocumentStatus status)
        => status switch
        {
            DocumentStatus.Active => DocumentStatusDto.Active,
            DocumentStatus.Canceled => DocumentStatusDto.Canceled,
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
        };

    public static DocumentStatus? ToDomainStatus(DocumentStatusDto? status)
        => status switch
        {
            DocumentStatusDto.Active => DocumentStatus.Active,
            DocumentStatusDto.Canceled => DocumentStatus.Canceled,
            _ => null
        };

    public static CombatTaskDetailsKindDto MapKind(CombatTaskDetailsKind kind)
       => kind switch
       {
           CombatTaskDetailsKind.Start => CombatTaskDetailsKindDto.Start,
           CombatTaskDetailsKind.End => CombatTaskDetailsKindDto.End,
           _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
       };

    public static CombatTaskDetailsKind? ToDomainKind(CombatTaskDetailsKindDto? kind)
        => kind switch
        {
            CombatTaskDetailsKindDto.Start => CombatTaskDetailsKind.Start,
            CombatTaskDetailsKindDto.End => CombatTaskDetailsKind.End,
            _ => null
        };
}
