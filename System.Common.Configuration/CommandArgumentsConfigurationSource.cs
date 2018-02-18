using Microsoft.Extensions.Configuration;

namespace System.Configuration
{
    public class CommandArgumentsConfigurationSource : IConfigurationSource
    {
        private readonly string[] args;

        public CommandArgumentsConfigurationSource(string[] args)
        {
            this.args = args;
        }

        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            return new CommandArgumentsConfigurationProvider(args);
        }
    }
}