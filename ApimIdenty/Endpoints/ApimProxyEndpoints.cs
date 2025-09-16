using ApimIdenty.Options;
using ApimIdenty.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;

namespace ApimIdenty.Endpoints;

public static class ApimProxyEndpoints
{
    public static IEndpointRouteBuilder MapApimProxyEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/apim-proxy", async (ApimClient client, IOptions<ApimOptions> options, HttpRequest httpRequest, CancellationToken cancellationToken) =>
        {
            return await ForwardAsync(HttpMethod.Get, client, options.Value, httpRequest, cancellationToken);
        });

        endpoints.MapPost("/apim-proxy", async (ApimClient client, IOptions<ApimOptions> options, HttpRequest httpRequest, CancellationToken cancellationToken) =>
        {
            return await ForwardAsync(HttpMethod.Post, client, options.Value, httpRequest, cancellationToken);
        });

        endpoints.MapPut("/apim-proxy", async (ApimClient client, IOptions<ApimOptions> options, HttpRequest httpRequest, CancellationToken cancellationToken) =>
        {
            return await ForwardAsync(HttpMethod.Put, client, options.Value, httpRequest, cancellationToken);
        });

        return endpoints;
    }

    private static async Task<IResult> ForwardAsync(HttpMethod method, ApimClient client, ApimOptions options, HttpRequest httpRequest, CancellationToken cancellationToken)
    {
        var relativeUrl = BuildRelativeUrl(options, httpRequest);

        try
        {
            using var requestMessage = new HttpRequestMessage(method, relativeUrl);

            var content = CreateContentFrom(httpRequest);
            if (content is not null)
            {
                requestMessage.Content = content;
            }

            using var response = await client.SendAsync(requestMessage, cancellationToken);
            var payload = await response.Content.ReadAsStringAsync(cancellationToken);
            var contentType = response.Content.Headers.ContentType?.ToString();

            return Results.Content(payload, contentType, statusCode: (int)response.StatusCode);
        }
        catch (OperationCanceledException)
        {
            return Results.StatusCode(499);
        }
        catch (HttpRequestException)
        {
            return Results.StatusCode(StatusCodes.Status502BadGateway);
        }
        catch
        {
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    private static string BuildRelativeUrl(ApimOptions options, HttpRequest httpRequest)
    {
        var relativePath = string.IsNullOrWhiteSpace(options.SampleEndpoint) ? "/status" : options.SampleEndpoint!;
        var query = httpRequest.QueryString.HasValue ? httpRequest.QueryString.Value : string.Empty;
        return string.Concat(relativePath, query);
    }

    private static StreamContent? CreateContentFrom(HttpRequest httpRequest)
    {
        if (httpRequest.ContentLength is not > 0)
        {
            return null;
        }

        var content = new StreamContent(httpRequest.Body);
        if (!string.IsNullOrWhiteSpace(httpRequest.ContentType))
        {
            content.Headers.ContentType = MediaTypeHeaderValue.Parse(httpRequest.ContentType);
        }
        return content;
    }
}
