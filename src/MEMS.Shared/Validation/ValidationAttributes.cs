using System.ComponentModel.DataAnnotations;

namespace MEMS.Shared.Validation;

/// <summary>
/// An absolute http(s) URL. Stricter than <see cref="UrlAttribute"/>, which also accepts ftp://,
/// and safe to render as a link (no javascript: or data: schemes can pass).
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class HttpUrlAttribute : ValidationAttribute
{
    public HttpUrlAttribute() : base("{0} must be a valid http:// or https:// address.")
    {
    }

    public override bool IsValid(object? value)
    {
        if (value is null || (value is string text && text.Length == 0))
        {
            return true;
        }

        return value is string url
            && Uri.TryCreate(url, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
            && !string.IsNullOrEmpty(uri.Host);
    }
}

/// <summary>A tenant URL segment: lowercase letters, digits and single hyphens, and not a reserved word.</summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class TenantSlugAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext context)
    {
        if (value is not string slug || slug.Length == 0)
        {
            return ValidationResult.Success; // [Required] reports missing values.
        }

        string[] members = context.MemberName is null ? [] : [context.MemberName];

        if (!SlugRules.IsWellFormed(slug))
        {
            return new ValidationResult(
                $"The URL slug must be {SlugRules.MinLength}-{SlugRules.MaxLength} characters: lowercase letters, digits and single hyphens (for example \"pitt-meadows\").",
                members);
        }

        return SlugRules.Reserved.Contains(slug)
            ? new ValidationResult($"\"{slug}\" is reserved by the system. Choose a different URL slug.", members)
            : ValidationResult.Success;
    }
}

/// <summary>Rejects dates outside a sane window, so a typo cannot store year 0202 or 20260.</summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class ElectionDateAttribute : ValidationAttribute
{
    public const int MinYear = 2000;
    public const int MaxYear = 2100;

    public ElectionDateAttribute() : base("{0} must be between the years 2000 and 2100.")
    {
    }

    public override bool IsValid(object? value) => value switch
    {
        null => true,
        DateOnly date => date.Year is >= MinYear and <= MaxYear,
        _ => false
    };
}
