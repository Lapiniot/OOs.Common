using Microsoft.Extensions.Configuration;

namespace OOs.Configuration;

public static class CommandArgumentsConfigurationExtensions
{
    public static IConfigurationBuilder AddCommandArguments(this IConfigurationBuilder builder, string[] args, bool strict = true)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.Add(new CommandArgumentsConfigurationSource(args, strict));
    }
}