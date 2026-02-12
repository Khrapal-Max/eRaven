//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetCodeTransition
//-----------------------------------------------------------------------------

namespace eRaven.Domain.Entities;

/// <summary>
/// Правило: чи дозволений перехід між кодами і як трактувати дату події.
/// </summary>
public sealed class TimesheetCodeTransition
{
    public Guid Id { get; set; }

    public Guid FromCodeId { get; set; }
    public TimesheetCodeDefinition FromCode { get; set; } = null!;

    public Guid ToCodeId { get; set; }
    public TimesheetCodeDefinition ToCode { get; set; } = null!;

    /// <summary>
    /// 0 = "З дати події" (новий код активний цього ж дня)
    /// 1 = "Ще поточний" (цей день ще старий код, новий з наступного дня)
    /// </summary>
    public int StartShiftDays { get; set; }

    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}
