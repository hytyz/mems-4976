using System.ComponentModel.DataAnnotations;
using MEMS.Shared.Validation;

namespace MEMS.Shared.Contracts;

public class PositionDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SeatsAvailable { get; set; }
    public int MunicipalityId { get; set; }
    public string MunicipalityName { get; set; } = string.Empty;
    public int CandidateCount { get; set; }
}

/// <summary>Body of POST and PUT /api/{tenant}/positions. The tenant comes from the URL, never the body.</summary>
public class PositionInput
{
    [Required(ErrorMessage = "Position name is required.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Position name must be between 2 and 100 characters.")]
    public string Name { get; set => field = value?.Trim() ?? string.Empty; } = string.Empty;

    [Required(ErrorMessage = "Number of available seats is required.")]
    [Range(1, 100, ErrorMessage = "Seats available must be between 1 and 100.")]
    [Display(Name = "Seats available")]
    public int? SeatsAvailable { get; set; } = 1;
}

public class EndorsementDto
{
    public int Id { get; set; }
    public string EndorserName { get; set; } = string.Empty;
    public string? Organization { get; set; }
    public string? Text { get; set; }
}

public class CandidateDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Profile { get; set; }

    /// <summary>Id of the photograph, or null when none was uploaded.</summary>
    public Guid? PhotoImageId { get; set; }

    public string? Website { get; set; }
    public string? Email { get; set; }
    public string? VideoUrl { get; set; }
    public string? SocialX { get; set; }
    public string? SocialFacebook { get; set; }
    public string? SocialInstagram { get; set; }
    public string? SocialLinkedIn { get; set; }
    public DateTime DateCreated { get; set; }

    public int PositionId { get; set; }
    public string PositionName { get; set; } = string.Empty;
    public int SeatsAvailable { get; set; }

    public int MunicipalityId { get; set; }
    public string MunicipalityName { get; set; } = string.Empty;
    public string MunicipalitySlug { get; set; } = string.Empty;

    public List<EndorsementDto> Endorsements { get; set; } = [];
}

/// <summary>Body of POST and PUT /api/{tenant}/candidates.</summary>
public class CandidateInput
{
    [Required(ErrorMessage = "Candidate name is required.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Candidate name must be between 2 and 100 characters.")]
    public string Name { get; set => field = value?.Trim() ?? string.Empty; } = string.Empty;

    [StringLength(2000, ErrorMessage = "Biography cannot exceed 2000 characters.")]
    [Display(Name = "Biography")]
    public string? Profile { get; set => field = Text.NullIfBlank(value); }

    [HttpUrl]
    [StringLength(300, ErrorMessage = "Website URL cannot exceed 300 characters.")]
    public string? Website { get; set => field = Text.NullIfBlank(value); }

    [EmailAddress(ErrorMessage = "Email must be a valid email address.")]
    [StringLength(254, ErrorMessage = "Email cannot exceed 254 characters.")]
    public string? Email { get; set => field = Text.NullIfBlank(value); }

    [HttpUrl]
    [StringLength(300, ErrorMessage = "Video URL cannot exceed 300 characters.")]
    [Display(Name = "Video URL")]
    public string? VideoUrl { get; set => field = Text.NullIfBlank(value); }

    [HttpUrl]
    [StringLength(300, ErrorMessage = "X URL cannot exceed 300 characters.")]
    [Display(Name = "X (Twitter) URL")]
    public string? SocialX { get; set => field = Text.NullIfBlank(value); }

    [HttpUrl]
    [StringLength(300, ErrorMessage = "Facebook URL cannot exceed 300 characters.")]
    [Display(Name = "Facebook URL")]
    public string? SocialFacebook { get; set => field = Text.NullIfBlank(value); }

    [HttpUrl]
    [StringLength(300, ErrorMessage = "Instagram URL cannot exceed 300 characters.")]
    [Display(Name = "Instagram URL")]
    public string? SocialInstagram { get; set => field = Text.NullIfBlank(value); }

    [HttpUrl]
    [StringLength(300, ErrorMessage = "LinkedIn URL cannot exceed 300 characters.")]
    [Display(Name = "LinkedIn URL")]
    public string? SocialLinkedIn { get; set => field = Text.NullIfBlank(value); }

    /// <summary>The position the candidate is running for. It must belong to the tenant in the URL.</summary>
    [Required(ErrorMessage = "Choose the position this candidate is running for.")]
    [Range(1, int.MaxValue, ErrorMessage = "Choose the position this candidate is running for.")]
    [Display(Name = "Position")]
    public int? PositionId { get; set; }
}

/// <summary>Query string of GET /api/{tenant}/candidates.</summary>
public class CandidateQuery
{
    public const int DefaultPageSize = 9;
    public const int MaxPageSize = 100;

    /// <summary>Matches part of the candidate's name.</summary>
    [StringLength(100, ErrorMessage = "Search text cannot exceed 100 characters.")]
    public string? Search { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "positionId must be a positive number.")]
    public int? PositionId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "page must be 1 or greater.")]
    public int Page { get; set; } = 1;

    [Range(1, MaxPageSize, ErrorMessage = "pageSize must be between 1 and 100.")]
    public int PageSize { get; set; } = DefaultPageSize;
}

public class PagedResult<T>
{
    public List<T> Items { get; set; } = [];
    public int Page { get; set; } = 1;
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; } = 1;
}

/// <summary>Returned after an image upload.</summary>
public class ImageUploadResult
{
    public Guid ImageId { get; set; }
}
