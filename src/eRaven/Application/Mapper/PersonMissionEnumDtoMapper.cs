//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonMissionEnumDtoMapper
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.Enums;
using eRaven.Domain.Enums;

namespace eRaven.Application.Mapper;

/// <summary>
/// Мапер enum-ів між Domain та Application DTO.
/// Важливо: EnrollmentKindDto має інші numeric values ніж Domain EnrollmentKind.
/// Тому мапінг робимо через switch, без кастів.
/// </summary>
public static class PersonMissionEnumDtoMapper
{
    // ------------------------------
    // EnrollmentKind
    // ------------------------------
    public static EnrollmentKind ToDomain(EnrollmentKindDto dto)
        => dto switch
        {
            EnrollmentKindDto.Unit => EnrollmentKind.Unit,
            EnrollmentKindDto.AttachedByOrder => EnrollmentKind.AttachedByOrder,
            EnrollmentKindDto.AttachedByList => EnrollmentKind.AttachedByList,
            _ => EnrollmentKind.Unit
        };

    public static EnrollmentKind? ToDomain(EnrollmentKindDto? dto)
        => dto is null ? null : ToDomain(dto.Value);

    public static EnrollmentKindDto ToDto(EnrollmentKind domain)
        => domain switch
        {
            EnrollmentKind.Unit => EnrollmentKindDto.Unit,
            EnrollmentKind.AttachedByOrder => EnrollmentKindDto.AttachedByOrder,
            EnrollmentKind.AttachedByList => EnrollmentKindDto.AttachedByList,
            _ => EnrollmentKindDto.Unknown
        };

    public static EnrollmentKindDto? ToDto(EnrollmentKind? domain)
        => domain is null ? null : ToDto(domain.Value);

    // ------------------------------
    // PersonLifecycle
    // ------------------------------
    public static PersonLifecycle ToDomain(PersonLifecycleDto dto)
        => dto switch
        {
            PersonLifecycleDto.Reserved => PersonLifecycle.Reserved,
            PersonLifecycleDto.Enrolled => PersonLifecycle.Enrolled,
            _ => PersonLifecycle.Reserved
        };

    public static PersonLifecycle? ToDomain(PersonLifecycleDto? dto)
        => dto is null ? null : ToDomain(dto.Value);

    public static PersonLifecycleDto ToDto(PersonLifecycle domain)
        => domain switch
        {
            PersonLifecycle.Reserved => PersonLifecycleDto.Reserved,
            PersonLifecycle.Enrolled => PersonLifecycleDto.Enrolled,
            _ => PersonLifecycleDto.Reserved
        };

    // ------------------------------
    // MissionMode
    // ------------------------------
    public static MissionMode ToDomain(MissionModeDto dto)
        => dto switch
        {
            MissionModeDto.Day => MissionMode.Day,
            MissionModeDto.Night => MissionMode.Night,
            MissionModeDto.FullTime => MissionMode.FullTime,
            _ => MissionMode.Day
        };

    public static MissionMode? ToDomain(MissionModeDto? dto)
        => dto is null ? null : ToDomain(dto.Value);

    public static MissionModeDto ToDto(MissionMode domain)
        => domain switch
        {
            MissionMode.Day => MissionModeDto.Day,
            MissionMode.Night => MissionModeDto.Night,
            MissionMode.FullTime => MissionModeDto.FullTime,
            _ => MissionModeDto.Day
        };
}
