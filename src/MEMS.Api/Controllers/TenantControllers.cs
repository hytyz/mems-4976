using MEMS.Api.Infrastructure;
using MEMS.Api.Tenancy;
using MEMS.Data.Services;
using MEMS.Shared.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace MEMS.Api.Controllers;

[ApiController]
[Route("api/{tenant}")]
[Produces("application/json")]
[Tags("Tenant")]
public class TenantController(ICurrentTenant tenant) : ControllerBase
{
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
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType<List<PositionDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<List<PositionDto>> GetAll(CancellationToken cancellationToken) =>
        await positions.ListAsync(cancellationToken);

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

    [HttpPost]
    [Authorize(Policy = ApiPolicies.TenantAdmin)]
    [ProducesResponseType<PositionDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PositionDto>> Create(PositionInput input, CancellationToken cancellationToken)
    {
        var result = await positions.CreateAsync(tenant.Required.Id, input, cancellationToken);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { tenant = tenant.Required.Slug, id = result.Value!.Id }, result.Value)
            : this.ToProblem(result);
    }

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
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType<PagedResult<CandidateDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<PagedResult<CandidateDto>> GetAll([FromQuery] CandidateQuery query, CancellationToken cancellationToken) =>
        await candidates.SearchAsync(query, cancellationToken);

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

        Response.Headers[HeaderNames.CacheControl] = "public, max-age=31536000, immutable";
        Response.Headers[HeaderNames.XContentTypeOptions] = "nosniff";
        return File(image.Data, image.ContentType);
    }
}
