//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetCodeDto
//-----------------------------------------------------------------------------

namespace eRaven.Application.DTOs.Timesheet;

/// <summary>
/// Запис кода табеля обліку часу
/// </summary>
public sealed record TimesheetCodeDto(
    Guid Id,
    string Code,
    string Title,
    string? Description,
    int SortOrder,
    int Priority,
    bool IsTerminal,
    bool IsActive
);