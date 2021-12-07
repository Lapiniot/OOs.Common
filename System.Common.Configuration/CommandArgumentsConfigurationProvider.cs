using System.Collections.Generic;
using System.Common.CommandLine;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace System.Configuration;

public sealed class CommandArgumentsConfigurationProvider : ConfigurationProvider
{
    private readonly string prefix;
    private readonly string[] args;
    private readonly bool strict;

    public CommandArgumentsConfigurationProvider(string[] args, string sectionName, bool strict)
    {
        ArgumentNullException.ThrowIfNull(args);
        this.args = args;
        prefix = !string.IsNullOrEmpty(sectionName) ? !sectionName.EndsWith(":", StringComparison.InvariantCultureIgnoreCase) ? sectionName + ":" : sectionName : "args:";
        this.strict = strict;
    }

    public override bool TryGet([NotNull] string key, out string value)
    {
        // if key is prefixed with 'args:' we treat it is as explicit query to our provider, otherwise let's make a generic lookup
        return key.StartsWith(prefix, false, CultureInfo.InvariantCulture) && base.TryGet(key[5..], out value) || base.TryGet(key, out value);
    }

    public override void Load()
    {
        var arguments = Arguments.Parse(args, strict);
        var values = arguments.ProvidedValues;

        // Make configuration value acessible via both original and upper camel case argument name
        Data = values.ToDictionary(a => a.Key, a => (a.Value?.ToString()));
        foreach(var (key, value) in values)
        {
            Data[ConvertToUpperCamelCase(key)] = value?.ToString();
        }
        Data["Command"] = arguments.Command;
    }

    private static string ConvertToUpperCamelCase(string key)
    {
        var sb = new StringBuilder();
        var jumpCase = true;
        for(int i = 0; i < key.Length; i++)
        {
            if(key[i] == '-')
            {
                jumpCase = true;
                continue;
            }

            if(jumpCase)
            {
                sb.Append(char.ToUpperInvariant(key[i]));
                jumpCase = false;
                continue;
            }

            sb.Append(key[i]);
        }
        return sb.ToString();
    }
}