namespace OOs;

public static class RuntimeSettings
{
    private static readonly bool threadingInstrumentationSupport = !AppContext.TryGetSwitch("OOs.Threading.InstrumentationSupport", out var isEnabled) || isEnabled;

    public static bool ThreadingInstrumentationSupport => threadingInstrumentationSupport;
}