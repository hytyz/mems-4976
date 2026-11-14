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
