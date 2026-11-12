using Microsoft.AspNetCore.Identity;

namespace MEMS.Data.Entities;

/// <summary>
/// A municipality. Each municipality is one tenant: every other row in the system
/// (positions, candidates, images, administrator accounts) points back to it.
/// </summary>
public class Municipality
{
    public int MunicipalityId { get; set; }
    public string Name { get; set; } = string.Empty;

    /// <summary>URL segment that identifies the tenant, e.g. "burnaby". Unique.</summary>
    public string Slug { get; set; } = string.Empty;

    public string Province { get; set; } = string.Empty;
    public DateOnly ElectionDate { get; set; }
    public string? Description { get; set; }
    public string? Tagline { get; set; }
    public string ThemeColor { get; set; } = "#1d4e89";

    public Guid? LogoImageId { get; set; }
    public StoredImage? Logo { get; set; }

    public ICollection<Position> Positions { get; set; } = new List<Position>();
    public ICollection<Candidate> Candidates { get; set; } = new List<Candidate>();
    public ICollection<ApplicationUser> Administrators { get; set; } = new List<ApplicationUser>();
}

public class Position
{
    public int PositionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SeatsAvailable { get; set; }

    public int MunicipalityId { get; set; }
    public Municipality? Municipality { get; set; }

    public ICollection<Candidate> Candidates { get; set; } = new List<Candidate>();
}

public class Candidate
{
    public int CandidateId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Profile { get; set; }
    public string? Website { get; set; }
    public string? Email { get; set; }
    public string? VideoUrl { get; set; }
    public string? SocialX { get; set; }
    public string? SocialFacebook { get; set; }
    public string? SocialInstagram { get; set; }
    public string? SocialLinkedIn { get; set; }
    public DateTime DateCreated { get; set; } = DateTime.UtcNow;

    public Guid? PhotoImageId { get; set; }
    public StoredImage? Photo { get; set; }

    public int PositionId { get; set; }
    public Position? Position { get; set; }

    /// <summary>
    /// The tenant. Always equal to the position's municipality: the database enforces
    /// that with a composite foreign key (PositionId, MunicipalityId) → Positions.
    /// </summary>
    public int MunicipalityId { get; set; }
    public Municipality? Municipality { get; set; }

    public ICollection<Endorsement> Endorsements { get; set; } = new List<Endorsement>();
}

public class Endorsement
{
    public int EndorsementId { get; set; }
    public string EndorserName { get; set; } = string.Empty;
    public string? Organization { get; set; }
    public string? Text { get; set; }

    public int CandidateId { get; set; }
    public Candidate? Candidate { get; set; }
}

/// <summary>
/// An uploaded logo or candidate photograph. Stored in SQL Server so that the Admin app
/// (which uploads) and the API (which serves) need no shared disk.
/// </summary>
public class StoredImage
{
    public Guid StoredImageId { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public byte[] Data { get; set; } = [];
    public int Length { get; set; }
    public DateTime UploadedUtc { get; set; } = DateTime.UtcNow;

    public int MunicipalityId { get; set; }
    public Municipality? Municipality { get; set; }
}

/// <summary>
/// An administrator account. A Municipality Administrator has exactly one
/// <see cref="MunicipalityId"/>; a Super Admin has none.
/// </summary>
public class ApplicationUser : IdentityUser
{
    public int? MunicipalityId { get; set; }
    public Municipality? Municipality { get; set; }
}
