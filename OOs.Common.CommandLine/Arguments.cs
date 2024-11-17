using System.Collections.Immutable;
using System.Reflection;

namespace OOs.CommandLine;

public readonly record struct Arguments : IArgumentsParser
{
    private Arguments(string command, IReadOnlyDictionary<string, object> options, ImmutableArray<string> values)
    {
        Command = command;
        Options = options;
        Values = values;
    }

    public string Command { get; }
    public IReadOnlyDictionary<string, object> Options { get; }
    public ImmutableArray<string> Values { get; }

    public static Arguments Parse(string[] args, bool strict)
    {
        var commands = Assembly.GetEntryAssembly()?
            .GetCustomAttributes<CommandAttribute>()
            .Distinct(EqualityComparer<CommandAttribute>.Create(
                equals: (a1, a2) => string.Equals(a1.Name, a2.Name, StringComparison.OrdinalIgnoreCase),
                getHashCode: a => a.Name.GetHashCode(StringComparison.OrdinalIgnoreCase)))
            .ToArray() ?? [];

        Parse(args, strict, out var options, out var values);
        var command = values is [{ } cmd, ..] ? commands.FirstOrDefault(c => c.Name == cmd) : null;
        return new(command?.Name, options, values);
    }

    public static (IReadOnlyDictionary<string, string> Options, ImmutableArray<string> Arguments) Parse(string[] args)
    {
        Parse(args, false, out var options, out var values);
        return (options.ToDictionary(kvp => kvp.Key, kvp => kvp.Value is { } value ? value.ToString() : null), values);
    }

    private static void Parse(string[] args, bool strict, out IReadOnlyDictionary<string, object> options, out ImmutableArray<string> arguments)
    {
        var queue = new Queue<string>(args);

        var schema = Assembly.GetEntryAssembly()?
            .GetCustomAttributes<OptionAttribute>()
            .Distinct(EqualityComparer<OptionAttribute>.Create(
                equals: (a1, a2) => string.Equals(a1.Name, a2.Name, StringComparison.OrdinalIgnoreCase),
                getHashCode: a => a.Name.GetHashCode(StringComparison.OrdinalIgnoreCase)))
            .ToArray() ?? [];

        var parser = new ArgumentParser(schema, strict);
        parser.Parse(queue, out options, out arguments);
    }
}