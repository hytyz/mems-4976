using System.Security.Claims;
using MEMS.Shared;

namespace MEMS.Data.Tenancy;

/// <summary>
/// The tenant boundary for the current unit of work (an API request or a Blazor circuit).
/// <see cref="ApplicationDbContext"/> reads it in its global query filters, so every EF Core
/// query is limited to the tenant automatically.
/// </summary>
public interface ITenantContext
{
    /// <summary>True only for a Super Admin working outside a tenant URL: all tenants are visible.</summary>
    bool IsUnrestricted { get; }

    /// <summary>The one municipality that is visible, when restricted.</summary>
    int? TenantId { get; }

    /// <summary>Whether rows of the given municipality may be read or written.</summary>
    bool CanAccess(int municipalityId);
}

/// <summary>
/// Fails closed: until <see cref="RestrictTo"/> or <see cref="Unrestrict"/> is called,
/// no tenant-owned row is visible at all.
/// </summary>
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

    /// <summary>
    /// Sets the boundary from a signed-in administrator: everything for a Super Admin,
    /// the assigned municipality for a Municipality Administrator, nothing for anyone else.
    /// </summary>
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

/// <summary>
/// Makes sure the <see cref="ITenantContext"/> has been populated before a service touches the
/// database. The API sets it in a request filter (so this is a no-op there); the Blazor Server
/// app sets it from the circuit's authentication state.
/// </summary>
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
