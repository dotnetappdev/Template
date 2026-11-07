using System.Net.Http.Headers;
using TemplateApp.External.Auth;

namespace TemplateApp.External.ApiClients;

/// <summary>
/// Sends the signed-in user's API access token (kept in the site's sign-in cookie) with every API call,
/// refreshing it first when it has expired.
/// </summary>
public class UserTokenHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContext;

    public UserTokenHandler(IHttpContextAccessor httpContext)
    {
        _httpContext = httpContext;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.Headers.Authorization is null && _httpContext.HttpContext is { } context)
        {
            var session = context.RequestServices.GetRequiredService<IUserSession>();
            var token = await session.GetAccessTokenAsync();
            if (token is not null)
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
