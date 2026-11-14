using System.ComponentModel.DataAnnotations;

namespace MEMS.Shared.Contracts;

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
    public string AccessToken { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;

    public string? TenantSlug { get; set; }
}

public class DashboardDto
{
    public int MunicipalityCount { get; set; }
    public int PositionCount { get; set; }
    public int CandidateCount { get; set; }
}
