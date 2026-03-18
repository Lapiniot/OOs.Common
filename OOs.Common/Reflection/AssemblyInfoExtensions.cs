using System.Reflection;

#pragma warning disable CA1034 // Nested types should not be visible

namespace OOs.Reflection;

public static class AssemblyInfoExtensions
{
    extension(Assembly assembly)
    {
        public string BuildLogoString() =>
            $"{assembly.GetDescription()} v{assembly.GetInformationalVersion()} ({assembly.GetCopyright()})";

        public string? GetInformationalVersion() =>
            assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        public string? GetDescription() =>
            assembly.GetCustomAttribute<AssemblyDescriptionAttribute>()?.Description;

        public string? GetCopyright() =>
            assembly.GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright;
    }
}