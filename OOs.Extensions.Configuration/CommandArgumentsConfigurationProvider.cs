using Microsoft.Extensions.Configuration;
using OOs.CommandLine;

namespace OOs.Extensions.Configuration;

public sealed class CommandArgumentsConfigurationProvider<TParser> : ConfigurationProvider where TParser : IArgumentsParser
{
    private readonly string[] args;
    private readonly string argsSectionKey;

    public CommandArgumentsConfigurationProvider(string[] args, string argsSectionKey) : base()
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentException.ThrowIfNullOrEmpty(argsSectionKey);

        this.args = args;
        this.argsSectionKey = argsSectionKey;
    }

    public override void Load()
    {
        var (options, arguments) = TParser.Parse(args);

        foreach (var option in options)
        {
            Data.Add(option);
        }

        for (var i = 0; i < arguments.Length; i++)
        {
            Data.Add($"{argsSectionKey}:{i}", arguments[i]);
        }
    }
}