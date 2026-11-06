namespace TemplateApp.External.ApiClients;

/// <summary>
/// Outcome of an API command (create, update, delete). <see cref="Error"/> holds the API's problem detail.
/// </summary>
public record ApiResult(bool Succeeded, string? Error = null)
{
    public static ApiResult Success() => new(true);

    public static ApiResult Failure(string error) => new(false, error);
}
