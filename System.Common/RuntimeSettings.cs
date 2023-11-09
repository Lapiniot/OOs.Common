namespace System;

public static class RuntimeSettings
{
    private static readonly bool threadingInstrumentationSupport = !AppContext.TryGetSwitch("System.Threading.InstrumentationSupport", out var isEnabled) || isEnabled;

    public static bool ThreadingInstrumentationSupport => threadingInstrumentationSupport;
}