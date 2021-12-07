using Microsoft.Extensions.Configuration;

namespace System.Configuration;

public class CommandArgumentsConfigurationSource : IConfigurationSource
{
    private readonly string[] args;
    private readonly bool strict;

    public CommandArgumentsConfigurationSource(string[] args, bool strict)
    {
        this.args = args;
        this.strict = strict;
    }

    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        return new CommandArgumentsConfigurationProvider(args, "args", strict);
    }
}