using Microsoft.AspNetCore.Identity;

namespace Smart_AIUB_Service_Issue.Common;

/// <summary>
/// What a service method returns when it changes data: success, or a readable error
/// (optionally tied to one form field so the view can show it next to that input).
/// </summary>
public class ServiceResult
{
    public bool Succeeded { get; protected init; }
    public string? Error { get; protected init; }

    /// <summary>Name of the form field the error belongs to; null = whole form.</summary>
    public string? Field { get; protected init; }

    public static ServiceResult Success() => new() { Succeeded = true };

    public static ServiceResult Fail(string error, string? field = null) =>
        new() { Error = error, Field = field };

    public static ServiceResult FromIdentity(IdentityResult result) =>
        result.Succeeded
            ? Success()
            : Fail(string.Join(" ", result.Errors.Select(e => e.Description)));
}

/// <summary>A ServiceResult that also returns a value on success (e.g. the new issue's Id).</summary>
public class ServiceResult<T> : ServiceResult
{
    public T? Value { get; private init; }

    public static ServiceResult<T> Success(T value) => new() { Succeeded = true, Value = value };

    public static new ServiceResult<T> Fail(string error, string? field = null) =>
        new() { Error = error, Field = field };
}
