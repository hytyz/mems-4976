using System.Text.RegularExpressions;

namespace MEMS.Shared;

public static class AppRoles
{
    public const string SuperAdmin = "SuperAdmin";

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
    public const string TenantId = "tenant_id";

    public const string TenantSlug = "tenant_slug";
}

public static partial class SlugRules
{
    public const int MinLength = 2;
    public const int MaxLength = 50;
    public const string Pattern = "^[a-z0-9]+(?:-[a-z0-9]+)*$";

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

public static class ImageRules
{
    public const long MaxBytes = 5 * 1024 * 1024;
    public const string MaxSizeText = "5 MB";
    public const string Accept = ".jpg,.jpeg,.png,.gif,.webp";
    public static readonly string[] AllowedContentTypes = ["image/jpeg", "image/png", "image/gif", "image/webp"];
}
