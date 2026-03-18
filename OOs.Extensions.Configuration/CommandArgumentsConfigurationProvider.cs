using Microsoft.Extensions.Configuration;

namespace OOs.Extensions.Configuration;

public sealed class CommandArgumentsConfigurationProvider : ConfigurationProvider
{
    public CommandArgumentsConfigurationProvider(IReadOnlyDictionary<string, string> options,
        IReadOnlyList<string> arguments, string argsSectionKey = "args")
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(arguments);
        ArgumentNullException.ThrowIfNull(argsSectionKey);

        foreach (var option in options)
        {
#pragma warning disable CS8620 // Argument cannot be used for parameter due to differences in the nullability of reference types.
            Data.Add(option);
#pragma warning restore CS8620 // Argument cannot be used for parameter due to differences in the nullability of reference types.

        }

        for (var i = 0; i < arguments.Count; i++)
        {
            Data.Add($"{argsSectionKey}:{i}", arguments[i]);
        }
    }
}