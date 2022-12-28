namespace System.Reflection;

public static class AssemblyInfoExtensions
{
    public static string BuildLogoString(this Assembly assembly) =>
        $"{GetDescription(assembly)} v{GetInformationalVersion(assembly)} ({GetCopyright(assembly)})";

    public static string GetInformationalVersion(this Assembly assembly) =>
        assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

    public static string GetDescription(this Assembly assembly) =>
        assembly.GetCustomAttribute<AssemblyDescriptionAttribute>()?.Description;

    public static string GetCopyright(this Assembly assembly) =>
        assembly.GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright;
}