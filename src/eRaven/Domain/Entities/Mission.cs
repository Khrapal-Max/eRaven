//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// MissionPoint
//-----------------------------------------------------------------------------

using eRaven.Domain.Enums;

namespace eRaven.Domain.Entities;


/// <summary>
/// Клас точки виконання бойового завдання
/// містить інформацію щодо:
/// 
/// - позиційний район
/// - назва точки (може бути відсутня)
/// - тип або типи засобів ураження (може бути відсутній)
/// - режим готовності
/// - мета завдань
/// - активність
/// </summary>
public sealed class Mission
{
    public Guid Id { get; set; }

    public string PositionArea { get; set; } = string.Empty;

    public string? NamePoint { get; set; }

    public string? TypeDrone { get; set; }

    public string Target { get; set; } = default!;

    public MissionMode MissionMode { get; set; }

    public DateOnly CreatedAt { get; set; }

    public DateOnly? ClosedAt { get; set; }

    public string DisplayMission =>
        $"{PositionArea}" +
        $" {NamePoint}" +
        $" {TypeDrone}" +
        $" {Target}" +
        $" {MissionMode}";
}
