using System;
using System.Collections.Generic;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.IO;

public class WebServer
{
    private readonly int port;
    private readonly HttpListener listener = new();
    private readonly List<WebSocket> sockets = new();

    public WebServer(int port)
    {
        this.port = port;
        listener.Prefixes.Add($"http://localhost:{port}/");
listener.Prefixes.Add($"http://+:{port}/");

    }

    public void Start()
    {
        listener.Start();
        Console.WriteLine($"[WebServer] Dashboard running at http://localhost:{port}/");

        Task.Run(async () =>
        {
            while (true)
            {
                var ctx = await listener.GetContextAsync();

                if (ctx.Request.IsWebSocketRequest)
                {
                    var wsContext = await ctx.AcceptWebSocketAsync(null);
                    var socket = wsContext.WebSocket;
                    sockets.Add(socket);
                    Console.WriteLine("[WebSocket] Client connected.");

                    _ = Task.Run(() => ListenSocket(socket));
                }
                else
                {
                    ServeStatic(ctx);
                }
            }
        });
    }

    private void ServeStatic(HttpListenerContext ctx)
    {
        string path = ctx.Request.Url.AbsolutePath;

        if (path == "/") path = "/index.html";

        string fullPath = Path.Combine(AppContext.BaseDirectory, "web", path.TrimStart('/'));

        if (!File.Exists(fullPath))
        {
            ctx.Response.StatusCode = 404;
            ctx.Response.OutputStream.Close();
            return;
        }

        byte[] data = File.ReadAllBytes(fullPath);
        ctx.Response.ContentType = GetContentType(path);
        ctx.Response.OutputStream.Write(data, 0, data.Length);
        ctx.Response.OutputStream.Close();
    }

    private string GetContentType(string path)
    {
        if (path.EndsWith(".html")) return "text/html";
        if (path.EndsWith(".js")) return "application/javascript";
        if (path.EndsWith(".css")) return "text/css";
        return "text/plain";
    }

    private async Task ListenSocket(WebSocket socket)
    {
        var buffer = new byte[1024];

        try
        {
            while (socket.State == WebSocketState.Open)
            {
                var result = await socket.ReceiveAsync(
                    new ArraySegment<byte>(buffer), CancellationToken.None);
            }
        }
        catch { }

        sockets.Remove(socket);
        Console.WriteLine("[WebSocket] Client disconnected.");
    }

    public void Broadcast(object obj)
    {
        string json = JsonSerializer.Serialize(obj);
        var bytes = Encoding.UTF8.GetBytes(json);

        foreach (var socket in new List<WebSocket>(sockets))
        {
            if (socket.State == WebSocketState.Open)
                socket.SendAsync(bytes, WebSocketMessageType.Text, true, CancellationToken.None);
        }
    }
}
