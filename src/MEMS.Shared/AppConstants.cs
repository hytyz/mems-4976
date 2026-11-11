using System.Text.RegularExpressions;

namespace MEMS.Shared;

/// <summary>Identity role names. There are exactly two roles in the system.</summary>
public static class AppRoles
{
    /// <summary>Full system access across every tenant.</summary>
    public const string SuperAdmin = "SuperAdmin";

    /// <summary>Manages exactly one tenant (the municipality stored on the user account).</summary>
    public const string MunicipalityAdmin = "MunicipalityAdmin";

    public static string DisplayName(string role) => role switch
    {
        SuperAdmin => "Super Admin",
        MunicipalityAdmin => "Municipality Administrator",
        _ => role
    };
}

public static class AppClaims
{
    /// <summary>Id of the municipality (tenant) a Municipality Administrator is assigned to.</summary>
    public const string TenantId = "tenant_id";

    /// <summary>URL slug of that municipality.</summary>
    public const string TenantSlug = "tenant_slug";
}

/// <summary>Rules for the URL segment that identifies a tenant, e.g. <c>/burnaby</c>.</summary>
public static partial class SlugRules
{
    public const int MinLength = 2;
    public const int MaxLength = 50;
    public const string Pattern = "^[a-z0-9]+(?:-[a-z0-9]+)*$";

    /// <summary>Segments that already mean something in the public site, admin site or API.</summary>
    public static readonly IReadOnlySet<string> Reserved = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "admin", "api", "auth", "municipalities", "health", "alive", "account", "error", "not-found",
        "openapi", "swagger", "css", "js", "lib", "_framework", "_content", "images", "index"
    };

    [GeneratedRegex(Pattern)]
    private static partial Regex SlugRegex();

    public static bool IsWellFormed(string? slug) =>
        slug is { Length: >= MinLength and <= MaxLength } && SlugRegex().IsMatch(slug);

    public static bool IsValid(string? slug) => IsWellFormed(slug) && !Reserved.Contains(slug!);

    /// <summary>Suggests a slug for a municipality name ("Pitt Meadows" → "pitt-meadows").</summary>
    public static string Suggest(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return string.Empty;
        }

        var chars = name.Trim().ToLowerInvariant()
            .Select(ch => char.IsAsciiLetterOrDigit(ch) ? ch : '-')
            .ToArray();
        string slug = string.Join('-', new string(chars).Split('-', StringSplitOptions.RemoveEmptyEntries));
        return slug.Length > MaxLength ? slug[..MaxLength].Trim('-') : slug;
    }
}

/// <summary>Limits applied to uploaded logos and candidate photographs at every layer.</summary>
public static class ImageRules
{
    public const long MaxBytes = 5 * 1024 * 1024;
    public const string MaxSizeText = "5 MB";
    public const string Accept = ".jpg,.jpeg,.png,.gif,.webp";
    public static readonly string[] AllowedContentTypes = ["image/jpeg", "image/png", "image/gif", "image/webp"];
}
