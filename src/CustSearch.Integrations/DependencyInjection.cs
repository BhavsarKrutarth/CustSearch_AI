using CustSearch.Application.Integrations;
using Microsoft.Extensions.DependencyInjection;

namespace CustSearch.Integrations;

/// <summary>
/// Provides the composition boundary for POS, webhook and Python AI integrations.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddCustSearchIntegrations(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<IIntegrationSecretResolver,ConfigurationIntegrationSecretResolver>();
        services.AddHttpClient<IIntegrationTransport,HttpIntegrationTransport>(client=>client.Timeout=Timeout.InfiniteTimeSpan);
        return services;
    }
}
