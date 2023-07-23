namespace System.Diagnostics;

public interface IProvideInstrumentationMetrics
{
    static abstract IDisposable EnableInstrumentation(string meterName = null);
}