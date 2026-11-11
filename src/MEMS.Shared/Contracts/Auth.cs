using System.ComponentModel.DataAnnotations;

namespace MEMS.Shared.Contracts;

/// <summary>Body of POST /api/auth/login.</summary>
public class LoginRequest
{
    [Required(ErrorMessage = "Username is required.")]
    [StringLength(50, ErrorMessage = "Username cannot exceed 50 characters.")]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [StringLength(100, ErrorMessage = "Password cannot exceed 100 characters.")]
    public string Password { get; set; } = string.Empty;
}

public class LoginResponse
{
    /// <summary>Send as <c>Authorization: Bearer {accessToken}</c>.</summary>
    public string AccessToken { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;

    /// <summary>The tenant a Municipality Administrator is limited to; null for a Super Admin.</summary>
    public string? TenantSlug { get; set; }
}

public class DashboardDto
{
    public int MunicipalityCount { get; set; }
    public int PositionCount { get; set; }
    public int CandidateCount { get; set; }
}
