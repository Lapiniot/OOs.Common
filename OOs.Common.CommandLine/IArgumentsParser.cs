using System.Collections.Immutable;

#nullable enable

namespace OOs.CommandLine;

public interface IArgumentsParser
{
    static abstract (IReadOnlyDictionary<string, string?> Options, ImmutableArray<string> Arguments) Parse(string[] args);
}