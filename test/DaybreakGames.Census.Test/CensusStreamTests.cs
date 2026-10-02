using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using DaybreakGames.Census.Stream;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace DaybreakGames.Census.Test;

public class CensusStreamTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(15);

    [Fact]
    public void CensusSubscriptionSerializesCamelCaseWithoutNulls()
    {
        var subscription = new CensusStreamSubscription
        {
            Characters = new[] { "1", "2" },
            EventNames = new[] { "Death" }
        };

        var json = JsonSerializer.Serialize(subscription, new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        Assert.Equal(
            "{\"service\":\"event\",\"action\":\"subscribe\",\"characters\":[\"1\",\"2\"],\"eventNames\":[\"Death\"],\"logicalAndCharactersWithWorlds\":false}",
            json);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void CensusStreamSettersRejectEmpty(string? value)
    {
        var client = NewClient();

        Assert.Throws<ArgumentNullException>(() => client.SetServiceId(value!));
        Assert.Throws<ArgumentNullException>(() => client.SetServiceNamespace(value!));
        Assert.Throws<ArgumentNullException>(() => client.SetEndpoint(value!));
    }

    [Fact]
    public async Task CensusStreamConnectsReceivesAndSubscribesAsync()
    {
        var subscribeReceived = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        var requestUrl = new TaskCompletionSource<Uri>(TaskCreationOptions.RunContinuationsAsynchronously);

        using var server = new TestServer(async context =>
        {
            requestUrl.TrySetResult(context.Request.Url!);

            var webSocketContext = await context.AcceptWebSocketAsync(null);
            var socket = webSocketContext.WebSocket;

            await socket.SendAsync(Encoding.UTF8.GetBytes("hello"), WebSocketMessageType.Text, true, CancellationToken.None);

            var buffer = new byte[4096];
            var received = await socket.ReceiveAsync(buffer, CancellationToken.None);
            subscribeReceived.TrySetResult(Encoding.UTF8.GetString(buffer, 0, received.Count));

            await Task.Delay(Timeout);
        });

        var message = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        var connected = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        var client = NewClient()
            .SetEndpoint($"ws://localhost:{server.Port}")
            .SetServiceId("testkey")
            .SetServiceNamespace("ps2:v2");

        client.OnMessage(text =>
        {
            message.TrySetResult(text);
            return Task.CompletedTask;
        });
        client.OnConnect(_ =>
        {
            connected.TrySetResult(true);
            return Task.CompletedTask;
        });

        try
        {
            await client.ConnectAsync();

            Assert.True(await connected.Task.WaitAsync(Timeout, TestContext.Current.CancellationToken));
            Assert.Equal("hello", await message.Task.WaitAsync(Timeout, TestContext.Current.CancellationToken));

            var url = await requestUrl.Task.WaitAsync(Timeout, TestContext.Current.CancellationToken);
            Assert.Contains("environment=ps2:v2", url.Query);
            Assert.Contains("service-id=s:testkey", url.Query);

            client.Subscribe(new CensusStreamSubscription
            {
                Characters = new[] { "1" },
                Worlds = new[] { "17" },
                EventNames = new[] { "Death" },
                LogicalAndCharactersWithWorlds = true
            });

            using var doc = JsonDocument.Parse(await subscribeReceived.Task.WaitAsync(Timeout, TestContext.Current.CancellationToken));
            var root = doc.RootElement;

            Assert.Equal("event", root.GetProperty("service").GetString());
            Assert.Equal("subscribe", root.GetProperty("action").GetString());
            Assert.Equal("1", root.GetProperty("characters")[0].GetString());
            Assert.Equal("17", root.GetProperty("worlds")[0].GetString());
            Assert.Equal("Death", root.GetProperty("eventNames")[0].GetString());
            Assert.True(root.GetProperty("logicalAndCharactersWithWorlds").GetBoolean());
        }
        finally
        {
            await client.DisconnectAsync();
        }
    }

    [Fact]
    public async Task CensusStreamDisconnectWithoutConnectIsSafeAsync()
    {
        var client = NewClient();

        await client.DisconnectAsync();
        client.Dispose();
    }

    private static CensusStreamClient NewClient()
    {
        return new CensusStreamClient(Options.Create(new CensusOptions()), NullLogger<CensusStreamClient>.Instance);
    }
}
