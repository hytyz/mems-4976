using MEMS.Data.Entities;
using MEMS.Data.Tenancy;
using MEMS.Shared.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MEMS.Data.Services;

public interface ITenantDirectory
{
    Task<List<MunicipalityDto>> ListAsync(CancellationToken cancellationToken = default);
    Task<MunicipalityDto?> FindBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<MunicipalityDto?> FindByIdAsync(int id, CancellationToken cancellationToken = default);
}

public sealed class TenantDirectory(IDbContextFactory<ApplicationDbContext> dbFactory) : ITenantDirectory
{
    public async Task<List<MunicipalityDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.Municipalities.IgnoreQueryFilters().AsNoTracking()
            .OrderBy(m => m.Name)
            .Select(Mapping.ToMunicipalityDto)
            .ToListAsync(cancellationToken);
    }

    public async Task<MunicipalityDto?> FindBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        string normalized = slug.Trim().ToLowerInvariant();
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.Municipalities.IgnoreQueryFilters().AsNoTracking()
            .Where(m => m.Slug == normalized)
            .Select(Mapping.ToMunicipalityDto)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<MunicipalityDto?> FindByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.Municipalities.IgnoreQueryFilters().AsNoTracking()
            .Where(m => m.MunicipalityId == id)
            .Select(Mapping.ToMunicipalityDto)
            .FirstOrDefaultAsync(cancellationToken);
    }
}

public interface IMunicipalityService
{
    Task<List<MunicipalityDto>> ListAsync(CancellationToken cancellationToken = default);
    Task<MunicipalityDto?> GetAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<MunicipalityDto>> CreateAsync(MunicipalityInput input, CancellationToken cancellationToken = default);
    Task<Result<MunicipalityDto>> UpdateAsync(int id, MunicipalityInput input, CancellationToken cancellationToken = default);
    Task<Result<bool>> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<Guid>> SetLogoAsync(int id, byte[] content, CancellationToken cancellationToken = default);
    Task<Result<bool>> RemoveLogoAsync(int id, CancellationToken cancellationToken = default);
    Task<DashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default);
}

public sealed class MunicipalityService(
    IDbContextFactory<ApplicationDbContext> dbFactory,
    ITenantContext tenant,
    ITenantContextInitializer tenantInitializer,
    ILogger<MunicipalityService> logger) : IMunicipalityService
{
    private const string NotFoundMessage = "The municipality was not found.";

    public async Task<List<MunicipalityDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        await tenantInitializer.EnsureInitializedAsync(cancellationToken);
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.Municipalities.AsNoTracking()
            .OrderBy(m => m.Name)
            .Select(Mapping.ToMunicipalityDto)
            .ToListAsync(cancellationToken);
    }

    public async Task<MunicipalityDto?> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        await tenantInitializer.EnsureInitializedAsync(cancellationToken);
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.Municipalities.AsNoTracking()
            .Where(m => m.MunicipalityId == id)
            .Select(Mapping.ToMunicipalityDto)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Result<MunicipalityDto>> CreateAsync(MunicipalityInput input, CancellationToken cancellationToken = default)
    {
        await tenantInitializer.EnsureInitializedAsync(cancellationToken);
        if (!tenant.IsUnrestricted)
        {
            return Result<MunicipalityDto>.Forbidden("Only a Super Admin can create a municipality.");
        }

        if (InputValidator.Validate(input) is { } errors)
        {
            return Result<MunicipalityDto>.Invalid(errors);
        }

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        if (await FindDuplicateAsync(db, input, exceptId: null, cancellationToken) is { } duplicate)
        {
            return duplicate;
        }

        var entity = new Municipality();
        entity.Apply(input);
        db.Municipalities.Add(entity);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.IsUniqueViolation())
        {
            return Result<MunicipalityDto>.Conflict(nameof(input.Slug), "Another municipality already uses this name or URL slug.");
        }

        logger.LogInformation("Municipality {MunicipalityId} ({Slug}) created", entity.MunicipalityId, entity.Slug);
        return Result<MunicipalityDto>.Ok((await GetAsync(entity.MunicipalityId, cancellationToken))!);
    }

    public async Task<Result<MunicipalityDto>> UpdateAsync(int id, MunicipalityInput input, CancellationToken cancellationToken = default)
    {
        await tenantInitializer.EnsureInitializedAsync(cancellationToken);
        if (InputValidator.Validate(input) is { } errors)
        {
            return Result<MunicipalityDto>.Invalid(errors);
        }

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var entity = await db.Municipalities.FirstOrDefaultAsync(m => m.MunicipalityId == id, cancellationToken);
        if (entity is null)
        {
            return Result<MunicipalityDto>.NotFound(NotFoundMessage);
        }

        if (!tenant.IsUnrestricted && !string.Equals(entity.Slug, input.Slug, StringComparison.Ordinal))
        {
            return Result<MunicipalityDto>.Forbidden("Only a Super Admin can change a municipality's URL slug.");
        }

        if (await FindDuplicateAsync(db, input, id, cancellationToken) is { } duplicate)
        {
            return duplicate;
        }

        entity.Apply(input);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.IsUniqueViolation())
        {
            return Result<MunicipalityDto>.Conflict(nameof(input.Slug), "Another municipality already uses this name or URL slug.");
        }

        logger.LogInformation("Municipality {MunicipalityId} updated", id);
        return Result<MunicipalityDto>.Ok((await GetAsync(id, cancellationToken))!);
    }

    public async Task<Result<bool>> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await tenantInitializer.EnsureInitializedAsync(cancellationToken);
        if (!tenant.IsUnrestricted)
        {
            return Result<bool>.Forbidden("Only a Super Admin can delete a municipality.");
        }

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        if (!await db.Municipalities.AnyAsync(m => m.MunicipalityId == id, cancellationToken))
        {
            return Result<bool>.NotFound(NotFoundMessage);
        }

        await db.InTransactionAsync(async () =>
        {
            await db.Candidates.Where(c => c.MunicipalityId == id).ExecuteDeleteAsync(cancellationToken);
            await db.Positions.Where(p => p.MunicipalityId == id).ExecuteDeleteAsync(cancellationToken);
            await db.Municipalities.Where(m => m.MunicipalityId == id)
                .ExecuteUpdateAsync(s => s.SetProperty(m => m.LogoImageId, (Guid?)null), cancellationToken);
            await db.Images.Where(i => i.MunicipalityId == id).ExecuteDeleteAsync(cancellationToken);
            await db.Users.Where(u => u.MunicipalityId == id).ExecuteDeleteAsync(cancellationToken);
            await db.Municipalities.Where(m => m.MunicipalityId == id).ExecuteDeleteAsync(cancellationToken);
        }, cancellationToken);

        logger.LogWarning("Municipality {MunicipalityId} and all of its data were deleted", id);
        return Result<bool>.Ok(true);
    }

    public async Task<Result<Guid>> SetLogoAsync(int id, byte[] content, CancellationToken cancellationToken = default)
    {
        await tenantInitializer.EnsureInitializedAsync(cancellationToken);
        if (ImageInspector.Validate(content, out string contentType) is { } error)
        {
            return Result<Guid>.Invalid("Logo", error);
        }

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var entity = await db.Municipalities.Include(m => m.Logo)
            .FirstOrDefaultAsync(m => m.MunicipalityId == id, cancellationToken);
        if (entity is null)
        {
            return Result<Guid>.NotFound(NotFoundMessage);
        }

        var previous = entity.Logo;
        var image = ImageInspector.ToEntity(content, contentType, id);
        db.Images.Add(image);
        entity.Logo = image;
        if (previous is not null)
        {
            db.Images.Remove(previous);
        }

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Logo {ImageId} ({ContentType}, {Length} bytes) stored for municipality {MunicipalityId}",
            image.StoredImageId, contentType, content.Length, id);
        return Result<Guid>.Ok(image.StoredImageId);
    }

    public async Task<Result<bool>> RemoveLogoAsync(int id, CancellationToken cancellationToken = default)
    {
        await tenantInitializer.EnsureInitializedAsync(cancellationToken);
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var entity = await db.Municipalities.Include(m => m.Logo)
            .FirstOrDefaultAsync(m => m.MunicipalityId == id, cancellationToken);
        if (entity is null)
        {
            return Result<bool>.NotFound(NotFoundMessage);
        }

        if (entity.Logo is { } logo)
        {
            entity.Logo = null;
            db.Images.Remove(logo);
            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Logo removed from municipality {MunicipalityId}", id);
        }

        return Result<bool>.Ok(true);
    }

    public async Task<DashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        await tenantInitializer.EnsureInitializedAsync(cancellationToken);
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        return new DashboardDto
        {
            MunicipalityCount = await db.Municipalities.CountAsync(cancellationToken),
            PositionCount = await db.Positions.CountAsync(cancellationToken),
            CandidateCount = await db.Candidates.CountAsync(cancellationToken)
        };
    }

    private static async Task<Result<MunicipalityDto>?> FindDuplicateAsync(
        ApplicationDbContext db, MunicipalityInput input, int? exceptId, CancellationToken cancellationToken)
    {
        var others = db.Municipalities.IgnoreQueryFilters().Where(m => m.MunicipalityId != exceptId);

        if (await others.AnyAsync(m => m.Name == input.Name, cancellationToken))
        {
            return Result<MunicipalityDto>.Conflict(nameof(input.Name), "A municipality with this name already exists.");
        }

        if (await others.AnyAsync(m => m.Slug == input.Slug, cancellationToken))
        {
            return Result<MunicipalityDto>.Conflict(nameof(input.Slug), "This URL slug is already used by another municipality.");
        }

        return null;
    }
}
