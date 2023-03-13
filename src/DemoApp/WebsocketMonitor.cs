using DaybreakGames.Census.Stream;
using Microsoft.Extensions.Logging;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Websocket.Client;

namespace DemoApp
{
    public class WebsocketMonitor : IWebsocketMonitor, IDisposable
    {
        private readonly ICensusStreamClient _client;
        private readonly ILogger<WebsocketMonitor> _logger;

        private readonly CensusStreamSubscription _subscription = new CensusStreamSubscription
        {
            Characters = new[] { "all" },
            Worlds = new[] { "17" },
            EventNames = new[] { "PlayerLogin", "PlayerLogout" }
        };

        public WebsocketMonitor(ICensusStreamClient censusStreamClient, ILogger<WebsocketMonitor> logger)
        {
            _client = censusStreamClient;
            _logger = logger;

            _client.OnConnect(OnConnect)
                .OnMessage(OnMessage)
                .OnDisconnect(OnDisconnect);
        }

        public Task OnApplicationShutdown(CancellationToken cancellationToken)
        {
            return _client.DisconnectAsync();
        }

        public Task OnApplicationStartup(CancellationToken cancellationToken)
        {
            return _client.ConnectAsync();
        }

        private Task OnConnect(ReconnectionType type)
        {
            if (type == ReconnectionType.Initial)
            {
                _logger.LogInformation("Websocket Client Connected!!");
            }
            else
            {
                _logger.LogInformation("Websocket Client Reconnected!!");
            }

            _client.Subscribe(_subscription);

            return Task.CompletedTask;
        }

        private async Task OnMessage(string message)
        {
            if (message == null)
            {
                return;
            }

            try
            {
                var msg = JsonDocument.Parse(message);
                _logger.LogInformation($"Received Message: {msg.RootElement.ToString()}");
            }
            catch(Exception)
            {
                _logger.LogError(91097, "Failed to parse message: {0}", message);
            }
        }

        private Task OnDisconnect(DisconnectionInfo info)
        {
            _logger.LogInformation($"Websocket Client Disconnected: {info.Type}");

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _client?.Dispose();
        }
    }
}
