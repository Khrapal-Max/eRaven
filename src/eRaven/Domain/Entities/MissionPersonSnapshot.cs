//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// MissionPersonSnapshot
//-----------------------------------------------------------------------------

namespace eRaven.Domain.Entities;

/// <summary>
/// Знімок особи на момент проведення документа з планування
/// 
/// Містить:
/// - ИД документа
/// - ИД особи
/// - Дата знімка
/// - Дані особи
/// </summary>
public class MissionPersonSnapshot
{
    public Guid Id { get; set; }

    public Guid DocumentId { get; set; }

    public DateOnly CreatedAt { get; set; }

    public Guid PersonId { get; set; }

    public string RNOKPP { get; set; } = default!;

    public string FullName { get; set; } = default!;

    public string Rank { get; set; } = default!;

    public string Position { get; set; } = default!;

    public string Weapon { get; set; } = default!;

    public string Callsign { get; set; } = default!;
}
