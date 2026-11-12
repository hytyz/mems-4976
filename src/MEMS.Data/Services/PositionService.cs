using MEMS.Data.Entities;
using MEMS.Data.Tenancy;
using MEMS.Shared.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MEMS.Data.Services;

public interface IPositionService
{
    Task<List<PositionDto>> ListAsync(CancellationToken cancellationToken = default);
    Task<PositionDto?> GetAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<PositionDto>> CreateAsync(int municipalityId, PositionInput input, CancellationToken cancellationToken = default);
    Task<Result<PositionDto>> UpdateAsync(int id, PositionInput input, CancellationToken cancellationToken = default);
    Task<Result<bool>> DeleteAsync(int id, CancellationToken cancellationToken = default);
}

public sealed class PositionService(
    IDbContextFactory<ApplicationDbContext> dbFactory,
    ITenantContext tenant,
    ITenantContextInitializer tenantInitializer,
    ILogger<PositionService> logger) : IPositionService
{
    private const string NotFoundMessage = "The position was not found.";
    private const string DuplicateMessage = "This municipality already has a position with that name.";

    public async Task<List<PositionDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        await tenantInitializer.EnsureInitializedAsync(cancellationToken);
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.Positions.AsNoTracking()
            .OrderBy(p => p.Municipality!.Name).ThenBy(p => p.Name)
            .Select(Mapping.ToPositionDto)
            .ToListAsync(cancellationToken);
    }

    public async Task<PositionDto?> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        await tenantInitializer.EnsureInitializedAsync(cancellationToken);
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.Positions.AsNoTracking()
            .Where(p => p.PositionId == id)
            .Select(Mapping.ToPositionDto)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Result<PositionDto>> CreateAsync(int municipalityId, PositionInput input, CancellationToken cancellationToken = default)
    {
        await tenantInitializer.EnsureInitializedAsync(cancellationToken);
        if (!tenant.CanAccess(municipalityId))
        {
            logger.LogWarning("Blocked an attempt to create a position in municipality {MunicipalityId} from outside its tenant", municipalityId);
            return Result<PositionDto>.Forbidden("You cannot add positions to that municipality.");
        }

        if (InputValidator.Validate(input) is { } errors)
        {
            return Result<PositionDto>.Invalid(errors);
        }

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        if (!await db.Municipalities.AnyAsync(m => m.MunicipalityId == municipalityId, cancellationToken))
        {
            return Result<PositionDto>.Invalid("MunicipalityId", "Choose a municipality.");
        }

        if (await db.Positions.AnyAsync(p => p.MunicipalityId == municipalityId && p.Name == input.Name, cancellationToken))
        {
            return Result<PositionDto>.Conflict(nameof(input.Name), DuplicateMessage);
        }

        var entity = new Position
        {
            Name = input.Name,
            SeatsAvailable = input.SeatsAvailable!.Value,
            MunicipalityId = municipalityId
        };
        db.Positions.Add(entity);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.IsUniqueViolation())
        {
            return Result<PositionDto>.Conflict(nameof(input.Name), DuplicateMessage);
        }

        logger.LogInformation("Position {PositionId} created in municipality {MunicipalityId}", entity.PositionId, municipalityId);
        return Result<PositionDto>.Ok((await GetAsync(entity.PositionId, cancellationToken))!);
    }

    public async Task<Result<PositionDto>> UpdateAsync(int id, PositionInput input, CancellationToken cancellationToken = default)
    {
        await tenantInitializer.EnsureInitializedAsync(cancellationToken);
        if (InputValidator.Validate(input) is { } errors)
        {
            return Result<PositionDto>.Invalid(errors);
        }

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var entity = await db.Positions.FirstOrDefaultAsync(p => p.PositionId == id, cancellationToken);
        if (entity is null)
        {
            return Result<PositionDto>.NotFound(NotFoundMessage);
        }

        if (await db.Positions.AnyAsync(
                p => p.MunicipalityId == entity.MunicipalityId && p.Name == input.Name && p.PositionId != id, cancellationToken))
        {
            return Result<PositionDto>.Conflict(nameof(input.Name), DuplicateMessage);
        }

        // A position never moves to another municipality, so MunicipalityId is not updatable.
        entity.Name = input.Name;
        entity.SeatsAvailable = input.SeatsAvailable!.Value;

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.IsUniqueViolation())
        {
            return Result<PositionDto>.Conflict(nameof(input.Name), DuplicateMessage);
        }

        logger.LogInformation("Position {PositionId} updated", id);
        return Result<PositionDto>.Ok((await GetAsync(id, cancellationToken))!);
    }

    public async Task<Result<bool>> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await tenantInitializer.EnsureInitializedAsync(cancellationToken);
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        if (!await db.Positions.AnyAsync(p => p.PositionId == id, cancellationToken))
        {
            return Result<bool>.NotFound(NotFoundMessage);
        }

        // Candidates go with their position (cascade); their photographs are removed explicitly.
        var photoIds = await db.Candidates
            .Where(c => c.PositionId == id && c.PhotoImageId != null)
            .Select(c => c.PhotoImageId!.Value)
            .ToListAsync(cancellationToken);

        await db.InTransactionAsync(async () =>
        {
            await db.Positions.Where(p => p.PositionId == id).ExecuteDeleteAsync(cancellationToken);
            if (photoIds.Count > 0)
            {
                await db.Images.Where(i => photoIds.Contains(i.StoredImageId)).ExecuteDeleteAsync(cancellationToken);
            }
        }, cancellationToken);

        logger.LogInformation("Position {PositionId} deleted with {PhotoCount} candidate photographs", id, photoIds.Count);
        return Result<bool>.Ok(true);
    }
}
