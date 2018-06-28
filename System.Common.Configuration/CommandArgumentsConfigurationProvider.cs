using System.CommandLine;
using System.Linq;
using Microsoft.Extensions.Configuration;

namespace System.Configuration
{
    public sealed class CommandArgumentsConfigurationProvider : ConfigurationProvider
    {
        private readonly string[] args;

        public CommandArgumentsConfigurationProvider(string[] args)
        {
            this.args = args;
        }

        public override bool TryGet(string key, out string value)
        {
            return base.TryGet(key, out value);
        }

        public override void Set(string key, string value)
        {
            base.Set(key, value);
        }

        public override void Load()
        {
            var arguments = Arguments.Parse(args);

            Data = arguments.AllArguments.ToDictionary(a => a.Key, a => a.Value?.ToString());

            Data["Command"] = arguments.Command;
        }

        public override IEnumerable<string> GetChildKeys(IEnumerable<string> earlierKeys, string parentPath)
        {
            return base.GetChildKeys(earlierKeys, parentPath);
        }
    }
}