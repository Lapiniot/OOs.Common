using Microsoft.Extensions.Configuration;

namespace OOs.Extensions.Configuration;

public class CommandArgumentsConfigurationSource(string[] args, bool strict) : IConfigurationSource
{
    public IConfigurationProvider Build(IConfigurationBuilder builder) =>
        new CommandArgumentsConfigurationProvider(args, "args", strict);
}