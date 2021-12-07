namespace System.Diagnostics;

public readonly record struct MeasureTimeScope : IDisposable
{
    private readonly string format;
    private readonly Stopwatch stopwatch;

    public MeasureTimeScope(string format)
    {
        ArgumentNullException.ThrowIfNull(format);
        this.format = format;
        stopwatch = Stopwatch.StartNew();
    }

    public MeasureTimeScope()
    {
        format = "Elapsed time: {0} ({1} milliseconds)";
        stopwatch = Stopwatch.StartNew();
    }

    public readonly void Dispose()
    {
        stopwatch.Stop();
        Console.WriteLine(format, stopwatch.Elapsed, stopwatch.ElapsedMilliseconds);
    }
}