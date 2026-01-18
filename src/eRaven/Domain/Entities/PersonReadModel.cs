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

    // EnrollmentInfo (поточне / останнє)
    public EnrollmentKind? EnrollmentKind { get; set; }   // <-- nullable
    public string? EnrollmentReference { get; set; }

    // персональні дані (поточні)
    public string Rnokpp { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }

    public string FullName { get; set; } = string.Empty;

    // професійні дані (поточні)
    public string? Rank { get; set; }
    public int? PositionSort { get; set; }
    public string? Position { get; set; }

    // військові дані (поточні)
    public string? Bzvp { get; set; }
    public string? Weapon { get; set; }
    public string? Callsign { get; set; }

    public DateOnly? EnrolledAt { get; set; }
    public DateOnly? ExcludedAt { get; set; }

    public long Version { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}