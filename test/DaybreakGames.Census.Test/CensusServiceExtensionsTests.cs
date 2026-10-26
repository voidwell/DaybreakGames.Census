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

        Assert.Equal(ServiceLifetime.Transient, services.Single(d => d.ServiceType == typeof(ICensusClient)).Lifetime);
        Assert.Equal(ServiceLifetime.Transient, services.Single(d => d.ServiceType == typeof(ICensusQueryFactory)).Lifetime);
        Assert.Equal(ServiceLifetime.Transient, services.Single(d => d.ServiceType == typeof(ICensusStreamClient)).Lifetime);
    }

    [Fact]
    public void CensusAddServicesResolves()
    {
        using var provider = BuildProvider(services => services.AddCensusServices());

        Assert.IsType<CensusClient>(provider.GetRequiredService<ICensusClient>());
        Assert.IsType<CensusQueryFactory>(provider.GetRequiredService<ICensusQueryFactory>());
        Assert.IsType<CensusStreamClient>(provider.GetRequiredService<ICensusStreamClient>());
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
    public void CensusAddServicesRejectsNulls()
    {
        Assert.Throws<ArgumentNullException>(() => CensusServiceExtensions.AddCensusServices(null!));
        Assert.Throws<ArgumentNullException>(() => CensusServiceExtensions.AddCensusServices(null!, _ => { }));
        Assert.Throws<ArgumentNullException>(() => new ServiceCollection().AddCensusServices(null!));
    }

    [Fact]
    public void CensusAddNamedServicesResolvesKeyed()
    {
        using var provider = BuildProvider(services => services.AddCensusServices("pc", _ => { }));

        Assert.IsType<CensusClient>(provider.GetRequiredKeyedService<ICensusClient>("pc"));
        Assert.IsType<CensusQueryFactory>(provider.GetRequiredKeyedService<ICensusQueryFactory>("pc"));
        Assert.IsType<CensusStreamClient>(provider.GetRequiredKeyedService<ICensusStreamClient>("pc"));
        Assert.NotSame(provider.GetRequiredKeyedService<ICensusStreamClient>("pc"), provider.GetRequiredKeyedService<ICensusStreamClient>("pc"));
        Assert.Null(provider.GetService<ICensusClient>());
    }

    [Fact]
    public void CensusAddNamedServicesAppliesOptionsPerName()
    {
        using var provider = BuildProvider(services => services
            .AddCensusServices(o => o.CensusServiceId = "default")
            .AddCensusServices("pc", o =>
            {
                o.CensusServiceId = "pc-id";
                o.CensusServiceNamespace = "ps2:v2";
            })
            .AddCensusServices("ps4us", o =>
            {
                o.CensusServiceId = "ps4us-id";
                o.CensusServiceNamespace = "ps2ps4us:v2";
                o.UseHttps = false;
            }));

        Assert.Equal(
            new Uri($"https://{Constants.CensusEndpoint}/s:default/get/ps2/character/"),
            provider.GetRequiredService<ICensusQueryFactory>().Create("character").GetUri());
        Assert.Equal(
            new Uri($"https://{Constants.CensusEndpoint}/s:pc-id/get/ps2:v2/character/"),
            provider.GetRequiredKeyedService<ICensusQueryFactory>("pc").Create("character").GetUri());
        Assert.Equal(
            new Uri($"http://{Constants.CensusEndpoint}/s:ps4us-id/get/ps2ps4us:v2/character/"),
            provider.GetRequiredKeyedService<ICensusClient>("ps4us").CreateQuery("character").GetUri());
    }

    [Fact]
    public void CensusAddNamedServicesConfiguresHttpClientPerName()
    {
        using var provider = BuildProvider(services => services
            .AddCensusServices("pc", o =>
            {
                o.CensusTimeout = TimeSpan.FromSeconds(30);
                o.UserAgent = "pc-agent/1.0";
            })
            .AddCensusServices("ps4us", o => o.CensusTimeout = TimeSpan.FromSeconds(5)));

        var factory = provider.GetRequiredService<IHttpClientFactory>();
        var pc = factory.CreateClient(CensusServiceExtensions.GetCensusHttpClientName("pc"));
        var ps4us = factory.CreateClient(CensusServiceExtensions.GetCensusHttpClientName("ps4us"));

        Assert.Equal(TimeSpan.FromSeconds(30), pc.Timeout);
        Assert.Equal("pc-agent/1.0", pc.DefaultRequestHeaders.UserAgent.ToString());
        Assert.Equal(TimeSpan.FromSeconds(5), ps4us.Timeout);
        Assert.Empty(ps4us.DefaultRequestHeaders.UserAgent);
    }

    [Fact]
    public void CensusAddNamedServicesRejectsNulls()
    {
        Assert.Throws<ArgumentNullException>(() => CensusServiceExtensions.AddCensusServices(null!, "pc", _ => { }));
        Assert.Throws<ArgumentNullException>(() => new ServiceCollection().AddCensusServices(null!, _ => { }));
        Assert.Throws<ArgumentNullException>(() => new ServiceCollection().AddCensusServices(" ", _ => { }));
        Assert.Throws<ArgumentNullException>(() => new ServiceCollection().AddCensusServices("pc", null!));
    }

    private static ServiceProvider BuildProvider(Action<IServiceCollection> configure)
    {
        var services = new ServiceCollection();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        configure(services);

        return services.BuildServiceProvider();
    }
}
