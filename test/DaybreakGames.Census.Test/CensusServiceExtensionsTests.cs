using DaybreakGames.Census.Stream;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace DaybreakGames.Census.Test;

public class CensusServiceExtensionsTests
{
    [Fact]
    public void CensusAddServicesRegistersLifetimes()
    {
        var services = new ServiceCollection().AddCensusServices();

        Assert.Equal(ServiceLifetime.Singleton, services.Single(d => d.ServiceType == typeof(ICensusClient)).Lifetime);
        Assert.Equal(ServiceLifetime.Singleton, services.Single(d => d.ServiceType == typeof(ICensusQueryFactory)).Lifetime);
        Assert.Equal(ServiceLifetime.Transient, services.Single(d => d.ServiceType == typeof(ICensusStreamClient)).Lifetime);
    }

    [Fact]
    public void CensusAddServicesResolves()
    {
        using var provider = BuildProvider(services => services.AddCensusServices());

        Assert.IsType<CensusClient>(provider.GetRequiredService<ICensusClient>());
        Assert.IsType<CensusQueryFactory>(provider.GetRequiredService<ICensusQueryFactory>());
        Assert.IsType<CensusStreamClient>(provider.GetRequiredService<ICensusStreamClient>());
        Assert.Same(provider.GetRequiredService<ICensusClient>(), provider.GetRequiredService<ICensusClient>());
        Assert.NotSame(provider.GetRequiredService<ICensusStreamClient>(), provider.GetRequiredService<ICensusStreamClient>());
    }

    [Fact]
    public void CensusAddServicesAppliesOptions()
    {
        using var provider = BuildProvider(services => services.AddCensusServices(o =>
        {
            o.CensusServiceId = "configured";
            o.UseHttps = true;
        }));

        var options = provider.GetRequiredService<IOptions<CensusOptions>>().Value;

        Assert.Equal("configured", options.CensusServiceId);
        Assert.True(options.UseHttps);
        Assert.Equal(
            new Uri($"https://{Constants.CensusEndpoint}/s:configured/get/ps2/character/"),
            provider.GetRequiredService<ICensusQueryFactory>().Create("character").GetUri());
    }

    [Fact]
    public void CensusAddServicesKeepsExistingRegistration()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ICensusClient>(new CensusClient(Options.Create(new CensusOptions()), NullLogger<CensusClient>.Instance));

        services.AddCensusServices();

        Assert.Equal(1, services.Count(d => d.ServiceType == typeof(ICensusClient)));
        Assert.NotNull(services.Single(d => d.ServiceType == typeof(ICensusClient)).ImplementationInstance);
    }

    [Fact]
    public void CensusAddServicesRejectsNulls()
    {
        Assert.Throws<ArgumentNullException>(() => CensusServiceExtensions.AddCensusServices(null!));
        Assert.Throws<ArgumentNullException>(() => CensusServiceExtensions.AddCensusServices(null!, _ => { }));
        Assert.Throws<ArgumentNullException>(() => new ServiceCollection().AddCensusServices(null!));
    }

    private static ServiceProvider BuildProvider(Action<IServiceCollection> configure)
    {
        var services = new ServiceCollection();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        configure(services);

        return services.BuildServiceProvider();
    }
}
