using System.Diagnostics.Tracing;
using System.Globalization;

namespace System.Diagnostics;

[CLSCompliant(false)]
public class RuntimeCountersListener(int updateIntervalSec = 1) : EventListener
{
    private EventSource source;

    public double CpuUsage { get; private set; }
    public double WorkingSet { get; private set; }
    public double GcHeapSize { get; private set; }
    public int Gen0GcCount { get; private set; }
    public int Gen1GcCount { get; private set; }
    public int Gen2GcCount { get; private set; }
    public ulong Gen0Size { get; private set; }
    public ulong Gen1Size { get; private set; }
    public ulong Gen2Size { get; private set; }
    public int ThreadPoolThreadCount { get; private set; }
    public int ThreadPoolQueueLength { get; private set; }
    public int ThreadPoolCompletedItemsCount { get; private set; }

    protected override void OnEventSourceCreated(EventSource eventSource)
    {
        if (string.Equals(eventSource?.Name, "System.Runtime", StringComparison.Ordinal))
        {
            source = eventSource;
        }
    }

    public void Start()
    {
        ArgumentNullException.ThrowIfNull(source);

        const EventKeywords AllButAppContext = (EventKeywords)(-1 & ~1);
        EnableEvents(source, EventLevel.LogAlways, AllButAppContext, new Dictionary<string, string>
            {
                { "EventCounterIntervalSec", updateIntervalSec.ToString(CultureInfo.InvariantCulture) }
            });
    }

    protected override void OnEventWritten(EventWrittenEventArgs eventData)
    {
        if (eventData is { EventName: "EventCounters", Payload: [IDictionary<string, object> data, ..] } && data.TryGetValue("Name", out var name))
        {
            switch (name)
            {
                case "cpu-usage": CpuUsage = (double)data["Mean"]; break;
                case "working-set": WorkingSet = (double)data["Mean"]; break;
                case "gc-heap-size": GcHeapSize = (double)data["Mean"]; break;
                case "gen-0-gc-count": Gen0GcCount += (int)(double)data["Increment"]; break;
                case "gen-1-gc-count": Gen1GcCount += (int)(double)data["Increment"]; break;
                case "gen-2-gc-count": Gen2GcCount += (int)(double)data["Increment"]; break;
                case "gen-0-size": Gen0Size = (ulong)(double)data["Mean"]; break;
                case "gen-1-size": Gen1Size = (ulong)(double)data["Mean"]; break;
                case "gen-2-size": Gen2Size = (ulong)(double)data["Mean"]; break;
                case "threadpool-thread-count": ThreadPoolThreadCount = (int)(double)data["Mean"]; break;
                case "threadpool-queue-length": ThreadPoolQueueLength = (int)(double)data["Mean"]; break;
                case "threadpool-completed-items-count": ThreadPoolCompletedItemsCount += (int)(double)data["Increment"]; break;
            }
        }
    }
}