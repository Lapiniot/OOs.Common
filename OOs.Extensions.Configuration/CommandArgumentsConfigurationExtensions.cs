using Microsoft.Extensions.Configuration;
using OOs.CommandLine;

namespace OOs.Extensions.Configuration;

public static class CommandArgumentsConfigurationExtensions
{
    public static IConfigurationBuilder AddCommandArguments(this IConfigurationBuilder builder,
        string[] args, string argsSectionKey = "args") =>
        AddCommandArguments<Arguments>(builder, args, argsSectionKey);

    public static IConfigurationBuilder AddCommandArguments<TParser>(this IConfigurationBuilder builder,
        string[] args, string argsSectionKey = "args") where TParser : IArgumentsParser
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(args);

        return builder.Add(new CommandArgumentsConfigurationSource<TParser>(args, argsSectionKey));
    }
}