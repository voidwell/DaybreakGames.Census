using DaybreakGames.Census;
using DaybreakGames.Census.Stream;
using Microsoft.Extensions.DependencyInjection.Extensions;
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

    internal static void ConfigureCensusServices(IServiceCollection services)
    {
        services.AddHttpClient<ICensusClient, CensusClient>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<CensusOptions>>().Value;

            client.Timeout = options.CensusTimeout;

            if (options.UserAgent != null)
            {
                client.DefaultRequestHeaders.UserAgent.TryParseAdd(options.UserAgent);
            }
        });

        services.TryAddTransient<ICensusQueryFactory, CensusQueryFactory>();
        services.TryAddTransient<ICensusStreamClient, CensusStreamClient>();
    }
}
