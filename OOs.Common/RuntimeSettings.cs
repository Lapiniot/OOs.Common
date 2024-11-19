namespace OOs;

public static class RuntimeSettings
{
#if NET9_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.FeatureSwitchDefinition("OOs.Threading.InstrumentationSupport")]
#endif
    public static bool ThreadingInstrumentationSupport { get; } =
        !AppContext.TryGetSwitch("OOs.Threading.InstrumentationSupport", out var isEnabled) || isEnabled;
}