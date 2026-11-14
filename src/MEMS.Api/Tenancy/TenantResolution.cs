using MEMS.Data.Services;
using MEMS.Data.Tenancy;
using MEMS.Shared;
using MEMS.Shared.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MEMS.Api.Tenancy;

public interface ICurrentTenant
{
    MunicipalityDto? Municipality { get; }

    MunicipalityDto Required { get; }
}

public sealed class CurrentTenant : ICurrentTenant
{
    public MunicipalityDto? Municipality { get; set; }

    public MunicipalityDto Required =>
        Municipality ?? throw new InvalidOperationException("This endpoint is not tenant-routed.");
}

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
    public const string TenantAdmin = "TenantAdmin";

    public const string AnyAdmin = "AnyAdmin";

    public const string SuperAdmin = "SuperAdmin";
}

public sealed class TenantAdminRequirement : IAuthorizationRequirement;

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
