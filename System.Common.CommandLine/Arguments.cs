using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace System.Common.CommandLine
{
    public class Arguments
    {
        private Arguments(string command, IReadOnlyDictionary<string, object> parameters, IEnumerable<string> extras)
        {
            Command = command;
            ProvidedValues = parameters;
            Extras = extras;
        }

        public string Command { get; }

        public IReadOnlyDictionary<string, object> ProvidedValues { get; }

        public IEnumerable<string> Extras { get; }

        public bool this[string name] => (bool)ProvidedValues[name];

        public static Arguments Parse(params string[] args)
        {
            var queue = new Queue<string>(args);

            var argumentByNameComparer = new EqualityComparerAdapter<ArgumentAttribute>((c1, c2) =>
                string.Equals(c1.Name, c2.Name, StringComparison.OrdinalIgnoreCase));
            var schema = Assembly.GetEntryAssembly()?
                 .GetCustomAttributes<ArgumentAttribute>()
                 .Distinct(argumentByNameComparer)
                 .ToArray() ?? Array.Empty<ArgumentAttribute>();

            var commandByNameComparer = new EqualityComparerAdapter<CommandAttribute>((c1, c2) =>
                string.Equals(c1.Name, c2.Name, StringComparison.OrdinalIgnoreCase));
            var commands = Assembly.GetEntryAssembly()?
                .GetCustomAttributes<CommandAttribute>()
                .Distinct(commandByNameComparer)
                .ToArray() ?? Array.Empty<CommandAttribute>();

            var parser = new ArgumentParser(commands, schema);

            parser.Parse(queue, out var command, out var parameters, out var extras);

            return new Arguments(command, parameters, extras);
        }
    }
}