//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// StatusKind
//-----------------------------------------------------------------------------

namespace eRaven.Domain.Models;

/// <summary>
/// Статус
/// </summary>
public class StatusKind
{
    public int Id { get; set; }

    /// <summary>
    /// Назва статуса
    /// </summary>
    public string Name { get; set; } = default!;

    /// <summary>
    /// Визначення статуса - скорочення
    /// </summary>
    public string Code { get; set; } = default!;

    /// <summary>
    /// Пріоритет
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// Стан - активнии або відмінений
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Автор
    /// </summary>
    public string? Author { get; set; }

    /// <summary>
    /// Дата та час останбої зміни
    /// </summary>
    public DateTime Modified { get; set; }
}
