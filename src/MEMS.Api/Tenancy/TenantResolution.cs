using MEMS.Data.Services;
using MEMS.Data.Tenancy;
using MEMS.Shared;
using MEMS.Shared.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MEMS.Api.Tenancy;

/// <summary>The tenant named in the URL of the current request (<c>/api/{tenant}/...</c>).</summary>
public interface ICurrentTenant
{
    MunicipalityDto? Municipality { get; }

    /// <summary>The resolved tenant. Only call from tenant-routed endpoints.</summary>
    MunicipalityDto Required { get; }
}

public sealed class CurrentTenant : ICurrentTenant
{
    public MunicipalityDto? Municipality { get; set; }

    public MunicipalityDto Required =>
        Municipality ?? throw new InvalidOperationException("This endpoint is not tenant-routed.");
}

/// <summary>
/// Establishes the tenant boundary for every request, before authorization and before any
/// controller runs:
/// <list type="bullet">
/// <item>/api/{tenant}/... – the slug is resolved to a municipality (404 when unknown) and all
/// data access is restricted to it, whoever is calling.</item>
/// <item>any other route – the boundary comes from the signed-in administrator
/// (everything for a Super Admin, their one municipality for a Municipality Administrator,
/// nothing for anonymous callers).</item>
/// </list>
/// </summary>
public sealed class TenantResolutionMiddleware(RequestDelegate next, ILogger<TenantResolutionMiddleware> logger)
{
    public const string RouteKey = "tenant";

    public async Task InvokeAsync(
        HttpContext context,
        TenantContext tenantContext,
        CurrentTenant currentTenant,
        ITenantDirectory directory,
        IProblemDetailsService problemDetails)
    {
        if (context.GetRouteValue(RouteKey) is string slug)
        {
            var municipality = SlugRules.IsWellFormed(slug.ToLowerInvariant())
                ? await directory.FindBySlugAsync(slug, context.RequestAborted)
                : null;

            if (municipality is null)
            {
                logger.LogInformation("Request for unknown tenant {TenantSlug}", slug);
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                await problemDetails.WriteAsync(new ProblemDetailsContext
                {
                    HttpContext = context,
                    ProblemDetails = new ProblemDetails
                    {
                        Status = StatusCodes.Status404NotFound,
                        Title = "Municipality not found",
                        Detail = $"There is no municipality at '{slug}'."
                    }
                });
                return;
            }

            currentTenant.Municipality = municipality;
            tenantContext.RestrictTo(municipality.Id);
        }
        else
        {
            tenantContext.SetFromUser(context.User);
        }

        // Every log line written while handling the request carries the tenant.
        using (logger.BeginScope(new Dictionary<string, object?>
        {
            ["TenantId"] = tenantContext.TenantId,
            ["TenantSlug"] = currentTenant.Municipality?.Slug
        }))
        {
            await next(context);
        }
    }
}

public static class ApiPolicies
{
    /// <summary>May change data of the tenant in the URL: a Super Admin, or that tenant's own administrator.</summary>
    public const string TenantAdmin = "TenantAdmin";

    /// <summary>Any signed-in administrator; the service layer applies the tenant boundary.</summary>
    public const string AnyAdmin = "AnyAdmin";

    public const string SuperAdmin = "SuperAdmin";
}

public sealed class TenantAdminRequirement : IAuthorizationRequirement;

/// <summary>
/// Tenant-aware authorization, enforced in the API itself: a Burnaby administrator calling
/// /api/surrey/... is refused with 403 no matter what any user interface allows.
/// </summary>
public sealed class TenantAdminHandler(ICurrentTenant currentTenant, ILogger<TenantAdminHandler> logger)
    : AuthorizationHandler<TenantAdminRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, TenantAdminRequirement requirement)
    {
        var user = context.User;
        if (user.Identity?.IsAuthenticated != true || currentTenant.Municipality is not { } tenant)
        {
            return Task.CompletedTask;
        }

        if (user.IsInRole(AppRoles.SuperAdmin)
            || (user.IsInRole(AppRoles.MunicipalityAdmin) && user.GetTenantId() == tenant.Id))
        {
            context.Succeed(requirement);
        }
        else
        {
            logger.LogWarning("Cross-tenant access denied: user {UserName} (tenant {UserTenantId}) attempted to modify tenant {TenantSlug}",
                user.Identity.Name, user.GetTenantId(), tenant.Slug);
        }

        return Task.CompletedTask;
    }
}
