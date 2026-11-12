using MEMS.Api.Infrastructure;
using MEMS.Api.Tenancy;
using MEMS.Data.Services;
using MEMS.Shared.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace MEMS.Api.Controllers;

// Every endpoint in this file lives under /api/{tenant}. By the time a controller runs,
// TenantResolutionMiddleware has resolved the slug and restricted the DbContext to that
// municipality, so an id that belongs to another tenant is simply "not found" here.

/// <summary>The public profile of the tenant named in the URL.</summary>
[ApiController]
[Route("api/{tenant}")]
[Produces("application/json")]
[Tags("Tenant")]
public class TenantController(ICurrentTenant tenant) : ControllerBase
{
    /// <summary>Gets the municipality for this tenant: details, election date, branding and logo.</summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType<MunicipalityDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public MunicipalityDto Get() => tenant.Required;
}

[ApiController]
[Route("api/{tenant}/positions")]
[Produces("application/json")]
[Tags("Positions")]
public class PositionsController(IPositionService positions, ICurrentTenant tenant) : ControllerBase
{
    /// <summary>Lists the tenant's positions.</summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType<List<PositionDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<List<PositionDto>> GetAll(CancellationToken cancellationToken) =>
        await positions.ListAsync(cancellationToken);

    /// <summary>Gets one of the tenant's positions.</summary>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType<PositionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PositionDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var position = await positions.GetAsync(id, cancellationToken);
        return position is null
            ? Problem("The position was not found.", statusCode: StatusCodes.Status404NotFound, title: "Not found")
            : position;
    }

    /// <summary>Creates a position in the tenant.</summary>
    [HttpPost]
    [Authorize(Policy = ApiPolicies.TenantAdmin)]
    [ProducesResponseType<PositionDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PositionDto>> Create(PositionInput input, CancellationToken cancellationToken)
    {
        // The municipality always comes from the URL; a client cannot name another one.
        var result = await positions.CreateAsync(tenant.Required.Id, input, cancellationToken);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { tenant = tenant.Required.Slug, id = result.Value!.Id }, result.Value)
            : this.ToProblem(result);
    }

    /// <summary>Updates one of the tenant's positions.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Policy = ApiPolicies.TenantAdmin)]
    [ProducesResponseType<PositionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PositionDto>> Update(int id, PositionInput input, CancellationToken cancellationToken)
    {
        var result = await positions.UpdateAsync(id, input, cancellationToken);
        return result.IsSuccess ? result.Value! : this.ToProblem(result);
    }

    /// <summary>Deletes one of the tenant's positions together with its candidates.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = ApiPolicies.TenantAdmin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await positions.DeleteAsync(id, cancellationToken);
        return result.IsSuccess ? NoContent() : this.ToProblem(result);
    }
}

[ApiController]
[Route("api/{tenant}/candidates")]
[Produces("application/json")]
[Tags("Candidates")]
public class CandidatesController(ICandidateService candidates, ICurrentTenant tenant) : ControllerBase
{
    /// <summary>Lists the tenant's candidates, with optional name search, position filter and paging.</summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType<PagedResult<CandidateDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<PagedResult<CandidateDto>> GetAll([FromQuery] CandidateQuery query, CancellationToken cancellationToken) =>
        await candidates.SearchAsync(query, cancellationToken);

    /// <summary>Gets one of the tenant's candidates, including endorsements.</summary>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType<CandidateDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CandidateDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var candidate = await candidates.GetAsync(id, cancellationToken);
        return candidate is null
            ? Problem("The candidate was not found.", statusCode: StatusCodes.Status404NotFound, title: "Not found")
            : candidate;
    }

    /// <summary>Creates a candidate. The position must belong to the tenant.</summary>
    [HttpPost]
    [Authorize(Policy = ApiPolicies.TenantAdmin)]
    [ProducesResponseType<CandidateDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<CandidateDto>> Create(CandidateInput input, CancellationToken cancellationToken)
    {
        var result = await candidates.CreateAsync(input, cancellationToken);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { tenant = tenant.Required.Slug, id = result.Value!.Id }, result.Value)
            : this.ToProblem(result);
    }

    /// <summary>Updates one of the tenant's candidates.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Policy = ApiPolicies.TenantAdmin)]
    [ProducesResponseType<CandidateDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CandidateDto>> Update(int id, CandidateInput input, CancellationToken cancellationToken)
    {
        var result = await candidates.UpdateAsync(id, input, cancellationToken);
        return result.IsSuccess ? result.Value! : this.ToProblem(result);
    }

    /// <summary>Deletes one of the tenant's candidates.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = ApiPolicies.TenantAdmin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await candidates.DeleteAsync(id, cancellationToken);
        return result.IsSuccess ? NoContent() : this.ToProblem(result);
    }

    /// <summary>Uploads or replaces the candidate's photograph (JPG, PNG, GIF or WEBP, at most 5 MB).</summary>
    [HttpPut("{id:int}/photo")]
    [Authorize(Policy = ApiPolicies.TenantAdmin)]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    [ProducesResponseType<ImageUploadResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ImageUploadResult>> UploadPhoto(int id, IFormFile file, CancellationToken cancellationToken)
    {
        var (content, error) = await file.ReadImageAsync(cancellationToken);
        if (error is not null)
        {
            return this.ToProblem(Result<Guid>.Invalid("file", error));
        }

        var result = await candidates.SetPhotoAsync(id, content!, cancellationToken);
        return result.IsSuccess ? new ImageUploadResult { ImageId = result.Value } : this.ToProblem(result);
    }

    /// <summary>Removes the candidate's photograph.</summary>
    [HttpDelete("{id:int}/photo")]
    [Authorize(Policy = ApiPolicies.TenantAdmin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> RemovePhoto(int id, CancellationToken cancellationToken)
    {
        var result = await candidates.RemovePhotoAsync(id, cancellationToken);
        return result.IsSuccess ? NoContent() : this.ToProblem(result);
    }
}

[ApiController]
[Route("api/{tenant}/images")]
[Tags("Images")]
public class ImagesController(IImageService images) : ControllerBase
{
    /// <summary>Serves one of the tenant's logos or candidate photographs.</summary>
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [Produces("image/jpeg", "image/png", "image/gif", "image/webp")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var image = await images.GetAsync(id, cancellationToken);
        if (image is null)
        {
            return Problem("The image was not found.", statusCode: StatusCodes.Status404NotFound, title: "Not found");
        }

        // An image id is never reused (a replacement gets a new id), so it can be cached for good.
        Response.Headers[HeaderNames.CacheControl] = "public, max-age=31536000, immutable";
        Response.Headers[HeaderNames.XContentTypeOptions] = "nosniff";
        return File(image.Data, image.ContentType);
    }
}
