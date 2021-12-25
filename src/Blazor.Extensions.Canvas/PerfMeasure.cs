using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Blazor.Extensions.Canvas;

public record PerfInfo
{
    public long Calls { get; set; }
    public double TotalMs { get; set; }
    public double MsPerCall => TotalMs / Calls;
}

public sealed class PerfContext
{
    public Dictionary<string, PerfInfo> Map { get; } = new Dictionary<string, PerfInfo>();

    public void AddInfo(string method, double time)
    {
        if (!Map.TryGetValue(method, out PerfInfo info))
        {
            info = new PerfInfo();
            Map[method] = info;
        }

        info.TotalMs += time;
        info.Calls++;
    }
}

public sealed class PerfMeasure : IDisposable
{
    readonly Stopwatch stopwatch = new ();
    private readonly string method;
    private readonly PerfContext context;

    public PerfMeasure(PerfContext perfContext, string methodName)
    {
        context = perfContext;
        method = methodName;
        stopwatch.Start();
    }

    public void Dispose()
    {
        context.AddInfo(method, stopwatch.Elapsed.TotalMilliseconds);
        stopwatch.Stop();
    }
}