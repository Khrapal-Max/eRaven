//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ITypeDrone
//-----------------------------------------------------------------------------

namespace eRaven.Application.Catalogs.CombatTask.TypeDrones;

/// <summary>
/// Каталог дронів та їх класіфікація
/// - використовується в CombatTask documents (Доукменти планування)
/// 
/// Поточні типи:
/// </summary>
public interface ITypeDroneCatalog
{
    IReadOnlyList<TypeDroneOption> GetActive();
}