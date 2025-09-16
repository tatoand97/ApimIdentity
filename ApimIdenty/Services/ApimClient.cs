using System.Net.Http.Headers;
using ApimIdenty.Options;
using Microsoft.Extensions.Options;

namespace ApimIdenty.Services;

public sealed class ApimClient
{
    private readonly HttpClient _httpClient;

    public ApimClient(HttpClient httpClient, IOptions<ApimOptions> options)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        ArgumentNullException.ThrowIfNull(options);

        var apimOptions = options.Value ?? throw new InvalidOperationException("Apim options are required");
        if (!Uri.TryCreate(apimOptions.BaseUrl, UriKind.Absolute, out var baseUri))
        {
            throw new InvalidOperationException("Apim:BaseUrl configuration must be a valid absolute URI");
        }

        _httpClient.BaseAddress = baseUri;
        _httpClient.DefaultRequestHeaders.Accept.Clear();
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }
}