using System.ComponentModel.DataAnnotations;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace MEMS.Data.Services;

public enum ResultStatus
{
    Ok,
    NotFound,
    Invalid,
    Conflict,
    Forbidden
}

/// <summary>
/// Outcome of a service call. Both front ends translate it: the API into HTTP status codes
/// and ProblemDetails, the Blazor admin into validation messages and alerts.
/// </summary>
public sealed class Result<T>
{
    private Result(ResultStatus status, T? value, string? message, IReadOnlyDictionary<string, string[]>? errors)
    {
        Status = status;
        Value = value;
        Message = message;
        Errors = errors ?? new Dictionary<string, string[]>();
    }

    public ResultStatus Status { get; }
    public T? Value { get; }

    /// <summary>A message that is safe to show to the user.</summary>
    public string? Message { get; }

    /// <summary>Field name → messages, for <see cref="ResultStatus.Invalid"/> and <see cref="ResultStatus.Conflict"/>.</summary>
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public bool IsSuccess => Status == ResultStatus.Ok;

    public static Result<T> Ok(T value) => new(ResultStatus.Ok, value, null, null);

    public static Result<T> NotFound(string message) => new(ResultStatus.NotFound, default, message, null);

    public static Result<T> Forbidden(string message) => new(ResultStatus.Forbidden, default, message, null);

    public static Result<T> Invalid(IReadOnlyDictionary<string, string[]> errors) =>
        new(ResultStatus.Invalid, default, "One or more fields are invalid.", errors);

    public static Result<T> Invalid(string field, string message) =>
        Invalid(new Dictionary<string, string[]> { [field] = [message] });

    public static Result<T> Conflict(string field, string message) =>
        new(ResultStatus.Conflict, default, message, new Dictionary<string, string[]> { [field] = [message] });
}

internal static class InputValidator
{
    /// <summary>
    /// Runs the data-annotation rules of a contract. The services call this themselves so the
    /// rules hold no matter which front end (or future client) is calling.
    /// </summary>
    public static IReadOnlyDictionary<string, string[]>? Validate(object input)
    {
        var results = new List<ValidationResult>();
        if (Validator.TryValidateObject(input, new ValidationContext(input), results, validateAllProperties: true))
        {
            return null;
        }

        return results
            .SelectMany(r => (r.MemberNames.Any() ? r.MemberNames : [string.Empty])
                .Select(member => (member, message: r.ErrorMessage ?? "Invalid value.")))
            .GroupBy(x => x.member, x => x.message)
            .ToDictionary(g => g.Key, g => g.ToArray());
    }
}

internal static class DbExtensions
{
    /// <summary>True when SQL Server rejected the save because of a unique index or constraint.</summary>
    public static bool IsUniqueViolation(this DbUpdateException exception) =>
        exception.InnerException is SqlException { Number: 2601 or 2627 };

    /// <summary>
    /// Runs several statements atomically. Goes through the execution strategy because the
    /// Aspire SQL Server integration enables connection retries, which forbid bare transactions.
    /// </summary>
    public static Task InTransactionAsync(this ApplicationDbContext db, Func<Task> work, CancellationToken cancellationToken)
    {
        var strategy = db.Database.CreateExecutionStrategy();
        return strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            await work();
            await transaction.CommitAsync(cancellationToken);
        });
    }
}
