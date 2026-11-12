using MEMS.Data.Services;
using MEMS.Shared;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace MEMS.Api.Infrastructure;

/// <summary>
/// Last line of defence: logs the full exception for operators (visible in the Aspire dashboard)
/// and answers with a generic ProblemDetails. Stack traces, SQL and connection details never
/// reach the client.
/// </summary>
public sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetails, ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            return true; // The client went away; nothing to report.
        }

        (int status, string title, string detail) = exception switch
        {
            BadHttpRequestException bad => (bad.StatusCode, "Bad request", "The request could not be read. Check its size and format."),
            _ => (StatusCodes.Status500InternalServerError, "Unexpected error",
                "Something went wrong on our side. Please try again; if it keeps happening, contact the administrator and quote the trace id.")
        };

        if (status >= 500)
        {
            logger.LogError(exception, "Unhandled exception for {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
        }
        else
        {
            logger.LogWarning(exception, "Rejected request {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
        }

        httpContext.Response.StatusCode = status;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = new ProblemDetails { Status = status, Title = title, Detail = detail }
        });
    }
}

public static class ResultExtensions
{
    /// <summary>Translates a service result into the matching HTTP response.</summary>
    public static ActionResult ToProblem<T>(this ControllerBase controller, Result<T> result) => result.Status switch
    {
        ResultStatus.NotFound => controller.Problem(result.Message, statusCode: StatusCodes.Status404NotFound, title: "Not found"),
        ResultStatus.Forbidden => controller.Problem(result.Message, statusCode: StatusCodes.Status403Forbidden, title: "Forbidden"),
        ResultStatus.Conflict => ValidationProblem(controller, result, StatusCodes.Status409Conflict, "Conflict"),
        ResultStatus.Invalid => ValidationProblem(controller, result, StatusCodes.Status400BadRequest, "One or more validation errors occurred."),
        _ => throw new InvalidOperationException("A successful result is not a problem.")
    };

    private static ActionResult ValidationProblem<T>(ControllerBase controller, Result<T> result, int status, string title)
    {
        foreach (var (field, messages) in result.Errors)
        {
            foreach (string message in messages)
            {
                controller.ModelState.AddModelError(field, message);
            }
        }

        return controller.ValidationProblem(statusCode: status, title: title, modelStateDictionary: controller.ModelState);
    }

    /// <summary>Reads an uploaded image into memory, refusing anything over the size limit before buffering it.</summary>
    public static async Task<(byte[]? Content, string? Error)> ReadImageAsync(this IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return (null, "Choose an image file to upload.");
        }

        if (file.Length > ImageRules.MaxBytes)
        {
            return (null, $"The image must be {ImageRules.MaxSizeText} or smaller.");
        }

        using var buffer = new MemoryStream((int)file.Length);
        await file.CopyToAsync(buffer, cancellationToken);
        return (buffer.ToArray(), null);
    }
}

public sealed class JwtSettings
{
    public const string SectionName = "Jwt";
    public const int MinKeyLength = 32;

    public string SigningKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = "mems-api";
    public string Audience { get; set; } = "mems-clients";
    public int ExpiryMinutes { get; set; } = 30;
}
