using System.Diagnostics.CodeAnalysis;
using static System.String;
using static System.Globalization.CultureInfo;
using System.Collections.Immutable;

namespace OOs.CommandLine;

public class ArgumentParser
{
    private static readonly char[] Quotes = ['"', '\''];
    private static readonly char[] Separator = ['='];

    private readonly IEnumerable<IArgumentMetadata> schema;
    private readonly bool strict;

    public ArgumentParser(IEnumerable<IArgumentMetadata> schema, bool strict)
    {
        ArgumentNullException.ThrowIfNull(schema);

        this.schema = schema;
        this.strict = strict;
    }

    public void Parse(Queue<string> tokens, out IReadOnlyDictionary<string, object> options, out ImmutableArray<string> arguments)
    {
        ArgumentNullException.ThrowIfNull(tokens);

        if (tokens.Count == 0)
        {
            options = new Dictionary<string, object>();
            arguments = [];
            return;
        }

        var opts = new Dictionary<string, object>();
        var unknown = ImmutableArray.CreateBuilder<string>();
        var smap = new SortedDictionary<string, IArgumentMetadata>(
            Comparer<string>.Create(static (s1, s2) => CompareByLength(s1, s2)));
        var nmap = new SortedDictionary<string, IArgumentMetadata>();

        foreach (var item in schema)
        {
            nmap.Add(item.LongAlias, item);

            if (item.ShortAlias is not '\0')
                smap.Add(item.ShortAlias.ToString(), item);
        }

        while (tokens.TryDequeue(out var arg))
        {
            if (arg.StartsWith("--", StringComparison.Ordinal))
            {
                if (arg.Length is 2)
                {
                    while (tokens.TryDequeue(out arg))
                    {
                        unknown.Add(arg);
                    }

                    break;
                }

                AddByName(arg[2..], tokens, nmap, opts);
            }
            else if (arg[0] is '-' or '/')
            {
                AddByShortName(arg[1..], tokens, smap, opts);
            }
            else
            {
                unknown.Add(arg);
            }
        }

        options = opts.AsReadOnly();
        arguments = unknown.ToImmutableArray();
    }

    private static int CompareByLength(string x, string y)
    {
        var d = y.Length - x.Length;

        return d != 0 ? d : CompareOrdinal(x, y);
    }

    private void AddByShortName(string arg, Queue<string> tokens, SortedDictionary<string, IArgumentMetadata> metadata, Dictionary<string, object> arguments)
    {
        if (metadata.TryGetValue(arg, out var def))
        {
            // Try parse as exact match
            var type = def.Type;
            var key = def.Name;

            if (type == typeof(bool))
            {
                if (tokens.TryPeek(out var str) && TryParseBoolean(str, out var value))
                {
                    arguments[key] = value;
                    tokens.Dequeue();
                }
                else
                {
                    arguments[key] = true;
                }
            }
            else
            {
                if (tokens.TryDequeue(out var value))
                    arguments[key] = type == typeof(string) ? value.Trim(Quotes) : Convert.ChangeType(value, type, InvariantCulture);
                else
                    ThrowMissingArgValue(key);
            }
        }
        else
        {
            // Try parse as argument+value concatenated

            if (TryParseAsJointKeyValuePair(arg, metadata, arguments)) return;

            // Try to parse as switches only concatenated

            if (TryParseJointSwitchesArgument(arg, metadata, arguments)) return;

            if (strict) ThrowInvalidArg(arg);

            arguments[arg] = tokens.TryPeek(out var next) && next[0] is not ('/' or '-') ? tokens.Dequeue() : "";
        }
    }

    private static bool TryParseAsJointKeyValuePair(string arg, SortedDictionary<string, IArgumentMetadata> metadata, Dictionary<string, object> options)
    {
        foreach (var (alias, def) in metadata)
        {
            var type = def.Type;
            var key = def.Name;

            if (!arg.StartsWith(alias, StringComparison.Ordinal)) continue;

            if (type != typeof(bool))
            {
                var value = arg[alias.Length..];
                options[key] = type == typeof(string) ? value.Trim(Quotes) : Convert.ChangeType(value, type, InvariantCulture);
                return true;
            }

            if (!TryParseBoolean(arg[alias.Length..], out var b)) continue;

            options[key] = b;

            return true;
        }

        return false;
    }

    private static bool TryParseJointSwitchesArgument(string arg, SortedDictionary<string, IArgumentMetadata> metadata, Dictionary<string, object> options)
    {
        var keys = new HashSet<string>();

        bool match;
        do
        {
            match = false;

            foreach (var (alias, def) in metadata)
            {
                if (def.Type != typeof(bool)) continue;

                if (!arg.StartsWith(alias, StringComparison.Ordinal)) continue;

                keys.Add(def.Name);
                arg = arg[alias.Length..];
                match = true;

                if (arg.Length == 0) break;
            }
        } while (match && arg.Length > 0);

        if (arg.Length > 0) return false;

        foreach (var key in keys)
        {
            options[key] = true;
        }

        return true;
    }

    private void AddByName(string arg, Queue<string> tokens, SortedDictionary<string, IArgumentMetadata> metadata, Dictionary<string, object> options)
    {
        var pair = arg.Split(Separator, 2);

        var key = pair[0];

        if (!metadata.TryGetValue(key, out var def))
        {
            if (strict) ThrowInvalidArg(key);
            options[key] = pair.Length > 1 ? pair[1] : "";
            return;
        }

        key = def.Name;
        var type = def.Type;

        if (type == typeof(bool))
        {
            if (pair.Length == 1) options[key] = true;
            else if (TryParseBoolean(pair[1], out var value)) options[key] = value;
            else ThrowInvalidSwitchValue(key);
        }
        else
        {
            if (pair.Length == 2)
            {
                var value = pair[1];
                options[key] = type == typeof(string) ? value.Trim(Quotes) : Convert.ChangeType(value, type, InvariantCulture);
            }
            else if (tokens.TryPeek(out var next) && next[0] is not ('/' or '-'))
            {
                options[key] = tokens.Dequeue();
            }
            else
            {
                ThrowMissingArgValue(arg);
            }
        }
    }

    private static bool TryParseBoolean(string str, out bool value)
    {
        value = false;

        return !IsNullOrWhiteSpace(str) && str switch
        {
            "True" => value = true,
            "true" => value = true,
            "1" => value = true,
            "False" => true,
            "false" => true,
            "0" => true,
            _ => false
        };
    }

    [DoesNotReturn]
    private static void ThrowMissingArgValue(string argName) =>
        throw new ArgumentException($"No value was specified for argument '{argName}'.");

    [DoesNotReturn]
    private static void ThrowInvalidArg(string argName) =>
        throw new ArgumentException($"Invalid argument '{argName}'.");

    [DoesNotReturn]
    private static void ThrowInvalidSwitchValue(string argName) =>
        throw new ArgumentException($"Invalid value for binary switch argument '{argName}'. Should be one of [True, False, true, false, 1, 0].");
}