using Microsoft.Extensions.Configuration;

namespace System.Configuration;

public class CommandArgumentsConfigurationSource(string[] args, bool strict) : IConfigurationSource
{
    public IConfigurationProvider Build(IConfigurationBuilder builder) =>
        new CommandArgumentsConfigurationProvider(args, "args", strict);
}