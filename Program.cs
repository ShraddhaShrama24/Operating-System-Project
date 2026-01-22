using System;
using System.Threading;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        int cacheSize = 4;
        int port = 5000;

        var server = new WebServer(port);
        server.Start();

        var cache = new MiniCache(cacheSize, server);

        Console.WriteLine("Dashboard → http://localhost:" + port);

        var watcher = new EtwWatcher("", (procName) =>
        {
            cache.OnPageAccess(procName);
        });

        var cts = new CancellationTokenSource();
        var task = watcher.RunAsync(cts.Token);

        Console.WriteLine("Press ENTER to stop...");
        Console.ReadLine();

        cts.Cancel();
        await task;
    }
}
