using System.Security.Cryptography;
using System.Text;

namespace TemplateApp.API.Security;

/// <summary>
/// Requires a shared key in the X-Api-Key header on /api requests. The Internal and External sites
/// send it from their Api:ApiKey setting. Configure the expected key in Security:ApiKey
/// (user-secrets / Key Vault outside Development).
/// </summary>
public class ApiKeyMiddleware
{
    public const string HeaderName = "X-Api-Key";

    private readonly RequestDelegate _next;
    private readonly byte[] _expectedKey;

    public ApiKeyMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        var key = configuration["Security:ApiKey"];
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new InvalidOperationException("Security:ApiKey is not configured.");
        }

        _expectedKey = Encoding.UTF8.GetBytes(key);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path;
        var isProtected = path.StartsWithSegments("/api") && !path.StartsWithSegments("/api/status");

        if (isProtected)
        {
            var supplied = context.Request.Headers[HeaderName].ToString();
            if (!CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(supplied), _expectedKey))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new { title = "Missing or invalid API key", status = 401 });
                return;
            }
        }

        await _next(context);
    }
}
