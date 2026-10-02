using Microsoft.Extensions.Options;

namespace DaybreakGames.Census.Test;

public class CensusUriTest
{
    [Fact]
    public void CensusCreatesBaseUri()
    {
        var service = "character";
        var ns = "ps2";
        var key = "testkey";

        var expectedUri = new Uri($"http://{Constants.CensusEndpoint}/s:{key}/get/{ns}/{service}/");

        var query = GetCensusQueryFactory().Create(service);

        var censusUri = query.GetUri();

        Assert.Equal(expectedUri, censusUri);
    }

    [Fact]
    public void CensusConditionalEquals()
    {
        var service = "character";
        var ns = "ps2";
        var key = "testkey";

        var expectedUri = new Uri($"http://{Constants.CensusEndpoint}/s:{key}/get/{ns}/{service}/?field=12345");

        var query = GetCensusQueryFactory().Create(service);

        query.Where("field").Equals("12345");

        var censusUri = query.GetUri();

        Assert.Equal(expectedUri, censusUri);
    }

    [Fact]
    public void CensusConditionalLessThan()
    {
        var service = "character";
        var ns = "ps2";
        var key = "testkey";

        var expectedUri = new Uri($"http://{Constants.CensusEndpoint}/s:{key}/get/{ns}/{service}/?field=<12345");

        var query = GetCensusQueryFactory().Create(service);

        query.Where("field").IsLessThan(12345);

        var censusUri = query.GetUri();

        Assert.Equal(expectedUri, censusUri);
    }

    [Fact]
    public void CensusConditionalLessThanOrEquals()
    {
        var service = "character";
        var ns = "ps2";
        var key = "testkey";

        var expectedUri = new Uri($"http://{Constants.CensusEndpoint}/s:{key}/get/{ns}/{service}/?field=[12345");

        var query = GetCensusQueryFactory().Create(service);

        query.Where("field").IsLessThanOrEquals(12345);

        var censusUri = query.GetUri();

        Assert.Equal(expectedUri, censusUri);
    }

    [Fact]
    public void CensusConditionalGreaterThan()
    {
        var service = "character";
        var ns = "ps2";
        var key = "testkey";

        var expectedUri = new Uri($"http://{Constants.CensusEndpoint}/s:{key}/get/{ns}/{service}/?field=>12345");

        var query = GetCensusQueryFactory().Create(service);

        query.Where("field").IsGreaterThan(12345);

        var censusUri = query.GetUri();

        Assert.Equal(expectedUri, censusUri);
    }

    [Fact]
    public void CensusConditionalGreaterThanOrEquals()
    {
        var service = "character";
        var ns = "ps2";
        var key = "testkey";

        var expectedUri = new Uri($"http://{Constants.CensusEndpoint}/s:{key}/get/{ns}/{service}/?field=]12345");

        var query = GetCensusQueryFactory().Create(service);

        query.Where("field").IsGreaterThanOrEquals(12345);

        var censusUri = query.GetUri();

        Assert.Equal(expectedUri, censusUri);
    }

    [Fact]
    public void CensusConditionalStartsWith()
    {
        var service = "character";
        var ns = "ps2";
        var key = "testkey";

        var expectedUri = new Uri($"http://{Constants.CensusEndpoint}/s:{key}/get/{ns}/{service}/?field=^12345");

        var query = GetCensusQueryFactory().Create(service);

        query.Where("field").StartsWith("12345");

        var censusUri = query.GetUri();

        Assert.Equal(expectedUri, censusUri);
    }

    [Fact]
    public void CensusConditionalContains()
    {
        var service = "character";
        var ns = "ps2";
        var key = "testkey";

        var expectedUri = new Uri($"http://{Constants.CensusEndpoint}/s:{key}/get/{ns}/{service}/?field=*12345");

        var query = GetCensusQueryFactory().Create(service);

        query.Where("field").Contains("12345");

        var censusUri = query.GetUri();

        Assert.Equal(expectedUri, censusUri);
    }

    [Fact]
    public void CensusConditionalNotEquals()
    {
        var service = "character";
        var ns = "ps2";
        var key = "testkey";

        var expectedUri = new Uri($"http://{Constants.CensusEndpoint}/s:{key}/get/{ns}/{service}/?field=!12345");

        var query = GetCensusQueryFactory().Create(service);

        query.Where("field").NotEquals("12345");

        var censusUri = query.GetUri();

        Assert.Equal(expectedUri, censusUri);
    }

    [Fact]
    public void CensusConditionalMultipleConditions()
    {
        var service = "character";
        var ns = "ps2";
        var key = "testkey";

        var expectedUri = new Uri($"http://{Constants.CensusEndpoint}/s:{key}/get/{ns}/{service}/?field1=12&field2=34&field3=56");

        var query = GetCensusQueryFactory().Create(service);

        query.Where("field1").Equals("12");
        query.Where("field2").Equals("34");
        query.Where("field3").Equals("56");

        var censusUri = query.GetUri();

        Assert.Equal(expectedUri, censusUri);
    }

    [Fact]
    public void CensusAddResolve()
    {
        var service = "character";
        var ns = "ps2";
        var key = "testkey";

        var expectedUri = new Uri($"http://{Constants.CensusEndpoint}/s:{key}/get/{ns}/{service}/?c:resolve=items");

        var query = GetCensusQueryFactory().Create(service);

        query.AddResolve("items");

        var censusUri = query.GetUri();

        Assert.Equal(expectedUri, censusUri);
    }

    [Fact]
    public void CensusAddResolveMany()
    {
        var service = "character";
        var ns = "ps2";
        var key = "testkey";

        var expectedUri = new Uri($"http://{Constants.CensusEndpoint}/s:{key}/get/{ns}/{service}/?c:resolve=items,stuff");

        var query = GetCensusQueryFactory().Create(service);

        query.AddResolve(new[] { "items", "stuff" });

        var censusUri = query.GetUri();

        Assert.Equal(expectedUri, censusUri);
    }

    [Fact]
    public void CensusAddLanguage()
    {
        var service = "character";
        var ns = "ps2";
        var key = "testkey";

        var expectedUri = new Uri($"http://{Constants.CensusEndpoint}/s:{key}/get/{ns}/{service}/?c:lang=en");

        var query = GetCensusQueryFactory().Create(service);

        query.SetLanguage("en");

        var censusUri = query.GetUri();

        Assert.Equal(expectedUri, censusUri);
    }

    [Fact]
    public void CensusAddJoin()
    {
        var service = "character";
        var ns = "ps2";
        var key = "testkey";

        var expectedUri = new Uri($"http://{Constants.CensusEndpoint}/s:{key}/get/{ns}/{service}/?c:join=joinedservice^terms:field1=12'field2=34^on:ontestfield^to:totestfield^inject_at:testinject");

        var query = GetCensusQueryFactory().Create(service);

        var joinedService = query.JoinService("joinedservice");
        joinedService.ToField("totestfield");
        joinedService.OnField("ontestfield");
        joinedService.WithInjectAt("testinject");
        joinedService.Where("field1").Equals("12");
        joinedService.Where("field2").Equals("34");

        var censusUri = query.GetUri();

        Assert.Equal(expectedUri, censusUri);
    }

    [Fact]
    public void CensusAddJoinWithSubJoin()
    {
        var service = "character";
        var ns = "ps2";
        var key = "testkey";

        var expectedUri = new Uri($"http://{Constants.CensusEndpoint}/s:{key}/get/{ns}/{service}/?c:join=joinedservice^on:testfield(subjoined^list:true)");

        var query = GetCensusQueryFactory().Create(service);

        var joinedService = query.JoinService("joinedservice");
        joinedService.OnField("testfield");

        var subJoinedService = joinedService.JoinService("subjoined");
        subJoinedService.IsList(true);

        var censusUri = query.GetUri();

        Assert.Equal(expectedUri, censusUri);
    }

    [Fact]
    public void CensusAddTree()
    {
        var service = "character";
        var ns = "ps2";
        var key = "testkey";

        var expectedUri = new Uri($"http://{Constants.CensusEndpoint}/s:{key}/get/{ns}/{service}/?c:tree=treefield^prefix:someprefix^start:treestart");

        var query = GetCensusQueryFactory().Create(service);

        var treeField = query.TreeField("treefield");
        treeField.StartField("treestart");
        treeField.GroupPrefix("someprefix");

        var censusUri = query.GetUri();

        Assert.Equal(expectedUri, censusUri);
    }

    [Fact]
    public void CensusAddTreeWithSubTree()
    {
        var service = "character";
        var ns = "ps2";
        var key = "testkey";

        var expectedUri = new Uri($"http://{Constants.CensusEndpoint}/s:{key}/get/{ns}/{service}/?c:tree=treefield^start:treestart(subtreefield^start:subtreestart)");

        var query = GetCensusQueryFactory().Create(service);

        var treeField = query.TreeField("treefield");
        treeField.StartField("treestart");

        var subTreeField = treeField.TreeField("subtreefield");
        subTreeField.StartField("subtreestart");

        var censusUri = query.GetUri();

        Assert.Equal(expectedUri, censusUri);
    }

    [Fact]
    public void CensusShowFields()
    {
        var service = "character";
        var ns = "ps2";
        var key = "testkey";

        var expectedUri = new Uri($"http://{Constants.CensusEndpoint}/s:{key}/get/{ns}/{service}/?c:show=field1,field2,field3");

        var query = GetCensusQueryFactory().Create(service);

        query.ShowFields(new[] { "field1", "field2", "field3" });

        var censusUri = query.GetUri();

        Assert.Equal(expectedUri, censusUri);
    }

    [Fact]
    public void CensusHideFields()
    {
        var service = "character";
        var ns = "ps2";
        var key = "testkey";

        var expectedUri = new Uri($"http://{Constants.CensusEndpoint}/s:{key}/get/{ns}/{service}/?c:hide=field1,field2,field3");

        var query = GetCensusQueryFactory().Create(service);

        query.HideFields(new[] { "field1", "field2", "field3" });

        var censusUri = query.GetUri();

        Assert.Equal(expectedUri, censusUri);
    }

    [Fact]
    public void CensusTestHttps()
    {
        var service = "character";
        var ns = "ps2";
        var key = "testkey";

        var expectedUri = new Uri($"https://{Constants.CensusEndpoint}/s:{key}/get/{ns}/{service}/?c:hide=field1,field2,field3");

        var query = GetCensusQueryFactory(useHttps: true).Create(service);

        query.HideFields(new[] { "field1", "field2", "field3" });

        var censusUri = query.GetUri();

        Assert.Equal(expectedUri, censusUri);
    }

    [Fact]
    public void CensusUseExactMatchFirst()
    {
        var service = "character";
        var ns = "ps2";
        var key = "testkey";

        var expectedUri = new Uri($"http://{Constants.CensusEndpoint}/s:{key}/get/{ns}/{service}/?c:exactMatchFirst=true");

        var query = GetCensusQueryFactory().Create(service);

        query.UseExactMatchFirst();

        var censusUri = query.GetUri();

        Assert.Equal(expectedUri, censusUri);
    }

    [Fact]
    public void CensusLimitPerDB()
    {
        var service = "character";
        var ns = "ps2";
        var key = "testkey";

        var expectedUri = new Uri($"http://{Constants.CensusEndpoint}/s:{key}/get/{ns}/{service}/?c:limitPerDB=20");

        var query = GetCensusQueryFactory().Create(service);

        query.SetLimitPerDB(20);

        var censusUri = query.GetUri();

        Assert.Equal(expectedUri, censusUri);
    }

    private static CensusQueryFactory GetCensusQueryFactory(bool useHttps = false)
    {
        var options = new CensusOptions
        {
            CensusServiceId = "testkey",
            CensusServiceNamespace = "ps2",
            UserAgent = "test",
            UseHttps = useHttps
        };

        var censusClient = new CensusClient(Options.Create(options), null!);

        return new CensusQueryFactory(censusClient);
    }
}
