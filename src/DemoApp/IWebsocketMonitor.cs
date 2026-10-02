namespace DemoApp;

public interface IWebsocketMonitor
{
    Task OnApplicationStartup(CancellationToken cancellationToken);
    Task OnApplicationShutdown(CancellationToken cancellationToken);
}
