//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonReadModel
//-----------------------------------------------------------------------------

using eRaven.Domain.Enums;

namespace eRaven.Domain.Entities;

public sealed class PersonReadModel
{
    public Guid Id { get; set; }

    public PersonLifecycle Lifecycle { get; set; }

    // EnrollmentInfo (поточне)
    public EnrollmentKind EnrollmentKind { get; set; } = EnrollmentKind.Unit;
    public string? EnrollmentReference { get; set; }

    // PersonalInfo (поточне)
    public string Rnokpp { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }

    public string FullName { get; set; } = string.Empty;

    // Key points (поточне)
    public string? Rank { get; set; }

    public Guid? PlannedPositionUnitId { get; set; }
    public string? PlannedPosition { get; set; }

    public Guid? PositionUnitId { get; set; }
    public string? Position { get; set; }

    public Guid? TemporaryPositionUnitId { get; set; }
    public string? TemporaryPosition { get; set; }

    public string? Bzvp { get; set; }
    public string? Weapon { get; set; }
    public string? Callsign { get; set; }

    public DateOnly? EnrolledAt { get; set; }
    public DateOnly? ExcludedAt { get; set; }

    /// <summary>Для інкрементального апдейту (ідемпотентність проектора)</summary>
    public long Version { get; set; }

    public DateTime UpdatedAtUtc { get; set; }
}
