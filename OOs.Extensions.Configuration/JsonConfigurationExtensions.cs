using Microsoft.Extensions.Configuration;
using OS = System.OperatingSystem;

namespace OOs.Extensions.Configuration;

public static class JsonConfigurationExtensions
{
    public static IConfigurationBuilder AddPlatformSpecificJsonFile(this IConfigurationBuilder builder, bool optional, bool reloadOnChange)
    {
        var platform = OS.IsWindows()
            ? "Windows"
            : OS.IsLinux()
                ? "Linux"
                : OS.IsFreeBSD()
                    ? "FreeBSD"
                    : OS.IsMacOS() || OS.IsMacCatalyst()
                        ? "MacOS"
                        : "UnknownOS";

        return builder.AddJsonFile($"appsettings.{platform}.json", optional, reloadOnChange);
    }
}