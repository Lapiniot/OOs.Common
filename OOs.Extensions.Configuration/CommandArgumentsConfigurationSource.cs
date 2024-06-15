using Microsoft.Extensions.Configuration;
using OOs.CommandLine;

namespace OOs.Extensions.Configuration;

internal sealed class CommandArgumentsConfigurationSource<TParser>(string[] args, string argsSectionKey) : IConfigurationSource
    where TParser : IArgumentsParser
{
    public IConfigurationProvider Build(IConfigurationBuilder builder) =>
        new CommandArgumentsConfigurationProvider<TParser>(args, argsSectionKey);
}