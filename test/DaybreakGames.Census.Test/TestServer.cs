using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace DaybreakGames.Census.Test;

internal sealed class TestServer : IDisposable
{
    private readonly HttpListener _listener = new();
    private readonly ConcurrentQueue<Uri> _requests = new();

    public TestServer(Func<HttpListenerContext, Task> handler)
    {
        var probe = new TcpListener(IPAddress.Loopback, 0);
        probe.Start();
        Port = ((IPEndPoint)probe.LocalEndpoint).Port;
        probe.Stop();

        _listener.Prefixes.Add($"http://localhost:{Port}/");
        _listener.Start();

        _ = Task.Run(async () =>
        {
            while (_listener.IsListening)
            {
                HttpListenerContext context;

                try
                {
                    context = await _listener.GetContextAsync();
                }
                catch (Exception ex) when (ex is HttpListenerException or ObjectDisposedException or InvalidOperationException)
                {
                    break;
                }

                _requests.Enqueue(context.Request.Url!);
                _ = Task.Run(() => handler(context));
            }
        });
    }

    public int Port { get; }

    public string Endpoint => $"localhost:{Port}";

    public IReadOnlyList<Uri> Requests => _requests.ToArray();

    public static async Task WriteJsonAsync(HttpListenerContext context, string json, int statusCode = 200)
    {
        var bytes = Encoding.UTF8.GetBytes(json);

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        context.Response.ContentLength64 = bytes.Length;
        await context.Response.OutputStream.WriteAsync(bytes);
        context.Response.Close();
    }

    public void Dispose()
    {
        _listener.Close();
    }
}
