using DaybreakGames.Census.Operators;
using Microsoft.Extensions.Options;

namespace DaybreakGames.Census.Test;

public class CensusQueryBuilderTests
{
    [Fact]
    public void CensusOptionsDefaults()
    {
        var options = new CensusOptions();

        Assert.Equal(Constants.DefaultServiceId, options.CensusServiceId);
        Assert.Equal(Constants.DefaultServiceNamespace, options.CensusServiceNamespace);
        Assert.Equal(Constants.CensusEndpoint, options.CensusApiEndpoint);
        Assert.Equal(Constants.CensusWebsocketEndpoint, options.CensusWebsocketEndpoint);
        Assert.Null(options.UserAgent);
        Assert.False(options.UseHttps);
        Assert.False(options.LogCensusErrors);
    }

    [Fact]
    public void CensusDefaultOptionsBuildUri()
    {
        using var client = Client(new CensusOptions());

        var uri = client.CreateQuery("character").GetUri();

        Assert.Equal(new Uri($"http://{Constants.CensusEndpoint}/s:example/get/ps2/character/"), uri);
    }

    [Fact]
    public void CensusQueryServiceIdOverridesOptions()
    {
        using var client = Client(new CensusOptions { CensusServiceId = "optionsKey" });

        var uri = client.CreateQuery("character").SetServiceId("queryKey").GetUri();

        Assert.Equal(new Uri($"http://{Constants.CensusEndpoint}/s:queryKey/get/ps2/character/"), uri);
    }

    [Fact]
    public void CensusQueryServiceNamespaceOverridesOptions()
    {
        using var client = Client(new CensusOptions());

        var uri = client.CreateQuery("character").SetServiceNamespace("ps2ps4eu:v2").GetUri();

        Assert.Equal(new Uri($"http://{Constants.CensusEndpoint}/s:example/get/ps2ps4eu:v2/character/"), uri);
    }

    [Fact]
    public void CensusCustomApiEndpoint()
    {
        using var client = Client(new CensusOptions { CensusApiEndpoint = "example.test:8080", UseHttps = true });

        var uri = client.CreateQuery("character").GetUri();

        Assert.Equal(new Uri("https://example.test:8080/s:example/get/ps2/character/"), uri);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CensusSetServiceIdRejectsEmpty(string? value)
    {
        var query = Query();

        Assert.Throws<ArgumentNullException>(() => query.SetServiceId(value!));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CensusSetServiceNamespaceRejectsEmpty(string? value)
    {
        var query = Query();

        Assert.Throws<ArgumentNullException>(() => query.SetServiceNamespace(value!));
    }

    [Fact]
    public void CensusSetServiceIdAndNamespaceStoreValues()
    {
        var query = Query().SetServiceId("key").SetServiceNamespace("ns");

        Assert.Equal("key", query.ServiceId);
        Assert.Equal("ns", query.ServiceNamespace);
        Assert.Equal("character", query.ServiceName);
    }

    [Theory]
    [InlineData(CensusLanguage.English, "en")]
    [InlineData(CensusLanguage.German, "de")]
    [InlineData(CensusLanguage.Spanish, "es")]
    [InlineData(CensusLanguage.French, "fr")]
    [InlineData(CensusLanguage.Italian, "it")]
    [InlineData(CensusLanguage.Turkish, "tr")]
    public void CensusSetLanguageEnum(CensusLanguage language, string expected)
    {
        var query = Query().SetLanguage(language);

        Assert.Equal(expected, query.Language);
        Assert.Equal($"character/?c:lang={expected}", query.ToString());
    }

    [Fact]
    public void CensusLimitAndStart()
    {
        var query = Query().SetLimit(10).SetStart(20);

        Assert.Equal(10, query.Limit);
        Assert.Equal(20, query.Start);
        Assert.Equal("character/?c:limit=10&c:start=20", query.ToString());
    }

    [Fact]
    public void CensusIncludeNull()
    {
        var query = Query().UseIncludeNull();

        Assert.True(query.IncludeNull);
        Assert.Equal("character/?c:includeNull=true", query.ToString());
    }

    [Fact]
    public void CensusShowAndHideAccumulate()
    {
        var query = Query().ShowFields("a").ShowFields("b", "c").HideFields("d").HideFields("e");

        Assert.Equal("character/?c:show=a,b,c&c:hide=d,e", query.ToString());
    }

    [Fact]
    public void CensusFactoryCreatesQueryForService()
    {
        using var client = Client(new CensusOptions());
        var factory = new CensusQueryFactory(client);

        var query = factory.Create("world");

        Assert.Equal("world", query.ServiceName);
        Assert.Equal("world/", query.ToString());
    }

    private static CensusQuery Query()
    {
        return new CensusQueryFactory(null!).Create("character");
    }

    private static CensusClient Client(CensusOptions options)
    {
        return new CensusClient(Options.Create(options), null!);
    }
}
