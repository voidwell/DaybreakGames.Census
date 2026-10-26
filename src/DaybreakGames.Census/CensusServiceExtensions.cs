using DaybreakGames.Census;
using DaybreakGames.Census.Stream;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for setting up DaybreakGames Census services in an <see cref="IServiceCollection" />.
/// </summary>
public static class CensusServiceExtensions
{
    /// <summary>
    /// Adds services required for DaybreakGames Census.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddCensusServices(this IServiceCollection services)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        services.AddOptions();

        ConfigureCensusServices(services);

        return services;
    }

    /// <summary>
    /// Adds services required for DaybreakGames Census.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <param name="setupAction">
    /// An <see cref="Action{CensusOptions}"/> to configure the <see cref="CensusOptions"/>.
    /// </param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddCensusServices(this IServiceCollection services, Action<CensusOptions> setupAction)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (setupAction == null)
        {
            throw new ArgumentNullException(nameof(setupAction));
        }

        ConfigureCensusServices(services);

        services.Configure(setupAction);

        return services;
    }

    /// <summary>
    /// Adds a named set of DaybreakGames Census services, configured independently of the default
    /// registration and of other named registrations. The services are registered as keyed services
    /// under <paramref name="name"/> and can be resolved with <c>[FromKeyedServices(name)]</c> or
    /// <see cref="ServiceProviderKeyedServiceExtensions.GetRequiredKeyedService{T}(IServiceProvider, object?)"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <param name="name">The key the services are registered under and the name of their <see cref="CensusOptions"/>.</param>
    /// <param name="setupAction">
    /// An <see cref="Action{CensusOptions}"/> to configure the named <see cref="CensusOptions"/>.
    /// </param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddCensusServices(this IServiceCollection services, string name, Action<CensusOptions> setupAction)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentNullException(nameof(name));
        }

        if (setupAction == null)
        {
            throw new ArgumentNullException(nameof(setupAction));
        }

        ConfigureNamedCensusServices(services, name);

        services.Configure(name, setupAction);

        return services;
    }

    /// <summary>
    /// Gets the name of the <see cref="HttpClient"/> used by the Census services registered under <paramref name="name"/>.
    /// Use it with <c>AddHttpClient(name)</c> to customize that client further.
    /// </summary>
    /// <param name="name">The name passed to <see cref="AddCensusServices(IServiceCollection, string, Action{CensusOptions})"/>.</param>
    public static string GetCensusHttpClientName(string name) => $"{nameof(ICensusClient)}.{name}";

    internal static void ConfigureCensusServices(IServiceCollection services)
    {
        services.AddHttpClient<ICensusClient, CensusClient>((sp, client) =>
        {
            ConfigureHttpClient(client, sp.GetRequiredService<IOptions<CensusOptions>>().Value);
        });

        services.TryAddTransient<ICensusQueryFactory, CensusQueryFactory>();
        services.TryAddTransient<ICensusStreamClient, CensusStreamClient>();
    }

    internal static void ConfigureNamedCensusServices(IServiceCollection services, string name)
    {
        var httpClientName = GetCensusHttpClientName(name);

        services.AddHttpClient(httpClientName, (sp, client) =>
        {
            ConfigureHttpClient(client, sp.GetRequiredService<IOptionsMonitor<CensusOptions>>().Get(name));
        });

        services.TryAddKeyedTransient<ICensusClient>(name, (sp, _) => new CensusClient(
            sp.GetRequiredService<IHttpClientFactory>().CreateClient(httpClientName),
            GetNamedOptions(sp, name),
            sp.GetRequiredService<ILogger<CensusClient>>()));

        services.TryAddKeyedTransient<ICensusQueryFactory>(name, (sp, key) =>
            new CensusQueryFactory(sp.GetRequiredKeyedService<ICensusClient>(key)));

        services.TryAddKeyedTransient<ICensusStreamClient>(name, (sp, _) => new CensusStreamClient(
            GetNamedOptions(sp, name),
            sp.GetRequiredService<ILogger<CensusStreamClient>>()));
    }

    private static IOptions<CensusOptions> GetNamedOptions(IServiceProvider sp, string name)
    {
        return Options.Options.Create(sp.GetRequiredService<IOptionsMonitor<CensusOptions>>().Get(name));
    }

    private static void ConfigureHttpClient(HttpClient client, CensusOptions options)
    {
        client.Timeout = options.CensusTimeout;

        if (options.UserAgent != null)
        {
            client.DefaultRequestHeaders.UserAgent.TryParseAdd(options.UserAgent);
        }
    }
}
