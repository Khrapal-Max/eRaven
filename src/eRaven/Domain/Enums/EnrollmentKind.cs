//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// EnrollmentKind
//-----------------------------------------------------------------------------

namespace eRaven.Domain.Enums;

/// <summary>
/// Тип зарахування/врахування особи у підрозділі.
/// </summary>
public enum EnrollmentKind
{
    /// <summary>
    /// Рекрут
    /// </summary>
    Recruit = 0,

    /// <summary>
    /// Зарахований у підрозділ (штатний).
    /// </summary>
    Unit = 1,

    /// <summary>
    /// Приряджений (по наказу).
    /// </summary>
    AttachedByOrder = 2,

    /// <summary>
    /// Приряджений (по списку).
    /// </summary>
    AttachedByList = 3
}
