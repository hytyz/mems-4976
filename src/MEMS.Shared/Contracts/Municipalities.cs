using System.ComponentModel.DataAnnotations;
using MEMS.Shared.Validation;

namespace MEMS.Shared.Contracts;

public class MunicipalityDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public string Province { get; set; } = string.Empty;
    public DateOnly ElectionDate { get; set; }
    public string? Description { get; set; }

    public string? Tagline { get; set; }

    public string ThemeColor { get; set; } = MunicipalityInput.DefaultThemeColor;

    public Guid? LogoImageId { get; set; }

    public int PositionCount { get; set; }
    public int CandidateCount { get; set; }
}

public class MunicipalityInput
{
    public const string DefaultThemeColor = "#1d4e89";

    [Required(ErrorMessage = "Municipality name is required.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Municipality name must be between 2 and 100 characters.")]
    public string Name { get; set => field = value?.Trim() ?? string.Empty; } = string.Empty;

    [Required(ErrorMessage = "The URL slug is required.")]
    [TenantSlug]
    [Display(Name = "URL slug")]
    public string Slug { get; set => field = value?.Trim().ToLowerInvariant() ?? string.Empty; } = string.Empty;

    [Required(ErrorMessage = "Province is required.")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Province must be between 2 and 50 characters.")]
    public string Province { get; set => field = value?.Trim() ?? string.Empty; } = "British Columbia";

    [Required(ErrorMessage = "Election date is required.")]
    [ElectionDate]
    [Display(Name = "Election date")]
    public DateOnly? ElectionDate { get; set; }

    [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters.")]
    public string? Description { get; set => field = Text.NullIfBlank(value); }

    [StringLength(150, ErrorMessage = "Tagline cannot exceed 150 characters.")]
    public string? Tagline { get; set => field = Text.NullIfBlank(value); }

    [Required(ErrorMessage = "Theme colour is required.")]
    [RegularExpression("^#[0-9a-fA-F]{6}$", ErrorMessage = "Theme colour must be a hex colour such as #1d4e89.")]
    [Display(Name = "Theme colour")]
    public string ThemeColor { get; set => field = value?.Trim().ToLowerInvariant() ?? string.Empty; } = DefaultThemeColor;
}

internal static class Text
{
    public static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
