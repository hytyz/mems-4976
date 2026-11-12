using System.Security.Claims;
using MEMS.Data.Entities;
using MEMS.Data.Services;
using MEMS.Shared;
using MEMS.Shared.Contracts;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace MEMS.Data.Identity;

public static class IdentityDefaults
{
    /// <summary>One password and lockout policy for the Admin app, the API and the seeder.</summary>
    public static void Configure(IdentityOptions options)
    {
        options.Password.RequiredLength = PasswordRules.MinLength;
        options.Password.RequireDigit = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireNonAlphanumeric = true;

        options.Lockout.AllowedForNewUsers = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);

        options.User.RequireUniqueEmail = false;
        options.SignIn.RequireConfirmedAccount = false;
    }
}

/// <summary>
/// Adds the administrator's tenant to their identity, so the cookie (Admin) and the
/// JWT (API) both carry the tenant boundary alongside the role.
/// </summary>
public sealed class TenantClaimsPrincipalFactory(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager,
    IOptions<IdentityOptions> options,
    ITenantDirectory tenants)
    : UserClaimsPrincipalFactory<ApplicationUser, IdentityRole>(userManager, roleManager, options)
{
    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);

        // Only a Municipality Administrator is given a tenant, and only one that still exists.
        if (user.MunicipalityId is int municipalityId
            && identity.HasClaim(identity.RoleClaimType, AppRoles.MunicipalityAdmin)
            && await tenants.FindByIdAsync(municipalityId) is { } municipality)
        {
            identity.AddClaim(new Claim(AppClaims.TenantId, municipality.Id.ToString()));
            identity.AddClaim(new Claim(AppClaims.TenantSlug, municipality.Slug));
        }

        return identity;
    }
}
