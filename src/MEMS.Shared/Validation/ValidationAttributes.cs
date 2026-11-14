using System.ComponentModel.DataAnnotations;

namespace MEMS.Shared.Validation;

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

[AttributeUsage(AttributeTargets.Property)]
public sealed class TenantSlugAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext context)
    {
        if (value is not string slug || slug.Length == 0)
        {
            return ValidationResult.Success;
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
