using System.Security.Claims;
using MEMS.Shared;

namespace MEMS.Data.Tenancy;

public interface ITenantContext
{
    bool IsUnrestricted { get; }

    int? TenantId { get; }

    bool CanAccess(int municipalityId);
}

public sealed class TenantContext : ITenantContext
{
    public bool IsUnrestricted { get; private set; }

    public int? TenantId { get; private set; }

    public bool CanAccess(int municipalityId) => IsUnrestricted || TenantId == municipalityId;

    public void RestrictTo(int municipalityId)
    {
        IsUnrestricted = false;
        TenantId = municipalityId;
    }

    public void Unrestrict()
    {
        IsUnrestricted = true;
        TenantId = null;
    }

    public void Clear()
    {
        IsUnrestricted = false;
        TenantId = null;
    }

    public void SetFromUser(ClaimsPrincipal? user)
    {
        Clear();

        if (user?.Identity?.IsAuthenticated != true)
        {
            return;
        }

        if (user.IsInRole(AppRoles.SuperAdmin))
        {
            Unrestrict();
        }
        else if (user.IsInRole(AppRoles.MunicipalityAdmin) && user.GetTenantId() is int tenantId)
        {
            RestrictTo(tenantId);
        }
    }
}

public interface ITenantContextInitializer
{
    ValueTask EnsureInitializedAsync(CancellationToken cancellationToken = default);
}

public sealed class NoOpTenantContextInitializer : ITenantContextInitializer
{
    public ValueTask EnsureInitializedAsync(CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
}

public static class TenantClaimsExtensions
{
    public static int? GetTenantId(this ClaimsPrincipal user) =>
        int.TryParse(user.FindFirstValue(AppClaims.TenantId), out int id) ? id : null;
}
