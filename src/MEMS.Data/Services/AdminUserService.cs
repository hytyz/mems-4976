using MEMS.Data.Entities;
using MEMS.Data.Tenancy;
using MEMS.Shared;
using MEMS.Shared.Contracts;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MEMS.Data.Services;

/// <summary>
/// Administrator accounts. There is no self-registration: only a Super Admin can create an
/// account, reset its password or delete it.
/// </summary>
public interface IAdminUserService
{
    Task<Result<List<AdminUserDto>>> ListAsync(CancellationToken cancellationToken = default);
    Task<Result<AdminUserDto>> GetAsync(string id, CancellationToken cancellationToken = default);
    Task<Result<AdminUserDto>> CreateAsync(CreateAdminUserInput input, CancellationToken cancellationToken = default);
    Task<Result<bool>> ResetPasswordAsync(string id, ResetPasswordInput input, CancellationToken cancellationToken = default);
    Task<Result<bool>> DeleteAsync(string id, string? actingUserId, CancellationToken cancellationToken = default);
}

public sealed class AdminUserService(
    IServiceScopeFactory scopeFactory,
    IDbContextFactory<ApplicationDbContext> dbFactory,
    ITenantContext tenant,
    ITenantContextInitializer tenantInitializer,
    ILogger<AdminUserService> logger) : IAdminUserService
{
    private const string SuperAdminOnly = "Only a Super Admin can manage administrator accounts.";
    private const string NotFoundMessage = "The administrator account was not found.";

    public async Task<Result<List<AdminUserDto>>> ListAsync(CancellationToken cancellationToken = default)
    {
        if (!await IsSuperAdminAsync(cancellationToken))
        {
            return Result<List<AdminUserDto>>.Forbidden(SuperAdminOnly);
        }

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        return Result<List<AdminUserDto>>.Ok(await QueryUsers(db).OrderBy(u => u.UserName).ToListAsync(cancellationToken));
    }

    public async Task<Result<AdminUserDto>> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        if (!await IsSuperAdminAsync(cancellationToken))
        {
            return Result<AdminUserDto>.Forbidden(SuperAdminOnly);
        }

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var user = await QueryUsers(db).FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        return user is null ? Result<AdminUserDto>.NotFound(NotFoundMessage) : Result<AdminUserDto>.Ok(user);
    }

    public async Task<Result<AdminUserDto>> CreateAsync(CreateAdminUserInput input, CancellationToken cancellationToken = default)
    {
        if (!await IsSuperAdminAsync(cancellationToken))
        {
            return Result<AdminUserDto>.Forbidden(SuperAdminOnly);
        }

        if (InputValidator.Validate(input) is { } errors)
        {
            return Result<AdminUserDto>.Invalid(errors);
        }

        // A Super Admin never has a tenant; a Municipality Administrator has exactly one.
        int? municipalityId = input.Role == AppRoles.MunicipalityAdmin ? input.MunicipalityId : null;

        await using var scope = scopeFactory.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        if (municipalityId is int tenantId)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
            if (!await db.Municipalities.AnyAsync(m => m.MunicipalityId == tenantId, cancellationToken))
            {
                return Result<AdminUserDto>.Invalid(nameof(input.MunicipalityId), "Choose an existing municipality.");
            }
        }

        if (await userManager.FindByNameAsync(input.UserName) is not null)
        {
            return Result<AdminUserDto>.Conflict(nameof(input.UserName), $"Username '{input.UserName}' is already taken.");
        }

        var user = new ApplicationUser { UserName = input.UserName, MunicipalityId = municipalityId };
        var created = await userManager.CreateAsync(user, input.Password);
        if (!created.Succeeded)
        {
            return Result<AdminUserDto>.Invalid(ToErrors(created, nameof(input.Password)));
        }

        var addedToRole = await userManager.AddToRoleAsync(user, input.Role);
        if (!addedToRole.Succeeded)
        {
            await userManager.DeleteAsync(user);
            return Result<AdminUserDto>.Invalid(ToErrors(addedToRole, nameof(input.Role)));
        }

        logger.LogInformation("Administrator {UserName} created with role {Role} for municipality {MunicipalityId}",
            user.UserName, input.Role, municipalityId);
        return await GetAsync(user.Id, cancellationToken);
    }

    public async Task<Result<bool>> ResetPasswordAsync(string id, ResetPasswordInput input, CancellationToken cancellationToken = default)
    {
        if (!await IsSuperAdminAsync(cancellationToken))
        {
            return Result<bool>.Forbidden(SuperAdminOnly);
        }

        if (InputValidator.Validate(input) is { } errors)
        {
            return Result<bool>.Invalid(errors);
        }

        await using var scope = scopeFactory.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByIdAsync(id);
        if (user is null)
        {
            return Result<bool>.NotFound(NotFoundMessage);
        }

        // Validate first so a rejected password does not leave the account without one.
        foreach (var validator in userManager.PasswordValidators)
        {
            var validation = await validator.ValidateAsync(userManager, user, input.Password);
            if (!validation.Succeeded)
            {
                return Result<bool>.Invalid(ToErrors(validation, nameof(input.Password)));
            }
        }

        string token = await userManager.GeneratePasswordResetTokenAsync(user);
        var reset = await userManager.ResetPasswordAsync(user, token, input.Password);
        if (!reset.Succeeded)
        {
            return Result<bool>.Invalid(ToErrors(reset, nameof(input.Password)));
        }

        // Clears any lockout and (via the new security stamp) signs the account out everywhere.
        await userManager.SetLockoutEndDateAsync(user, null);
        logger.LogInformation("Password reset for administrator {UserName}", user.UserName);
        return Result<bool>.Ok(true);
    }

    public async Task<Result<bool>> DeleteAsync(string id, string? actingUserId, CancellationToken cancellationToken = default)
    {
        if (!await IsSuperAdminAsync(cancellationToken))
        {
            return Result<bool>.Forbidden(SuperAdminOnly);
        }

        if (id == actingUserId)
        {
            return Result<bool>.Forbidden("You cannot delete the account you are signed in with.");
        }

        await using var scope = scopeFactory.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByIdAsync(id);
        if (user is null)
        {
            return Result<bool>.NotFound(NotFoundMessage);
        }

        await userManager.DeleteAsync(user);
        logger.LogInformation("Administrator {UserName} deleted", user.UserName);
        return Result<bool>.Ok(true);
    }

    private async Task<bool> IsSuperAdminAsync(CancellationToken cancellationToken)
    {
        await tenantInitializer.EnsureInitializedAsync(cancellationToken);
        return tenant.IsUnrestricted;
    }

    private static IQueryable<AdminUserDto> QueryUsers(ApplicationDbContext db) =>
        from user in db.Users.AsNoTracking()
        join userRole in db.UserRoles on user.Id equals userRole.UserId
        join role in db.Roles on userRole.RoleId equals role.Id
        select new AdminUserDto
        {
            Id = user.Id,
            UserName = user.UserName ?? string.Empty,
            Role = role.Name ?? string.Empty,
            MunicipalityId = user.MunicipalityId,
            MunicipalityName = user.Municipality!.Name
        };

    private static Dictionary<string, string[]> ToErrors(IdentityResult result, string field) =>
        new() { [field] = result.Errors.Select(e => e.Description).ToArray() };
}
