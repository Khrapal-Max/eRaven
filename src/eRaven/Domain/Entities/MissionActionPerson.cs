//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// MissionActionPerson
//-----------------------------------------------------------------------------

namespace eRaven.Domain.Entities;

/// <summary>
/// Участь особи у конкретній дії документа + "снапшот" полів особи на момент внесення/фіксації цієї дії.
/// 
/// Це ключовий компроміс:
/// - документ не "пливе" при подальших змінах картки особи;
/// - у межах одного документа різні дії можуть мати різні стани особи (через Sequence).
/// </summary>
public sealed class MissionActionPerson
{
    /// <summary>
    /// Дія документа (FK на <see cref="MissionAction"/>).
    /// </summary>
    public Guid ActionId { get; set; }

    /// <summary>
    /// Ідентифікатор особи (стабільний).
    /// </summary>
    public Guid PersonId { get; set; }

    /// <summary>
    /// РНОКПП (зафіксоване значення на дію).
    /// </summary>
    public string RNOKPP { get; set; } = string.Empty;

    /// <summary>
    /// ПІБ (зафіксоване значення на дію).
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Звання (зафіксоване значення на дію).
    /// </summary>
    public string Rank { get; set; } = string.Empty;

    /// <summary>
    /// Посада (зафіксоване значення на дію).
    /// </summary>
    public string Position { get; set; } = string.Empty;

    /// <summary>
    /// Зброя (зафіксоване значення на дію).
    /// </summary>
    public string Weapon { get; set; } = string.Empty;

    /// <summary>
    /// Позивний (зафіксоване значення на дію).
    /// </summary>
    public string Callsign { get; set; } = string.Empty;
}
