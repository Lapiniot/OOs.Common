using System.Collections.Generic;
using System.Common.CommandLine;
using System.Globalization;
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
            if(key is null) throw new ArgumentNullException(nameof(key));

            value = null;

            return key.StartsWith("args:", false, CultureInfo.InvariantCulture) && base.TryGet(key[5..], out value);
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