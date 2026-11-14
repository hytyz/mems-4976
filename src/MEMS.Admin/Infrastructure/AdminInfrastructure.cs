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
    public const string AnyAdmin = "AnyAdmin";

    public const string SuperAdmin = "SuperAdmin";
}

public sealed class CircuitTenantInitializer(AuthenticationStateProvider authenticationState, TenantContext tenant)
    : ITenantContextInitializer
{
    public async ValueTask EnsureInitializedAsync(CancellationToken cancellationToken = default)
    {
        var state = await authenticationState.GetAuthenticationStateAsync();
        tenant.SetFromUser(state.User);
    }
}

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

    public static string? ImageUrl(this Guid? imageId) => imageId is null ? null : $"images/{imageId}";
}

public static class NavigationExtensions
{
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
