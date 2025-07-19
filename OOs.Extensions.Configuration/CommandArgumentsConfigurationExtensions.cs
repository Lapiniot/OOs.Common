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
        ArgumentException.ThrowIfNullOrEmpty(argsSectionKey);

        return builder.Add(new CommandArgumentsConfigurationSource<TParser>(args, argsSectionKey));
    }

    public static IConfigurationBuilder AddCommandArguments(this IConfigurationBuilder builder,
        IReadOnlyDictionary<string, string> options, IReadOnlyList<string> arguments,
        string argsSectionKey = "args")
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(arguments);
        ArgumentException.ThrowIfNullOrEmpty(argsSectionKey);

        return builder.Add(new CommandArgumentsConfigurationSource(options, arguments, argsSectionKey));
    }
}