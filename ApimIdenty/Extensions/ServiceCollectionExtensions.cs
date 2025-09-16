using ApimIdenty.Options;
using ApimIdenty.Services;
using Azure.Core;
using Azure.Identity;


namespace ApimIdenty.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();

        services.AddOptions<ApimOptions>()
            .Bind(configuration.GetSection("Apim"))
            .ValidateDataAnnotations()
            .Validate(
                options => Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out _),
                "Apim:BaseUrl configuration must be a valid absolute URI.")
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.Scope),
                "Apim:Scope configuration is required.");

        services.AddSingleton<TokenCredential, DefaultAzureCredential>();
        services.AddTransient<BearerTokenHandler>();

        services.AddHttpClient<ApimClient>()
            .AddHttpMessageHandler<BearerTokenHandler>();

        return services;
    }
}