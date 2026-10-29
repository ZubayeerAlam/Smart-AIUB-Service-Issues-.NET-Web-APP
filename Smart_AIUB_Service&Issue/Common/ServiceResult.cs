using Microsoft.AspNetCore.Identity;

namespace Smart_AIUB_Service_Issue.Common;

/// <summary>
/// What a service method returns when it changes data: success, or a readable error
/// (optionally tied to one form field so the view can show it next to that input).
/// </summary>
public class ServiceResult
{
    public bool Succeeded { get; private init; }
    public string? Error { get; private init; }

    /// <summary>Name of the form field the error belongs to; null = whole form.</summary>
    public string? Field { get; private init; }

    public static ServiceResult Success() => new() { Succeeded = true };

    public static ServiceResult Fail(string error, string? field = null) =>
        new() { Error = error, Field = field };

    public static ServiceResult FromIdentity(IdentityResult result) =>
        result.Succeeded
            ? Success()
            : Fail(string.Join(" ", result.Errors.Select(e => e.Description)));
}
