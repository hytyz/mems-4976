using System.ComponentModel.DataAnnotations;

namespace MEMS.Shared.Contracts;

public class AdminUserDto
{
    public string Id { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public int? MunicipalityId { get; set; }
    public string? MunicipalityName { get; set; }
}

public class CreateAdminUserInput : IValidatableObject
{
    [Required(ErrorMessage = "Username is required.")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Username must be between 2 and 50 characters.")]
    [RegularExpression("^[A-Za-z0-9._-]+$", ErrorMessage = "Username may only contain letters, digits and . _ -")]
    [Display(Name = "Username")]
    public string UserName { get; set => field = value?.Trim() ?? string.Empty; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [StringLength(100, MinimumLength = PasswordRules.MinLength, ErrorMessage = PasswordRules.LengthMessage)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please confirm the password.")]
    [Compare(nameof(Password), ErrorMessage = "The passwords do not match.")]
    [Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Choose a role.")]
    public string Role { get; set; } = AppRoles.MunicipalityAdmin;

    /// <summary>Required for a Municipality Administrator; must be empty for a Super Admin.</summary>
    [Display(Name = "Municipality")]
    public int? MunicipalityId { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Role is not (AppRoles.SuperAdmin or AppRoles.MunicipalityAdmin))
        {
            yield return new ValidationResult("Choose a role.", [nameof(Role)]);
        }
        else if (Role == AppRoles.MunicipalityAdmin && MunicipalityId is null or <= 0)
        {
            yield return new ValidationResult(
                "A Municipality Administrator must be assigned to exactly one municipality.", [nameof(MunicipalityId)]);
        }
    }
}

public class ResetPasswordInput
{
    [Required(ErrorMessage = "Password is required.")]
    [StringLength(100, MinimumLength = PasswordRules.MinLength, ErrorMessage = PasswordRules.LengthMessage)]
    [Display(Name = "New password")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please confirm the password.")]
    [Compare(nameof(Password), ErrorMessage = "The passwords do not match.")]
    [Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public static class PasswordRules
{
    public const int MinLength = 8;
    public const string LengthMessage = "Password must be at least 8 characters.";
    public const string Summary = "At least 8 characters with an uppercase letter, a lowercase letter, a digit and a symbol.";
}
