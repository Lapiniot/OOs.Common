using Microsoft.Extensions.Configuration;

namespace System.Configuration
{
    public static class CommandArgumentsConfigurationExtensions
    {
        public static IConfigurationBuilder AddCommandArguments(this IConfigurationBuilder builder, string[] args)
        {
            if(builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.Add(new CommandArgumentsConfigurationSource(args));
        }
    }
}