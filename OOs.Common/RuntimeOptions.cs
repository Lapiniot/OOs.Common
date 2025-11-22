using System.Diagnostics.CodeAnalysis;

namespace OOs;

public static class RuntimeOptions
{
    private const string ThreadingInstrumentationSupportedSwitchName = "OOs.Threading.Instrumentation.IsSupported";

    [FeatureSwitchDefinition(ThreadingInstrumentationSupportedSwitchName)]
    public static bool ThreadingInstrumentationSupported { get; } =
        !AppContext.TryGetSwitch(ThreadingInstrumentationSupportedSwitchName, out var isEnabled) || isEnabled;
}