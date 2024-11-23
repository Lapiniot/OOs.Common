namespace OOs;

public static class RuntimeOptions
{
    private const string ThreadingInstrumentationSupportedSwitchName = "OOs.Threading.Instrumentation.IsSupported";
#if NET9_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.FeatureSwitchDefinition(ThreadingInstrumentationSupportedSwitchName)]
#endif
    public static bool ThreadingInstrumentationSupported { get; } =
        !AppContext.TryGetSwitch(ThreadingInstrumentationSupportedSwitchName, out var isEnabled) || isEnabled;
}