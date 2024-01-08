namespace OOs.Diagnostics;

public interface IProvideInstrumentationMetrics
{
    static abstract IDisposable EnableInstrumentation(string meterName = null);
}