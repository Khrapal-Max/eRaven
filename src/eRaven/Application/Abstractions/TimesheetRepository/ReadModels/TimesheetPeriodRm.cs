//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetPeriodRm
//-----------------------------------------------------------------------------

namespace eRaven.Application.Abstractions.TimesheetRepository.ReadModels;

/// <summary>
/// Табельний період для 1 особи: готова "матриця" (список днів).
/// <para>
/// Дані про особу (ПІБ/РНОКПП/звання/посада) не входять у цю модель.
/// </para>
/// </summary>
public sealed record TimesheetPeriodRm(
    Guid PersonId,
    IReadOnlyList<TimesheetDayRm> Days);
