//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetCodeDefinition
//-----------------------------------------------------------------------------

namespace eRaven.Domain.Entities;

using eRaven.Domain.Enums;

/// <summary>
/// Довідник табельних кодів (що існує в системі).
/// Приклади: "30", "Т", "100", "ЛХ"...
/// </summary>
public sealed class TimesheetCodeDefinition
{
    public Guid Id { get; set; }

    /// <summary>Стабільний код (коротке позначення).</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Назва для користувача.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Опціонально: розширений опис (для довідки/підказок).</summary>
    public string? Description { get; set; }

    /// <summary>Порядок у списках/матрицях.</summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Вага/пріоритет для випадків, коли події попадають на одну дату.
    /// </summary>
    public int Priority { get; set; }

    /// <summary>Код є “кінцевим”.</summary>
    public bool IsTerminal { get; set; }

    /// <summary>Чи активний (можна використовувати).</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Роль коду (system/transition/emergency).</summary>
    public RoleCode RoleCode { get; set; } = RoleCode.TransitionCode;

    /// <summary>Семантичний стиль відображення в UI.</summary>
    public TimesheetUiStyle UiStyle { get; set; } = TimesheetUiStyle.Warning;

    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }

    public string? UpdatedBy { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}