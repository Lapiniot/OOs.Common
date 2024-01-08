using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.Extensions.Configuration;
using OOs.CommandLine;

namespace OOs.Configuration;

public sealed class CommandArgumentsConfigurationProvider : ConfigurationProvider
{
    private readonly string[] args;
    private readonly string prefix;
    private readonly bool strict;

    public CommandArgumentsConfigurationProvider(string[] args, string sectionName, bool strict)
    {
        ArgumentNullException.ThrowIfNull(args);
        this.args = args;
        prefix = !string.IsNullOrEmpty(sectionName) ? !sectionName.EndsWith(':') ? sectionName + ":" : sectionName : "args:";
        this.strict = strict;
    }

    // if key is prefixed with 'args:' we treat it is as explicit query to our provider, otherwise let's make a generic lookup
    public override bool TryGet([NotNull] string key, out string value) =>
        key.StartsWith(prefix, false, CultureInfo.InvariantCulture) && base.TryGet(key[5..], out value) || base.TryGet(key, out value);

    public override void Load()
    {
        var arguments = Arguments.Parse(args, strict);
        var values = arguments.ProvidedValues;

        var data = new Dictionary<string, string>(capacity: values.Count, StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in values)
        {
            data[key.Replace("-", "", StringComparison.OrdinalIgnoreCase)] = value?.ToString();
        }

        data["Command"] = arguments.Command;
        Data = data;
    }
}