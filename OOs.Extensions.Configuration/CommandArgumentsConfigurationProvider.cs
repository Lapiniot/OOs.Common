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
            Data.Add(option);
        }

        for (var i = 0; i < arguments.Count; i++)
        {
            Data.Add($"{argsSectionKey}:{i}", arguments[i]);
        }
    }
}