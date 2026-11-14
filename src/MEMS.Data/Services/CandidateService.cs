using MEMS.Data.Entities;
using MEMS.Data.Tenancy;
using MEMS.Shared.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MEMS.Data.Services;

public interface ICandidateService
{
    Task<PagedResult<CandidateDto>> SearchAsync(CandidateQuery query, CancellationToken cancellationToken = default);
    Task<List<CandidateDto>> ListAsync(CancellationToken cancellationToken = default);
    Task<CandidateDto?> GetAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<CandidateDto>> CreateAsync(CandidateInput input, CancellationToken cancellationToken = default);
    Task<Result<CandidateDto>> UpdateAsync(int id, CandidateInput input, CancellationToken cancellationToken = default);
    Task<Result<bool>> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<Guid>> SetPhotoAsync(int id, byte[] content, CancellationToken cancellationToken = default);
    Task<Result<bool>> RemovePhotoAsync(int id, CancellationToken cancellationToken = default);
}

public sealed class CandidateService(
    IDbContextFactory<ApplicationDbContext> dbFactory,
    ITenantContextInitializer tenantInitializer,
    ILogger<CandidateService> logger) : ICandidateService
{
    private const string NotFoundMessage = "The candidate was not found.";
    private const string PositionMessage = "Choose a position that belongs to this municipality.";

    public async Task<PagedResult<CandidateDto>> SearchAsync(CandidateQuery query, CancellationToken cancellationToken = default)
    {
        await tenantInitializer.EnsureInitializedAsync(cancellationToken);
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        int pageSize = Math.Clamp(query.PageSize, 1, CandidateQuery.MaxPageSize);
        IQueryable<Candidate> candidates = db.Candidates.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            string search = query.Search.Trim();
            candidates = candidates.Where(c => c.Name.Contains(search));
        }

        if (query.PositionId is int positionId)
        {
            candidates = candidates.Where(c => c.PositionId == positionId);
        }

        int total = await candidates.CountAsync(cancellationToken);
        int totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
        int page = Math.Clamp(query.Page, 1, totalPages);

        var items = await candidates
            .OrderBy(c => c.Municipality!.Name).ThenBy(c => c.Position!.Name).ThenBy(c => c.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(Mapping.ToCandidateDto)
            .ToListAsync(cancellationToken);

        return new PagedResult<CandidateDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = total,
            TotalPages = totalPages
        };
    }

    public async Task<List<CandidateDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        await tenantInitializer.EnsureInitializedAsync(cancellationToken);
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.Candidates.AsNoTracking()
            .OrderBy(c => c.Municipality!.Name).ThenBy(c => c.Position!.Name).ThenBy(c => c.Name)
            .Select(Mapping.ToCandidateDto)
            .ToListAsync(cancellationToken);
    }

    public async Task<CandidateDto?> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        await tenantInitializer.EnsureInitializedAsync(cancellationToken);
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var candidate = await db.Candidates.AsNoTracking()
            .Where(c => c.CandidateId == id)
            .Select(Mapping.ToCandidateDto)
            .FirstOrDefaultAsync(cancellationToken);

        if (candidate is not null)
        {
            candidate.Endorsements = await db.Endorsements.AsNoTracking()
                .Where(e => e.CandidateId == id)
                .OrderBy(e => e.EndorserName)
                .Select(e => new EndorsementDto
                {
                    Id = e.EndorsementId,
                    EndorserName = e.EndorserName,
                    Organization = e.Organization,
                    Text = e.Text
                })
                .ToListAsync(cancellationToken);
        }

        return candidate;
    }

    public async Task<Result<CandidateDto>> CreateAsync(CandidateInput input, CancellationToken cancellationToken = default)
    {
        await tenantInitializer.EnsureInitializedAsync(cancellationToken);
        if (InputValidator.Validate(input) is { } errors)
        {
            return Result<CandidateDto>.Invalid(errors);
        }

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var position = await db.Positions.AsNoTracking()
            .FirstOrDefaultAsync(p => p.PositionId == input.PositionId, cancellationToken);
        if (position is null)
        {
            return Result<CandidateDto>.Invalid(nameof(input.PositionId), PositionMessage);
        }

        var entity = new Candidate
        {
            PositionId = position.PositionId,
            MunicipalityId = position.MunicipalityId,
            DateCreated = DateTime.UtcNow
        };
        entity.Apply(input);
        db.Candidates.Add(entity);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Candidate {CandidateId} created in municipality {MunicipalityId}",
            entity.CandidateId, entity.MunicipalityId);
        return Result<CandidateDto>.Ok((await GetAsync(entity.CandidateId, cancellationToken))!);
    }

    public async Task<Result<CandidateDto>> UpdateAsync(int id, CandidateInput input, CancellationToken cancellationToken = default)
    {
        await tenantInitializer.EnsureInitializedAsync(cancellationToken);
        if (InputValidator.Validate(input) is { } errors)
        {
            return Result<CandidateDto>.Invalid(errors);
        }

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var entity = await db.Candidates.FirstOrDefaultAsync(c => c.CandidateId == id, cancellationToken);
        if (entity is null)
        {
            return Result<CandidateDto>.NotFound(NotFoundMessage);
        }

        bool positionOk = await db.Positions.AnyAsync(
            p => p.PositionId == input.PositionId && p.MunicipalityId == entity.MunicipalityId, cancellationToken);
        if (!positionOk)
        {
            return Result<CandidateDto>.Invalid(nameof(input.PositionId), PositionMessage);
        }

        entity.Apply(input);
        entity.PositionId = input.PositionId!.Value;
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Candidate {CandidateId} updated", id);
        return Result<CandidateDto>.Ok((await GetAsync(id, cancellationToken))!);
    }

    public async Task<Result<bool>> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await tenantInitializer.EnsureInitializedAsync(cancellationToken);
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var entity = await db.Candidates.Include(c => c.Photo)
            .FirstOrDefaultAsync(c => c.CandidateId == id, cancellationToken);
        if (entity is null)
        {
            return Result<bool>.NotFound(NotFoundMessage);
        }

        if (entity.Photo is { } photo)
        {
            db.Images.Remove(photo);
        }

        db.Candidates.Remove(entity);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Candidate {CandidateId} deleted", id);
        return Result<bool>.Ok(true);
    }

    public async Task<Result<Guid>> SetPhotoAsync(int id, byte[] content, CancellationToken cancellationToken = default)
    {
        await tenantInitializer.EnsureInitializedAsync(cancellationToken);
        if (ImageInspector.Validate(content, out string contentType) is { } error)
        {
            return Result<Guid>.Invalid("Photo", error);
        }

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var entity = await db.Candidates.Include(c => c.Photo)
            .FirstOrDefaultAsync(c => c.CandidateId == id, cancellationToken);
        if (entity is null)
        {
            return Result<Guid>.NotFound(NotFoundMessage);
        }

        var previous = entity.Photo;
        var image = ImageInspector.ToEntity(content, contentType, entity.MunicipalityId);
        db.Images.Add(image);
        entity.Photo = image;
        if (previous is not null)
        {
            db.Images.Remove(previous);
        }

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Photograph {ImageId} ({ContentType}, {Length} bytes) stored for candidate {CandidateId}",
            image.StoredImageId, contentType, content.Length, id);
        return Result<Guid>.Ok(image.StoredImageId);
    }

    public async Task<Result<bool>> RemovePhotoAsync(int id, CancellationToken cancellationToken = default)
    {
        await tenantInitializer.EnsureInitializedAsync(cancellationToken);
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var entity = await db.Candidates.Include(c => c.Photo)
            .FirstOrDefaultAsync(c => c.CandidateId == id, cancellationToken);
        if (entity is null)
        {
            return Result<bool>.NotFound(NotFoundMessage);
        }

        if (entity.Photo is { } photo)
        {
            entity.Photo = null;
            db.Images.Remove(photo);
            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Photograph removed from candidate {CandidateId}", id);
        }

        return Result<bool>.Ok(true);
    }
}
