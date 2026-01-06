using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;

namespace LocaGuest.Infrastructure.Services;

public sealed class ForwardAuthorizationHeaderHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ForwardAuthorizationHeaderHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var context = _httpContextAccessor.HttpContext;
        var authHeader = context?.Request.Headers.Authorization.ToString();

        if (!string.IsNullOrWhiteSpace(authHeader) && AuthenticationHeaderValue.TryParse(authHeader, out var parsed))
        {
            request.Headers.Authorization = parsed;
        }

        return base.SendAsync(request, cancellationToken);
    }
}
