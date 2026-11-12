using System.Security.Claims;
using MEMS.Data.Entities;
using MEMS.Data.Tenancy;
using MEMS.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace MEMS.Admin.Infrastructure;

public static class AdminPolicies
{
    /// <summary>A Super Admin or a Municipality Administrator.</summary>
    public const string AnyAdmin = "AnyAdmin";

    public const string SuperAdmin = "SuperAdmin";
}

/// <summary>
/// Sets the tenant boundary for the Blazor circuit from the signed-in administrator, every time
/// a service is about to touch the database. The EF Core query filters then keep a Municipality
/// Administrator inside their own municipality on every page, whatever ids appear in the URL.
/// </summary>
public sealed class CircuitTenantInitializer(AuthenticationStateProvider authenticationState, TenantContext tenant)
    : ITenantContextInitializer
{
    public async ValueTask EnsureInitializedAsync(CancellationToken cancellationToken = default)
    {
        var state = await authenticationState.GetAuthenticationStateAsync();
        tenant.SetFromUser(state.User);
    }
}

/// <summary>
/// Re-checks the signed-in account every few minutes while a circuit is open. A deleted account,
/// a reset password or a deleted municipality ends the session instead of leaving it usable.
/// </summary>
internal sealed class IdentityRevalidatingAuthenticationStateProvider(
    ILoggerFactory loggerFactory,
    IServiceScopeFactory scopeFactory,
    IOptions<IdentityOptions> options)
    : RevalidatingServerAuthenticationStateProvider(loggerFactory)
{
    protected override TimeSpan RevalidationInterval => TimeSpan.FromMinutes(1);

    protected override async Task<bool> ValidateAuthenticationStateAsync(
        AuthenticationState authenticationState, CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var user = await userManager.GetUserAsync(authenticationState.User);
        if (user is null)
        {
            return false;
        }

        string? principalStamp = authenticationState.User.FindFirstValue(options.Value.ClaimsIdentity.SecurityStampClaimType);
        return principalStamp == await userManager.GetSecurityStampAsync(user);
    }
}

/// <summary>A one-off success or error banner that survives the navigation that follows a save.</summary>
public sealed class FlashService
{
    private int _navigationsToSurvive;

    public string? Message { get; private set; }
    public bool IsError { get; private set; }

    public event Action? Changed;

    public void Success(string message) => Set(message, isError: false);

    public void Error(string message) => Set(message, isError: true);

    public void Clear()
    {
        Message = null;
        Changed?.Invoke();
    }

    /// <summary>Called by the layout on every navigation.</summary>
    public void OnNavigated()
    {
        if (_navigationsToSurvive > 0)
        {
            _navigationsToSurvive--;
        }
        else if (Message is not null)
        {
            Clear();
        }
    }

    private void Set(string message, bool isError)
    {
        Message = message;
        IsError = isError;
        _navigationsToSurvive = 1;
        Changed?.Invoke();
    }
}

/// <summary>Links to the other Aspire resources, resolved from service-discovery configuration.</summary>
public sealed class ServiceLinks(IConfiguration configuration)
{
    public string? PublicSite => Resolve("public");

    public string? Api => Resolve("api");

    public string? PublicTenantUrl(string slug) => PublicSite is { } site ? $"{site}/{slug}" : null;

    private string? Resolve(string service) =>
        (configuration[$"services:{service}:https:0"] ?? configuration[$"services:{service}:http:0"])?.TrimEnd('/');
}

public static class AdminUserExtensions
{
    public static bool IsSuperAdmin(this ClaimsPrincipal user) => user.IsInRole(AppRoles.SuperAdmin);

    public static string? UserId(this ClaimsPrincipal user) => user.FindFirstValue(ClaimTypes.NameIdentifier);

    public static string? TenantSlug(this ClaimsPrincipal user) => user.FindFirstValue(AppClaims.TenantSlug);

    public static string RoleDisplay(this ClaimsPrincipal user) =>
        user.IsSuperAdmin() ? AppRoles.DisplayName(AppRoles.SuperAdmin) : AppRoles.DisplayName(AppRoles.MunicipalityAdmin);

    /// <summary>Relative URL of an image served by this app (see the /images endpoint in Program.cs).</summary>
    public static string? ImageUrl(this Guid? imageId) => imageId is null ? null : $"images/{imageId}";
}

public static class NavigationExtensions
{
    /// <summary>Accepts only a base-relative return path, so a crafted link cannot redirect off-site.</summary>
    public static string SafeReturnPath(string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl)
            || returnUrl.StartsWith('/') || returnUrl.StartsWith('\\')
            || returnUrl.Contains("//") || returnUrl.Contains(':'))
        {
            return string.Empty;
        }

        return returnUrl;
    }

    public static void GoTo(this NavigationManager navigation, string path) => navigation.NavigateTo(path);
}
