using System.Collections.Generic;
using System.Common.CommandLine;
using System.Globalization;
using Microsoft.Extensions.Configuration;

namespace System.Configuration;

public sealed class CommandArgumentsConfigurationProvider : ConfigurationProvider
{
    private readonly string prefix;
    private readonly string[] args;

    public CommandArgumentsConfigurationProvider(string[] args, string sectionName)
    {
        ArgumentNullException.ThrowIfNull(args);
        this.args = args;
        this.prefix = !string.IsNullOrEmpty(sectionName) ? !sectionName.EndsWith(":", StringComparison.InvariantCultureIgnoreCase) ? sectionName + ":" : sectionName : "args:";
    }

    public override bool TryGet(string key, out string value)
    {
        // if key is prefixed with 'args:' we treat it is as explicit query to our provider, otherwise let's make a generic lookup
        return key.StartsWith(prefix, false, CultureInfo.InvariantCulture) && base.TryGet(key[5..], out value) || base.TryGet(key, out value);
    }

    public override void Load()
    {
        var arguments = Arguments.Parse(args);

        Data = arguments.ProvidedValues.ToDictionary(a => a.Key, a => a.Value?.ToString());
        Data["Command"] = arguments.Command;
    }
}