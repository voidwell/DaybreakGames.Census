using System.Collections.Concurrent;
using System.Net;
using DaybreakGames.Census.Exceptions;
using Microsoft.Extensions.DependencyInjection;
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
        private readonly ConcurrentQueue<(LogLevel Level, int EventId, IReadOnlyDictionary<string, object?> Properties)> _entries = new();

        public IReadOnlyList<(LogLevel Level, int EventId)> Entries => _entries.Select(e => (e.Level, e.EventId)).ToList();

        public IReadOnlyDictionary<string, object?> Properties(int eventId) => _entries.Single(e => e.EventId == eventId).Properties;

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var properties = state is IEnumerable<KeyValuePair<string, object?>> kvps
                ? kvps.ToDictionary(kv => kv.Key, kv => kv.Value)
                : new Dictionary<string, object?>();

            _entries.Enqueue((logLevel, eventId.Id, properties));
        }
    }

    [Fact]
    public async Task CensusExecuteQueryListDeserializesItemsAsync()
    {
        using var server = new TestServer(c => TestServer.WriteJsonAsync(c,
            "{\"character_list\":[{\"character_id\":\"1\",\"level\":\"5\",\"is_active\":\"1\",\"created\":\"1577934245\"},{\"character_id\":\"2\",\"level\":7,\"is_active\":\"0\",\"created\":\"0\"}],\"returned\":2}"));
        var client = Client(server);

        var result = (await client.CreateQuery("character").GetListAsync<CharacterModel>(TestContext.Current.CancellationToken)).ToList();

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
        var client = Client(server);

        var result = await client.CreateQuery("character").GetAsync<CharacterModel>(TestContext.Current.CancellationToken);

        Assert.Equal("1", result!.CharacterId);
    }

    [Fact]
    public async Task CensusGetListAsyncReturnsJsonElementsAsync()
    {
        using var server = new TestServer(c => TestServer.WriteJsonAsync(c,
            "{\"character_list\":[{\"character_id\":\"1\"}]}"));
        var client = Client(server);

        var result = (await client.CreateQuery("character").GetListAsync(TestContext.Current.CancellationToken)).ToList();

        Assert.Single(result);
        Assert.Equal("1", result[0].GetProperty("character_id").GetString());
    }

    [Fact]
    public async Task CensusRequestUsesQueryUriAsync()
    {
        using var server = new TestServer(c => TestServer.WriteJsonAsync(c, "{\"character_list\":[]}"));
        var client = Client(server);

        var query = client.CreateQuery("character").SetLimit(3);
        query.Where("name.first_lower").Equals("abc");

        await query.GetListAsync(TestContext.Current.CancellationToken);

        Assert.Single(server.Requests);
        Assert.Equal("/s:testkey/get/ps2/character/", server.Requests[0].AbsolutePath);
        Assert.Contains("name.first_lower=abc", server.Requests[0].Query);
        Assert.Contains("c:limit=3", server.Requests[0].Query);
    }

    [Fact]
    public async Task CensusNonSuccessStatusThrowsConnectionExceptionAsync()
    {
        using var server = new TestServer(c => TestServer.WriteJsonAsync(c, "{}", 500));
        var client = Client(server);

        var ex = await Assert.ThrowsAsync<CensusConnectionException>(() => client.CreateQuery("character").GetListAsync(TestContext.Current.CancellationToken));

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

        var client = new CensusClient(new HttpClient(), Options.Create(new CensusOptions
        {
            CensusApiEndpoint = $"localhost:{port}",
            UseHttps = false,
            CensusServiceId = "testkey"
        }), NullLogger<CensusClient>.Instance);

        await Assert.ThrowsAsync<CensusConnectionException>(() => client.CreateQuery("character").GetListAsync(TestContext.Current.CancellationToken));
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
        var client = Client(server);

        var ex = await Assert.ThrowsAsync<CensusException>(() => client.CreateQuery("character").GetListAsync(TestContext.Current.CancellationToken));

        Assert.Contains("Failed to read JSON", ex.Message);
        Assert.NotNull(ex.InnerException);
    }

    [Fact]
    public async Task CensusServiceUnavailableThrowsAsync()
    {
        using var server = new TestServer(c => TestServer.WriteJsonAsync(c, "{\"error\":\"service_unavailable\"}"));
        var client = Client(server);

        await Assert.ThrowsAsync<CensusServiceUnavailableException>(() => client.CreateQuery("character").GetListAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CensusErrorFieldThrowsServerExceptionAsync()
    {
        using var server = new TestServer(c => TestServer.WriteJsonAsync(c, "{\"error\":\"something broke\"}"));
        var client = Client(server);

        var ex = await Assert.ThrowsAsync<CensusServerException>(() => client.CreateQuery("character").GetListAsync(TestContext.Current.CancellationToken));

        Assert.Equal("something broke", ex.Message);
    }

    [Fact]
    public async Task CensusErrorCodeThrowsServerExceptionAsync()
    {
        using var server = new TestServer(c => TestServer.WriteJsonAsync(c, "{\"errorCode\":\"SERVER_ERROR\",\"errorMessage\":\"bad things\"}"));
        var client = Client(server);

        var ex = await Assert.ThrowsAsync<CensusServerException>(() => client.CreateQuery("character").GetListAsync(TestContext.Current.CancellationToken));

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
        var client = Client(server);

        var result = (await client.CreateQuery("character").GetBatchAsync<CharacterModel>(TestContext.Current.CancellationToken)).ToList();

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
        var client = Client(server);

        var result = (await client.CreateQuery("character").GetBatchAsync<CharacterModel>(TestContext.Current.CancellationToken)).ToList();

        Assert.Equal(2, result.Count);
        Assert.Single(server.Requests);
    }

    [Fact]
    public async Task CensusLogsErrorsAsync()
    {
        var logger = new ListLogger();
        using var server = new TestServer(c => TestServer.WriteJsonAsync(c, "{\"error\":\"service_unavailable\"}"));
        var client = Client(server, logger);

        await Assert.ThrowsAsync<CensusServiceUnavailableException>(() => client.CreateQuery("character").GetListAsync(TestContext.Current.CancellationToken));

        Assert.Contains((LogLevel.Error, 84531), logger.Entries);
        Assert.IsType<Uri>(logger.Properties(84531)["RequestUri"]);
    }

    [Fact]
    public async Task CensusTracesRequestWithTimingAsync()
    {
        var logger = new ListLogger();
        using var server = new TestServer(c => TestServer.WriteJsonAsync(c, "{\"character_list\":[]}"));
        var client = Client(server, logger);

        await client.CreateQuery("character").GetListAsync(TestContext.Current.CancellationToken);

        Assert.Contains((LogLevel.Trace, 85400), logger.Entries);
        Assert.Contains((LogLevel.Trace, 85401), logger.Entries);
        Assert.IsType<Uri>(logger.Properties(85400)["RequestUri"]);

        var completed = logger.Properties(85401);
        Assert.Equal(HttpStatusCode.OK, completed["StatusCode"]);
        Assert.IsType<double>(completed["ElapsedMilliseconds"]);
        Assert.IsType<Uri>(completed["RequestUri"]);
    }

    [Fact]
    public async Task CensusTracesAndRethrowsTimeoutAsync()
    {
        var logger = new ListLogger();
        using var server = new TestServer(async c =>
        {
            await Task.Delay(TimeSpan.FromSeconds(5));
            await TestServer.WriteJsonAsync(c, "{\"character_list\":[]}");
        });
        var client = Client(server, logger, new HttpClient { Timeout = TimeSpan.FromMilliseconds(200) });

        var ex = await Assert.ThrowsAsync<TaskCanceledException>(() => client.CreateQuery("character").GetListAsync(TestContext.Current.CancellationToken));

        Assert.IsType<TimeoutException>(ex.InnerException);
        Assert.Contains((LogLevel.Trace, 85402), logger.Entries);
        Assert.IsType<double>(logger.Properties(85402)["ElapsedMilliseconds"]);
        Assert.Contains((LogLevel.Error, 84534), logger.Entries);
        Assert.DoesNotContain((LogLevel.Trace, 85401), logger.Entries);
    }

    [Fact]
    public async Task CensusDoesNotLogErrorWhenCallerCancelsAsync()
    {
        var logger = new ListLogger();
        using var server = new TestServer(async c =>
        {
            await Task.Delay(TimeSpan.FromSeconds(5));
            await TestServer.WriteJsonAsync(c, "{\"character_list\":[]}");
        });
        var client = Client(server, logger);
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        cts.CancelAfter(TimeSpan.FromMilliseconds(200));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => client.CreateQuery("character").GetListAsync(cts.Token));

        Assert.DoesNotContain(logger.Entries, e => e.Level == LogLevel.Error);
        Assert.Contains((LogLevel.Trace, 85402), logger.Entries);
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
        var services = new ServiceCollection();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddCensusServices(o =>
        {
            o.CensusApiEndpoint = server.Endpoint;
            o.UseHttps = false;
            o.CensusServiceId = "testkey";
            o.UserAgent = "census-test/1.0";
        });
        using var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<ICensusClient>();

        await client.CreateQuery("character").GetListAsync(TestContext.Current.CancellationToken);

        Assert.Equal("census-test/1.0", userAgent);
    }

    private static CensusClient Client(TestServer server, ILogger<CensusClient>? logger = null, HttpClient? httpClient = null)
    {
        return new CensusClient(httpClient ?? new HttpClient(), Options.Create(new CensusOptions
        {
            CensusApiEndpoint = server.Endpoint,
            UseHttps = false,
            CensusServiceId = "testkey"
        }), logger ?? NullLogger<CensusClient>.Instance);
    }
}
