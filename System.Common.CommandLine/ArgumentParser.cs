using static System.String;
using static System.Globalization.CultureInfo;

namespace System.Common.CommandLine;

public class ArgumentParser
{
    ICommandMetadata[] commands;
    IArgumentMetadata[] schema;
    static char[] quotes = { '"', '\'' };

    public ArgumentParser(ICommandMetadata[] commands, IArgumentMetadata[] schema)
    {
        ArgumentNullException.ThrowIfNull(commands);
        ArgumentNullException.ThrowIfNull(schema);

        this.commands = commands;
        this.schema = schema;
    }

    public void Parse(Queue<string> tokens, out string command, out IReadOnlyDictionary<string, object> arguments, out IEnumerable<string> extras)
    {
        ArgumentNullException.ThrowIfNull(tokens);

        command = default;
        var args = new Dictionary<string, object>();
        var unknown = new List<string>();
        arguments = args;
        extras = unknown;

        var comparer = new ComparerAdapter<string>((x1, x2) => CompareByLength(x1, x2));

        var smap = new SortedDictionary<string, IArgumentMetadata>(comparer);
        var nmap = new SortedDictionary<string, IArgumentMetadata>();

        foreach(var item in schema)
        {
            nmap.Add(item.Name, item);

            if(item.ShortName != null) smap.Add(item.ShortName, item);
        }

        command = commands.SingleOrDefault(c => c.IsDefault)?.Name;

        if(tokens.Count == 0) return;

        var arg = tokens.Peek();

        if(!arg.StartsWith('-') && !arg.StartsWith('/'))
        {
            var name = arg;

            command = commands.FirstOrDefault(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase))?.Name;

            tokens.Dequeue();
        }

        while(tokens.Count > 0)
        {
            arg = tokens.Dequeue();

            if(arg.StartsWith("--", false, InvariantCulture))
            {
                AddByName(arg[2..], nmap, args);
            }
            else if(arg[0] == '-' || arg[0] == '/')
            {
                AddByShortName(arg[1..], tokens, smap, args);
            }
            else
            {
                unknown.Add(arg);
            }
        }
    }

    private static int CompareByLength(string x, string y)
    {
        var d = y.Length - x.Length;

        return d != 0 ? d : CompareOrdinal(x, y);
    }

    private static void AddByShortName(string arg, Queue<string> queue, IDictionary<string, IArgumentMetadata> metadata, IDictionary<string, object> arguments)
    {
        if(metadata.TryGetValue(arg, out var def))
        {
            // Try parse as exact match
            var type = def.Type;

            if(type == typeof(bool))
            {
                if(queue.TryPeek(out var str) && TryParseBoolean(str, out var value))
                {
                    arguments[arg] = value;
                    queue.Dequeue();
                }
                else
                {
                    arguments[arg] = true;
                }
            }
            else
            {
                if(queue.TryDequeue(out var value))
                    arguments[arg] = type == typeof(string) ? value.Trim(quotes) : Convert.ChangeType(value, type, InvariantCulture);
                else
                    throw new ArgumentException($"No value was specified for argument {arg}");
            }
        }
        else
        {
            // Try parse as argument+value concatenated

            if(TryParseAsJointKeyValuePair(arg, metadata, arguments)) return;

            // Try to parse as switches only concatenated

            if(!TryParseJointSwitchesArgument(arg, metadata, arguments))
            {
                throw new ArgumentException($"Invalid argument '{arg}'");
            }
        }
    }

    private static bool TryParseAsJointKeyValuePair(string arg, IDictionary<string, IArgumentMetadata> metadata, IDictionary<string, object> arguments)
    {
        foreach(var (key, def) in metadata)
        {
            var type = def.Type;

            if(!arg.StartsWith(key, false, InvariantCulture)) continue;

            if(type != typeof(bool))
            {
                string value = arg[key.Length..];
                arguments[key] = type == typeof(string) ? value.Trim(quotes) : Convert.ChangeType(value, type, InvariantCulture);
                return true;
            }

            if(!TryParseBoolean(arg[key.Length..], out var b)) continue;

            arguments[key] = b;

            return true;
        }

        return false;
    }

    private static bool TryParseJointSwitchesArgument(string arg, IDictionary<string, IArgumentMetadata> metadata, IDictionary<string, object> arguments)
    {
        var keys = new HashSet<string>();

        bool match;
        do
        {
            match = false;

            foreach(var (key, value) in metadata)
            {
                if(value.Type != typeof(bool)) continue;

                if(!arg.StartsWith(key, false, InvariantCulture)) continue;

                keys.Add(key);
                arg = arg[key.Length..];
                match = true;

                if(arg.Length == 0) break;
            }
        } while(match && arg.Length > 0);

        if(arg.Length > 0) return false;

        foreach(var k in keys)
        {
            arguments[k] = true;
        }

        return true;
    }

    private static void AddByName(string arg, IDictionary<string, IArgumentMetadata> metadata, IDictionary<string, object> arguments)
    {
        var pair = arg.Split(new[] { '=' }, 2);

        if(!metadata.TryGetValue(pair[0], out var def)) return;

        var key = def.Name;
        var type = def.Type;

        if(type == typeof(bool))
        {
            if(pair.Length == 1)
            {
                arguments[key] = true;
            }
            else if(TryParseBoolean(pair[1], out var b))
            {
                arguments[key] = b;
            }
            else
            {
                throw new ArgumentException($"Invalid value for binary switch argument {key} Should be one of [True, False, true, false, 1, 0]");
            }
        }
        else
        {
            if(pair.Length == 2)
            {
                string value = pair[1];
                arguments[key] = type == typeof(string) ? value.Trim(quotes) : Convert.ChangeType(value, type, InvariantCulture);
            }
            else
            {
                throw new ArgumentException($"Missing value for non-switch parameter {key}");
            }
        }
    }

    private static bool TryParseBoolean(string str, out bool value)
    {
        value = false;

        if(IsNullOrWhiteSpace(str)) return false;

        return str switch
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
}