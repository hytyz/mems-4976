using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MunicipalElections.Data;
using MunicipalElections.Models;

namespace MunicipalElections.Services;

public static class Roles
{
    /// <summary>System-wide administrators.</summary>
    public const string SuperAdmin = "SuperAdmin";

    /// <summary>
    /// Name of the distinct admin group (Identity role) for one municipality,
    /// e.g. "Pitt Meadows Admin". Each municipality gets its own role.
    /// </summary>
    public static string ForMunicipality(string municipalityName) => $"{municipalityName.Trim()} Admin";
}

public static class Policies
{
    /// <summary>SuperAdmin, or a member of at least one municipality admin group.</summary>
    public const string AdminArea = "AdminArea";
}

public class AdminAreaRequirement : IAuthorizationRequirement
{
}

public class AdminAreaHandler : AuthorizationHandler<AdminAreaRequirement>
{
    private readonly MunicipalityAccessService _access;

    public AdminAreaHandler(MunicipalityAccessService access)
    {
        _access = access;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, AdminAreaRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            return;
        }

        if (_access.IsSuperAdmin(context.User) || (await _access.GetMunicipalityIdsAsync(context.User)).Count > 0)
        {
            context.Succeed(requirement);
        }
    }
}

public class MunicipalityAccessService
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    // Per-request cache (the service is scoped) so repeated checks cost one query.
    private string? _cachedUserId;
    private List<int>? _cachedIds;

    public MunicipalityAccessService(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public bool IsSuperAdmin(ClaimsPrincipal user)
    {
        return user.Identity?.IsAuthenticated == true && user.IsInRole(Roles.SuperAdmin);
    }

    /// <summary>
    /// Municipalities the user may administer: all of them for a SuperAdmin, otherwise
    /// the municipalities whose admin group (role) the user belongs to.
    /// </summary>
    public async Task<List<int>> GetMunicipalityIdsAsync(ClaimsPrincipal user)
    {
        if (user.Identity?.IsAuthenticated != true)
        {
            return new List<int>();
        }

        if (user.IsInRole(Roles.SuperAdmin))
        {
            return await _db.Municipalities.Select(m => m.MunicipalityId).ToListAsync();
        }

        string? userId = _userManager.GetUserId(user);
        if (userId is null)
        {
            return new List<int>();
        }

        if (_cachedIds is null || _cachedUserId != userId)
        {
            // Role membership is read from the database (not the cookie) so that
            // changes made by a SuperAdmin take effect immediately.
            _cachedIds = await (
                from userRole in _db.UserRoles
                join role in _db.Roles on userRole.RoleId equals role.Id
                join municipality in _db.Municipalities on role.Name equals municipality.AdminRoleName
                where userRole.UserId == userId
                select municipality.MunicipalityId).ToListAsync();
            _cachedUserId = userId;
        }

        return _cachedIds;
    }

    /// <summary>
    /// For a signed-in municipality admin, the municipality ids they are restricted to
    /// everywhere on the site. Null means "no restriction" (visitors and SuperAdmins).
    /// </summary>
    public async Task<List<int>?> GetRestrictedScopeAsync(ClaimsPrincipal user)
    {
        if (user.Identity?.IsAuthenticated != true || user.IsInRole(Roles.SuperAdmin))
        {
            return null;
        }

        return await GetMunicipalityIdsAsync(user);
    }

    public async Task<bool> CanManageMunicipalityAsync(ClaimsPrincipal user, int municipalityId)
    {
        if (IsSuperAdmin(user))
        {
            return true;
        }

        return (await GetMunicipalityIdsAsync(user)).Contains(municipalityId);
    }

    public async Task<bool> CanManagePositionAsync(ClaimsPrincipal user, int positionId)
    {
        if (IsSuperAdmin(user))
        {
            return true;
        }

        var municipalityId = await _db.Positions
            .Where(p => p.PositionId == positionId)
            .Select(p => p.MunicipalityId)
            .FirstOrDefaultAsync();

        return municipalityId != 0 && await CanManageMunicipalityAsync(user, municipalityId);
    }

    public async Task<bool> CanManageCandidateAsync(ClaimsPrincipal user, int candidateId)
    {
        if (IsSuperAdmin(user))
        {
            return true;
        }

        var municipalityId = await _db.Candidates
            .Where(c => c.CandidateId == candidateId)
            .Select(c => c.Position!.MunicipalityId)
            .FirstOrDefaultAsync();

        return municipalityId != 0 && await CanManageMunicipalityAsync(user, municipalityId);
    }
}
