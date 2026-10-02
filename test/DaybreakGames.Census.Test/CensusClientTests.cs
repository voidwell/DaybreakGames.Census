using DaybreakGames.Census.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace DaybreakGames.Census.Test;

public class CensusClientTests
{
    public sealed class CharacterModel
    {
        public string? CharacterId { get; set; }
        public int Level { get; set; }
        public bool IsActive { get; set; }
        public DateTime Created { get; set; }
    }

    private sealed class ListLogger : ILogger<CensusClient>
    {
        public List<(LogLevel Level, int EventId)> Entries { get; } = new();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Entries.Add((logLevel, eventId.Id));
        }
    }

    [Fact]
    public async Task CensusExecuteQueryListDeserializesItemsAsync()
    {
        using var server = new TestServer(c => TestServer.WriteJsonAsync(c,
            "{\"character_list\":[{\"character_id\":\"1\",\"level\":\"5\",\"is_active\":\"1\",\"created\":\"1577934245\"},{\"character_id\":\"2\",\"level\":7,\"is_active\":\"0\",\"created\":\"0\"}],\"returned\":2}"));
        using var client = Client(server);

        var result = (await client.CreateQuery("character").GetListAsync<CharacterModel>()).ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal("1", result[0].CharacterId);
        Assert.Equal(5, result[0].Level);
        Assert.True(result[0].IsActive);
        Assert.Equal(new DateTime(2020, 1, 2, 3, 4, 5, DateTimeKind.Utc), result[0].Created);
        Assert.Equal(7, result[1].Level);
        Assert.False(result[1].IsActive);
    }

    [Fact]
    public async Task CensusGetAsyncReturnsFirstItemAsync()
    {
        using var server = new TestServer(c => TestServer.WriteJsonAsync(c,
            "{\"character_list\":[{\"character_id\":\"1\"},{\"character_id\":\"2\"}]}"));
        using var client = Client(server);

        var result = await client.CreateQuery("character").GetAsync<CharacterModel>();

        Assert.Equal("1", result!.CharacterId);
    }

    [Fact]
    public async Task CensusGetListAsyncReturnsJsonElementsAsync()
    {
        using var server = new TestServer(c => TestServer.WriteJsonAsync(c,
            "{\"character_list\":[{\"character_id\":\"1\"}]}"));
        using var client = Client(server);

        var result = (await client.CreateQuery("character").GetListAsync()).ToList();

        Assert.Single(result);
        Assert.Equal("1", result[0].GetProperty("character_id").GetString());
    }

    [Fact]
    public async Task CensusRequestUsesQueryUriAsync()
    {
        using var server = new TestServer(c => TestServer.WriteJsonAsync(c, "{\"character_list\":[]}"));
        using var client = Client(server);

        var query = client.CreateQuery("character").SetLimit(3);
        query.Where("name.first_lower").Equals("abc");

        await query.GetListAsync();

        Assert.Single(server.Requests);
        Assert.Equal("/s:testkey/get/ps2/character/", server.Requests[0].AbsolutePath);
        Assert.Contains("name.first_lower=abc", server.Requests[0].Query);
        Assert.Contains("c:limit=3", server.Requests[0].Query);
    }

    [Fact]
    public async Task CensusNonSuccessStatusThrowsConnectionExceptionAsync()
    {
        using var server = new TestServer(c => TestServer.WriteJsonAsync(c, "{}", 500));
        using var client = Client(server);

        var ex = await Assert.ThrowsAsync<CensusConnectionException>(() => client.CreateQuery("character").GetListAsync());

        Assert.Contains("InternalServerError", ex.Message);
    }

    [Fact]
    public async Task CensusUnreachableServerThrowsConnectionExceptionAsync()
    {
        int port;
        using (var server = new TestServer(_ => Task.CompletedTask))
        {
            port = server.Port;
        }

        using var client = new CensusClient(Options.Create(new CensusOptions
        {
            CensusApiEndpoint = $"localhost:{port}",
            CensusServiceId = "testkey"
        }), NullLogger<CensusClient>.Instance);

        await Assert.ThrowsAsync<CensusConnectionException>(() => client.CreateQuery("character").GetListAsync());
    }

    [Fact]
    public async Task CensusInvalidJsonThrowsCensusExceptionAsync()
    {
        using var server = new TestServer(async c =>
        {
            c.Response.ContentType = "text/html";
            await using var writer = new StreamWriter(c.Response.OutputStream);
            await writer.WriteAsync("<html>maintenance</html>");
        });
        using var client = Client(server);

        var ex = await Assert.ThrowsAsync<CensusException>(() => client.CreateQuery("character").GetListAsync());

        Assert.Contains("Failed to read JSON", ex.Message);
        Assert.NotNull(ex.InnerException);
    }

    [Fact]
    public async Task CensusServiceUnavailableThrowsAsync()
    {
        using var server = new TestServer(c => TestServer.WriteJsonAsync(c, "{\"error\":\"service_unavailable\"}"));
        using var client = Client(server);

        await Assert.ThrowsAsync<CensusServiceUnavailableException>(() => client.CreateQuery("character").GetListAsync());
    }

    [Fact]
    public async Task CensusErrorFieldThrowsServerExceptionAsync()
    {
        using var server = new TestServer(c => TestServer.WriteJsonAsync(c, "{\"error\":\"something broke\"}"));
        using var client = Client(server);

        var ex = await Assert.ThrowsAsync<CensusServerException>(() => client.CreateQuery("character").GetListAsync());

        Assert.Equal("something broke", ex.Message);
    }

    [Fact]
    public async Task CensusErrorCodeThrowsServerExceptionAsync()
    {
        using var server = new TestServer(c => TestServer.WriteJsonAsync(c, "{\"errorCode\":\"SERVER_ERROR\",\"errorMessage\":\"bad things\"}"));
        using var client = Client(server);

        var ex = await Assert.ThrowsAsync<CensusServerException>(() => client.CreateQuery("character").GetListAsync());

        Assert.Equal("SERVER_ERROR: bad things", ex.Message);
    }

    [Fact]
    public async Task CensusBatchPagesUntilShortPageAsync()
    {
        var limit = Constants.DefaultBatchLimit;
        using var server = new TestServer(c =>
        {
            var start = int.Parse(c.Request.QueryString["c:start"]!);
            var count = start == 0 ? limit : 3;
            var items = string.Join(",", Enumerable.Range(start, count).Select(i => $"{{\"character_id\":\"{i}\"}}"));

            return TestServer.WriteJsonAsync(c, $"{{\"character_list\":[{items}]}}");
        });
        using var client = Client(server);

        var result = (await client.CreateQuery("character").GetBatchAsync<CharacterModel>()).ToList();

        Assert.Equal(limit + 3, result.Count);
        Assert.Equal("0", result[0].CharacterId);
        Assert.Equal((limit + 2).ToString(), result[^1].CharacterId);
        Assert.Equal(2, server.Requests.Count);
        Assert.Contains($"c:limit={limit}", server.Requests[0].Query);
        Assert.Contains("c:start=0", server.Requests[0].Query);
    }

    [Fact]
    public async Task CensusBatchSinglePageStopsImmediatelyAsync()
    {
        using var server = new TestServer(c => TestServer.WriteJsonAsync(c,
            "{\"character_list\":[{\"character_id\":\"1\"},{\"character_id\":\"2\"}]}"));
        using var client = Client(server);

        var result = (await client.CreateQuery("character").GetBatchAsync<CharacterModel>()).ToList();

        Assert.Equal(2, result.Count);
        Assert.Single(server.Requests);
    }

    [Fact]
    public async Task CensusLogsErrorsWhenEnabledAsync()
    {
        var logger = new ListLogger();
        using var server = new TestServer(c => TestServer.WriteJsonAsync(c, "{\"error\":\"service_unavailable\"}"));
        using var client = Client(server, logger, logErrors: true);

        await Assert.ThrowsAsync<CensusServiceUnavailableException>(() => client.CreateQuery("character").GetListAsync());

        Assert.Contains((LogLevel.Error, 84531), logger.Entries);
    }

    [Fact]
    public async Task CensusDoesNotLogErrorsWhenDisabledAsync()
    {
        var logger = new ListLogger();
        using var server = new TestServer(c => TestServer.WriteJsonAsync(c, "{\"error\":\"service_unavailable\"}"));
        using var client = Client(server, logger, logErrors: false);

        await Assert.ThrowsAsync<CensusServiceUnavailableException>(() => client.CreateQuery("character").GetListAsync());

        Assert.DoesNotContain(logger.Entries, e => e.Level == LogLevel.Error);
    }

    [Fact]
    public async Task CensusSendsUserAgentAsync()
    {
        string? userAgent = null;
        using var server = new TestServer(c =>
        {
            userAgent = c.Request.UserAgent;
            return TestServer.WriteJsonAsync(c, "{\"character_list\":[]}");
        });
        using var client = new CensusClient(Options.Create(new CensusOptions
        {
            CensusApiEndpoint = server.Endpoint,
            CensusServiceId = "testkey",
            UserAgent = "census-test/1.0"
        }), NullLogger<CensusClient>.Instance);

        await client.CreateQuery("character").GetListAsync();

        Assert.Equal("census-test/1.0", userAgent);
    }

    private static CensusClient Client(TestServer server, ILogger<CensusClient>? logger = null, bool logErrors = false)
    {
        return new CensusClient(Options.Create(new CensusOptions
        {
            CensusApiEndpoint = server.Endpoint,
            CensusServiceId = "testkey",
            LogCensusErrors = logErrors
        }), logger ?? NullLogger<CensusClient>.Instance);
    }
}
