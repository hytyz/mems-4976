using MEMS.Api.Infrastructure;
using MEMS.Api.Tenancy;
using MEMS.Data.Services;
using MEMS.Shared.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MEMS.Api.Controllers;

[ApiController]
[Route("api/municipalities")]
[Produces("application/json")]
[Tags("Municipalities (tenants)")]
public class MunicipalitiesController(ITenantDirectory directory, IMunicipalityService municipalities) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType<List<MunicipalityDto>>(StatusCodes.Status200OK)]
    public async Task<List<MunicipalityDto>> GetAll(CancellationToken cancellationToken) =>
        await directory.ListAsync(cancellationToken);

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
