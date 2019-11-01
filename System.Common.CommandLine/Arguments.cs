using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using static System.String;

namespace System.Common.CommandLine
{
    public class Arguments
    {
        private Arguments(string command, IDictionary<string, object> parameters, IList<string> extras)
        {
            Command = command;
            AllArguments = new ReadOnlyDictionary<string, object>(parameters);
            Extras = new ReadOnlyCollection<string>(extras);
        }

        public string Command { get; }

        public IReadOnlyDictionary<string, object> AllArguments { get; }

        public IReadOnlyList<string> Extras { get; }

        public bool this[string name] => (bool)AllArguments[name];

        public static Arguments Parse(params string[] args)
        {
            var queue = new Queue<string>(args);

            Parse(queue, out var command, out var parameters, out var extras);

            return new Arguments(command, parameters, extras);
        }

        private static void Parse(Queue<string> queue, out string command,
            out Dictionary<string, object> arguments,
            out List<string> extras)
        {
            int CompareByLength(string x, string y)
            {
                var d = y.Length - x.Length;

                return d != 0 ? d : CompareOrdinal(x, y);
            }

            command = default;
            arguments = new Dictionary<string, object>();
            extras = new List<string>();

            var comparer = new ComparerAdapter<string>(CompareByLength);
            var commandByNameComparer = new EqualityComparerAdapter<CommandAttribute>((c1, c2) =>
                string.Equals(c1.Name, c2.Name, StringComparison.OrdinalIgnoreCase));
            var argumentByNameComparer =
                new EqualityComparerAdapter<ArgumentAttribute>((c1, c2) =>
                    string.Equals(c1.Name, c2.Name, StringComparison.OrdinalIgnoreCase));

            var smap = new SortedDictionary<string, ArgumentAttribute>(comparer);
            var nmap = new SortedDictionary<string, ArgumentAttribute>(comparer);

            var schema = Assembly.GetEntryAssembly()?
                             .GetCustomAttributes<ArgumentAttribute>()
                             .Distinct(argumentByNameComparer)
                             .ToArray() ?? Array.Empty<ArgumentAttribute>();

            var commands = Assembly.GetEntryAssembly()?
                               .GetCustomAttributes<CommandAttribute>()
                               .Distinct(commandByNameComparer)
                               .ToArray() ?? Array.Empty<CommandAttribute>();

            foreach(var item in schema)
            {
                if(item.Optional) arguments.Add(item.Name, item.DefaultValue);

                nmap.Add(item.Name, item);

                if(item.Synonym != null) smap.Add(item.Synonym, item);
            }

            command = commands.SingleOrDefault(c => c.Default)?.Name;

            if(queue.Count == 0) return;

            var arg = queue.Peek();

            if(!arg.StartsWith("-") && !arg.StartsWith("/"))
            {
                var name = arg;

                command = commands.FirstOrDefault(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase))?.Name;

                queue.Dequeue();
            }

            while(queue.Count > 0)
            {
                arg = queue.Dequeue();

                if(arg.StartsWith("--"))
                {
                    AddBySynonym(arg.Substring(2), smap, arguments);
                }
                else if(arg[0] == '-' || arg[0] == '/')
                {
                    AddByName(arg.Substring(1), queue, nmap, arguments);
                }
                else
                {
                    extras.Add(arg);
                }
            }
        }

        private static void AddByName(string arg, Queue<string> queue,
            IDictionary<string, ArgumentAttribute> nmap,
            Dictionary<string, object> arguments)
        {
            if(nmap.TryGetValue(arg, out var def))
            {
                // Try parse as exact match

                if(def.Type == typeof(bool))
                {
                    if(TryParseBoolean(queue.Peek(), out var value))
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
                    if(queue.Count == 0) throw new ArgumentException($"No value was specified for argument {arg}");

                    arguments[arg] = Convert.ChangeType(queue.Dequeue(), def.Type);
                }
            }
            else
            {
                // Try parse as argument+value concatenated

                if(TryParseAsJointKeyValuePair(arg, nmap, arguments)) return;

                // Try to parse as switches only concatenated

                if(!TryParseJointSwitchesArgument(arg, nmap, arguments))
                {
                    throw new ArgumentException($"Invalid argument '{arg}'");
                }
            }
        }

        private static bool TryParseAsJointKeyValuePair(string arg,
            IDictionary<string, ArgumentAttribute> nmap, Dictionary<string, object> arguments)
        {
            foreach(var item in nmap)
            {
                var def = item.Value;
                var type = def.Type;
                var key = item.Key;

                if(arg.StartsWith(key))
                {
                    if(type != typeof(bool))
                    {
                        arguments[arg] = Convert.ChangeType(arg.Substring(key.Length), type);

                        return true;
                    }

                    if(TryParseBoolean(arg.Substring(key.Length), out var b))
                    {
                        arguments[arg] = b;

                        return true;
                    }
                }
            }

            return false;
        }

        private static bool TryParseJointSwitchesArgument(string arg,
            IDictionary<string, ArgumentAttribute> nmap, Dictionary<string, object> arguments)
        {
            var keys = new HashSet<string>();

            bool match;
            do
            {
                match = false;

                foreach(var item in nmap)
                {
                    if(item.Value.Type != typeof(bool)) continue;

                    var key = item.Key;

                    if(arg.StartsWith(key))
                    {
                        keys.Add(key);

                        arg = arg.Substring(key.Length);

                        match = true;

                        if(arg == Empty) break;
                    }
                }
            } while(match && arg != Empty);

            if(arg != Empty) return false;

            foreach(var k in keys)
            {
                arguments[k] = true;
            }

            return true;
        }

        private static void AddBySynonym(string arg,
            IDictionary<string, ArgumentAttribute> smap,
            Dictionary<string, object> arguments)
        {
            var pair = arg.Split(new[] {'='}, 2);

            if(smap.TryGetValue(pair[0], out var def))
            {
                var key = def.Name;

                if(def.Type == typeof(bool))
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
                        throw new ArgumentException("Invalid value for binary switch argument " + key +
                                                    " Should be one of [True, False, true, false, 1, 0]");
                    }
                }
                else
                {
                    if(pair.Length == 2)
                    {
                        arguments[key] = Convert.ChangeType(pair[1], def.Type);
                    }
                    else
                    {
                        throw new ArgumentException("Missing value for non-switch parameter " + key);
                    }
                }
            }
        }

        private static bool TryParseBoolean(string str, out bool value)
        {
            value = false;

            if(IsNullOrWhiteSpace(str)) return false;

            if(str == "True" || str == "true" || str == "1")
            {
                value = true;
                return true;
            }

            if(str == "False" || str == "false" || str == "0") return true;

            return false;
        }
    }
}