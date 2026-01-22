using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

public class EtwWatcher
{
    private readonly string filter;
    private readonly Action<string> onEvent;
    private readonly int flushIntervalMs;

    private readonly ConcurrentQueue<string> events = new();

    public EtwWatcher(string filter, Action<string> onEvent, int flushIntervalMs = 200)
    {
        this.filter = filter?.ToLower() ?? "";
        this.onEvent = onEvent;
        this.flushIntervalMs = flushIntervalMs;
    }

    public async Task RunAsync(CancellationToken token)
    {
        var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(flushIntervalMs));

        while (!token.IsCancellationRequested)
        {
            foreach (var proc in Process.GetProcesses())
            {
                try
                {
                    string name = proc.ProcessName.ToLower();

                    if (!string.IsNullOrEmpty(filter) && !name.Contains(filter))
                        continue;

                    events.Enqueue(proc.ProcessName);
                }
                catch { }
            }

            await timer.WaitForNextTickAsync(token);

            while (events.TryDequeue(out var app))
                onEvent(app);
        }
    }
}
