using MEMS.Api.Infrastructure;
using MEMS.Api.Tenancy;
using MEMS.Data.Services;
using MEMS.Shared.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MEMS.Api.Controllers;

/// <summary>Tenant management. Each municipality is one tenant.</summary>
[ApiController]
[Route("api/municipalities")]
[Produces("application/json")]
[Tags("Municipalities (tenants)")]
public class MunicipalitiesController(ITenantDirectory directory, IMunicipalityService municipalities) : ControllerBase
{
    /// <summary>Lists every municipality: the public tenant directory.</summary>
    /// <remarks>Returns only each municipality's own public details, never positions or candidates.</remarks>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType<List<MunicipalityDto>>(StatusCodes.Status200OK)]
    public async Task<List<MunicipalityDto>> GetAll(CancellationToken cancellationToken) =>
        await directory.ListAsync(cancellationToken);

    /// <summary>Gets one municipality by id.</summary>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType<MunicipalityDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MunicipalityDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var municipality = await directory.FindByIdAsync(id, cancellationToken);
        return municipality is null
            ? Problem("The municipality was not found.", statusCode: StatusCodes.Status404NotFound, title: "Not found")
            : municipality;
    }

    /// <summary>Creates a municipality (a new tenant). Super Admin only.</summary>
    [HttpPost]
    [Authorize(Policy = ApiPolicies.SuperAdmin)]
    [ProducesResponseType<MunicipalityDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MunicipalityDto>> Create(MunicipalityInput input, CancellationToken cancellationToken)
    {
        var result = await municipalities.CreateAsync(input, cancellationToken);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value)
            : this.ToProblem(result);
    }

    /// <summary>Updates a municipality. A Municipality Administrator can only update their own, and cannot change its slug.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Policy = ApiPolicies.AnyAdmin)]
    [ProducesResponseType<MunicipalityDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MunicipalityDto>> Update(int id, MunicipalityInput input, CancellationToken cancellationToken)
    {
        var result = await municipalities.UpdateAsync(id, input, cancellationToken);
        return result.IsSuccess ? result.Value! : this.ToProblem(result);
    }

    /// <summary>Deletes a municipality with its positions, candidates, images and administrators. Super Admin only.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = ApiPolicies.SuperAdmin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await municipalities.DeleteAsync(id, cancellationToken);
        return result.IsSuccess ? NoContent() : this.ToProblem(result);
    }

    /// <summary>Uploads or replaces the municipality's logo (JPG, PNG, GIF or WEBP, at most 5 MB).</summary>
    [HttpPut("{id:int}/logo")]
    [Authorize(Policy = ApiPolicies.AnyAdmin)]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    [ProducesResponseType<ImageUploadResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ImageUploadResult>> UploadLogo(int id, IFormFile file, CancellationToken cancellationToken)
    {
        var (content, error) = await file.ReadImageAsync(cancellationToken);
        if (error is not null)
        {
            return this.ToProblem(Result<Guid>.Invalid("file", error));
        }

        var result = await municipalities.SetLogoAsync(id, content!, cancellationToken);
        return result.IsSuccess ? new ImageUploadResult { ImageId = result.Value } : this.ToProblem(result);
    }

    /// <summary>Removes the municipality's logo.</summary>
    [HttpDelete("{id:int}/logo")]
    [Authorize(Policy = ApiPolicies.AnyAdmin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> RemoveLogo(int id, CancellationToken cancellationToken)
    {
        var result = await municipalities.RemoveLogoAsync(id, cancellationToken);
        return result.IsSuccess ? NoContent() : this.ToProblem(result);
    }
}
