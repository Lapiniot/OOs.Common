using Microsoft.Extensions.Configuration;
using OOs.CommandLine;

namespace OOs.Extensions.Configuration;

internal sealed class CommandArgumentsConfigurationSource<TParser>(string[] args, string argsSectionKey) :
    IConfigurationSource
    where TParser : IArgumentsParser
{
    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        var (options, arguments) = TParser.Parse(args);
        return new CommandArgumentsConfigurationProvider(options, arguments, argsSectionKey);
    }
}

internal sealed class CommandArgumentsConfigurationSource(
    IReadOnlyDictionary<string, string> options,
    IReadOnlyList<string> arguments,
    string argsSectionKey) : IConfigurationSource
{
    public IConfigurationProvider Build(IConfigurationBuilder builder) =>
        new CommandArgumentsConfigurationProvider(options, arguments, argsSectionKey);
}