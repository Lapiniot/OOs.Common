namespace OOs.Extensions.Diagnostics;

public sealed class MetricsCollectorOptions
{
    public TimeSpan RecordInterval { get; set; } = TimeSpan.FromSeconds(5);
}