using System.Collections.Immutable;

namespace OOs.CommandLine;

public interface IArgumentsParser
{
    static abstract (IReadOnlyDictionary<string, string> Options, ImmutableArray<string> Arguments) Parse(ReadOnlySpan<string> args);
}