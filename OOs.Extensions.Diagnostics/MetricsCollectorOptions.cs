namespace OOs.Extensions.Diagnostics;

public class MetricsCollectorOptions
{
    public TimeSpan RecordInterval { get; set; } = TimeSpan.FromSeconds(5);
}