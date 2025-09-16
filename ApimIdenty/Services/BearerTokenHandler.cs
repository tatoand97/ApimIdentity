using System.Net.Http.Headers;
using ApimIdenty.Options;
using Azure.Core;
using Microsoft.Extensions.Options;

namespace ApimIdenty.Services;

/// <summary>
/// HttpMessageHandler that acquires Azure AD tokens using a TokenCredential and attaches them as bearer headers.
/// </summary>
public sealed class BearerTokenHandler : DelegatingHandler
{
    private readonly TokenCredential _credential;
    private readonly string[] _scopes;
    private AccessToken _cachedToken;
    private readonly object _lock = new();

    public BearerTokenHandler(TokenCredential credential, IOptions<ApimOptions> options)
    {
        _credential = credential ?? throw new ArgumentNullException(nameof(credential));
        ArgumentNullException.ThrowIfNull(options);

        var apimOptions = options.Value ?? throw new InvalidOperationException("Apim options are required");
        if (string.IsNullOrWhiteSpace(apimOptions.Scope))
        {
            throw new InvalidOperationException("Apim:Scope configuration is required");
        }

        _scopes = [apimOptions.Scope];
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var token = await GetTokenAsync(cancellationToken).ConfigureAwait(false);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);

        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }

    private async Task<AccessToken> GetTokenAsync(CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            if (_cachedToken.ExpiresOn > DateTimeOffset.UtcNow.AddMinutes(1))
            {
                return _cachedToken;
            }
        }

        var newToken = await _credential.GetTokenAsync(new TokenRequestContext(_scopes), cancellationToken).ConfigureAwait(false);

        lock (_lock)
        {
            _cachedToken = newToken;
            return _cachedToken;
        }
    }
}