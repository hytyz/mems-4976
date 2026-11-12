using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace MEMS.Api.Infrastructure;

/// <summary>Adds the title, description and the bearer-token scheme to the OpenAPI document.</summary>
public sealed class ApiDocumentTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        document.Info = new OpenApiInfo
        {
            Title = "Municipal Elections Management System API",
            Version = "v1",
            Description =
                "Multi-tenant API. Tenant data lives under /api/{tenant}/..., where {tenant} is the municipality's URL slug " +
                "(for example /api/burnaby/candidates). Reads are public; writes need a bearer token from POST /api/auth/login " +
                "and are limited to the caller's own tenant unless the caller is a Super Admin."
        };

        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "Paste the accessToken returned by POST /api/auth/login."
        };

        var requirement = new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", document)] = []
        };

        // Only the write operations need the token.
        foreach (var operation in document.Paths.Values
                     .Where(path => path.Operations is not null)
                     .SelectMany(path => path.Operations!)
                     .Where(o => o.Key != HttpMethod.Get)
                     .Select(o => o.Value))
        {
            if (operation.OperationId?.Contains("Login", StringComparison.OrdinalIgnoreCase) == true)
            {
                continue;
            }

            operation.Security ??= [];
            operation.Security.Add(requirement);
        }

        return Task.CompletedTask;
    }
}
