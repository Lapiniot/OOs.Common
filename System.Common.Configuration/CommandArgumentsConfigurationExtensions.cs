using Microsoft.Extensions.Configuration;

namespace System.Configuration
{
    public static class CommandArgumentsConfigurationExtensions
    {
        public static IConfigurationBuilder AddCommandArguments(this IConfigurationBuilder builder, string[] args)
        {
            return builder.Add(new CommandArgumentsConfigurationSource(args));
        }
    }
}