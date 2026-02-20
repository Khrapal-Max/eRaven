//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// EnrollmentKindDto
//-----------------------------------------------------------------------------

namespace eRaven.Application.DTOs.Enums;

/// <summary>
/// DTO типу зарахування людин в табелі.
/// </summary>
public enum EnrollmentKindDto : byte
{
    Unknown = 0,
    Unit = 1,
    AttachedByList = 2,
    AttachedByOrder = 3
}