using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using static OOs.CommandLine.Generators.UnknownOptionBehavior;

#pragma warning disable CA1308 // Normalize strings to uppercase

namespace OOs.CommandLine.Generators;

public static class ArgumentParserCodeEmitter
{
    internal static string Emit(string namespaceName, string typeName, TypeKind kind, Accessibility accessibility,
        ImmutableArray<OptionGenerationContext> options, TypeGenerationOptions generationOptions)
    {
        var typeAccessibility = accessibility is Accessibility.Public ? "public" : "internal";
        var typeKind = kind is TypeKind.Struct ? "struct" : "class";
        var (addStandardOptions, generateSynopsis, unknownOptionBehavior) = generationOptions;

        var emitBooleanSupport = addStandardOptions;
        var emitTimeSpanSupport = false;
        var emitEnumSupport = false;
        var emitReadRawSupport = false;
        var emitBooleanShortFormSupport = addStandardOptions;
        var emitTimeSpanShortFormSupport = false;
        var emitEnumShortFormSupport = false;
        var emitReadRawShortSupport = unknownOptionBehavior is Allow;

        foreach (var option in options)
        {
            switch (option)
            {
                case { TypeContext.KnownType: WellKnownType.Boolean, ShortAlias: var alias }:
                    emitBooleanSupport = true;
                    emitBooleanShortFormSupport |= alias is not '\0';
                    break;
                case { TypeContext.KnownType: WellKnownType.TimeSpan, ShortAlias: var alias }:
                    emitTimeSpanSupport = true;
                    emitTimeSpanShortFormSupport |= alias is not '\0';
                    break;
                case { TypeContext.KnownType: WellKnownType.Enum, ShortAlias: var alias }:
                    emitEnumSupport = true;
                    emitEnumShortFormSupport |= alias is not '\0';
                    break;
                case { ShortAlias: var alias }:
                    emitReadRawSupport = true;
                    emitReadRawShortSupport |= alias is not '\0';
                    break;
            }
        }

        var emitReadNextRawSupport = emitReadRawSupport || emitReadRawShortSupport;

        var sb = new StringBuilder();
        CodeEmitHelper.AppendFileHeader(sb);
        sb.AppendLine();
        sb.Append("""
#pragma warning disable CS1591
#nullable enable


""");
        if (!string.IsNullOrWhiteSpace(namespaceName))
        {
            sb.Append($$"""
namespace {{namespaceName}};


""");
        }

        CodeEmitHelper.AppendGeneratedCodeAttribute(sb);
        sb.Append($$"""

{{typeAccessibility}} partial {{typeKind}} {{typeName}}: global::OOs.CommandLine.IArgumentsParser
{
    public static (global::System.Collections.Generic.IReadOnlyDictionary<string, string?> Options, global::System.Collections.Immutable.ImmutableArray<string> Arguments) Parse(global::System.ReadOnlySpan<string> args)
    {
        var options = new global::System.Collections.Generic.Dictionary<string, string?>();
        var builder = global::System.Collections.Immutable.ImmutableArray.CreateBuilder<string>(args.Length);

        for (var index = 0; index < args.Length; index++)
        {
            var token = args[index];
            var span = token.AsSpan();
            string key, name;

""");
        if (emitEnumSupport)
        {
            sb.Append("""
            scoped global::System.ReadOnlySpan<string> enumValues = default;

""");
        }

        sb.Append("""

            if (span.StartsWith("--"))
            {
                span = span.Slice(2);

                if (span.IsEmpty)
                {
                    builder.AddRange(args.Slice(index + 1));
                    break;
                }


""");

        var i = 0;
        foreach (var option in options)
        {
            if (option is { Name: { } name, Alias: { Length: var len } alias, TypeContext: { KnownType: var type, AllowedValues: var values } })
            {
                sb.Append($$"""
                {{(i++ is 0 ? "if" : "else if")}} ((span.Length == {{len}} || span.Length > {{len}} && span[{{len}}] == '=') && span.Slice(0, {{len}}).SequenceEqual("{{alias}}"))
                {

""");
                sb.Append($$"""
                    key = "{{name}}";
                    name = "--{{alias}}";
                    span = span.Slice({{len}});

""");
                if (type is WellKnownType.Boolean)
                {
                    sb.Append($$"""
                                    goto ReadAsBoolean;

                """);
                }
                else if (type is WellKnownType.TimeSpan)
                {
                    sb.Append($$"""
                                    goto ReadAsTimeSpan;

                """);
                }
                else if (type is WellKnownType.Enum)
                {
                    sb.Append("""
                    enumValues = [
""");
                    for (var j = 0; j < values.Length; j++)
                    {
                        var value = values[j];
                        sb.Append('"');
                        sb.Append(value);
                        sb.Append('"');
                        if (j != values.Length - 1)
                        {
                            sb.Append(", ");
                        }
                    }

                    sb.Append("""
];
                    goto ReadAsEnum;

""");
                }

                sb.Append("""
                }

""");
            }
        }

        if (addStandardOptions)
        {
            sb.Append("""
                else if ((span.Length == 4 || span.Length > 4 && span[4] == '=') && span.Slice(0, 4).SequenceEqual("help"))
                {
                    key = "PrintHelp";
                    name = "--help";
                    span = span.Slice(4);
                    goto ReadAsBoolean;
                }
                else if ((span.Length == 7 || span.Length > 7 && span[7] == '=') && span.Slice(0, 7).SequenceEqual("version"))
                {
                    key = "PrintVersion";
                    name = "--version";
                    span = span.Slice(7);
                    goto ReadAsBoolean; 
                }

""");
        }

        if (unknownOptionBehavior is Allow)
        {
            sb.Append("""
                else
                {
                    if (span.IndexOf('=') is >= 0 and var i)
                    {
                        key = new(span.Slice(0, i));
                        options[key] = new(span.Slice(i + 1));
                        continue;
                    }
                    else
                    {
                        key = new(span);
                        name = token;
                        goto TryReadNext;
                    }
                }

""");
        }
        else if (unknownOptionBehavior is Preserve)
        {
            sb.Append("""
                else
                {
                    builder.Add(token);
                    continue;
                }

""");
        }
        else if (unknownOptionBehavior is Prohibit)
        {
            sb.Append("""
                else
                {
                    ThrowInvalidOption(span.IndexOf('=') is >= 0 and var i ? new(token.AsSpan().Slice(0, i + 2)) : token);
                    continue;
                }

""");
        }
        else
        {
            sb.Append("""
                else
                {
                    continue;
                }

""");
        }

        if (emitReadRawSupport)
        {
            sb.Append("""

                if (span.StartsWith("="))
                {
                    options[key] = new(span.Slice(1));
                    continue;
                }
                else
                {
                    goto TryReadNext;
                }

""");
        }

        if (emitBooleanSupport)
        {
            sb.Append("""

            ReadAsBoolean:
                if (span.StartsWith("="))
                {
                    if (TryParseBoolean(span.Slice(1), out var value))
                    {
                        options[key] = value ? "True" : "False";
                        continue;
                    }
                    else
                    {
                        ThrowInvalidOptionValue(name);
                    }
                }
                else
                {
                    goto TryReadNextAsBoolean;
                }

""");
        }

        if (emitTimeSpanSupport)
        {
            sb.Append("""

            ReadAsTimeSpan:
                if (span.StartsWith("="))
                {
                    if (int.TryParse(span.Slice(1), out var ms))
                    {
                        options[key] = global::System.TimeSpan.FromMilliseconds(ms).ToString();
                        continue;
                    }
                    else if (global::System.TimeSpan.TryParse(span.Slice(1), out var value))
                    {
                        options[key] = value.ToString();
                        continue;
                    }
                    else
                    {
                        ThrowInvalidOptionValue(name);
                    }
                }
                else
                {
                    goto TryReadNextAsTimeSpan;
                }

""");
        }

        if (emitEnumSupport)
        {
            sb.Append("""

            ReadAsEnum:
                if (span.StartsWith("="))
                {
                    string value = new(span.Slice(1));
                    if (ResolveEnumValueName(enumValues, value) is { } exactValue)
                    {
                        options[key] = exactValue;
                        continue;
                    }
                    else
                    {
                        ThrowInvalidOptionValue(name);
                    }
                }
                else
                {
                    goto TryReadNextAsEnum;
                }

""");
        }

        sb.Append("""
            }
            else if (span.StartsWith("-"))
            {
                for (var i = 1; i < span.Length; i++)
                {
                    switch (span[i]) 
                    {

""");

        foreach (var option in options)
        {
            if (option is
                {
                    Name: { } name,
                    ShortAlias: not '\0' and var alias,
                    TypeContext: { KnownType: var type, AllowedValues: var values }
                })
            {
                sb.Append($$"""
                        case '{{alias}}':

""");
                sb.Append($$"""
                            key = "{{name}}";
                            name = "-{{alias}}";

""");
                if (type is WellKnownType.Boolean)
                {
                    sb.Append($$"""
                            goto ReadAsBooleanShort;

""");
                }
                else if (type is WellKnownType.TimeSpan)
                {
                    sb.Append($$"""
                            goto ReadAsTimeSpanShort;

""");
                }
                else if (type is WellKnownType.Enum)
                {
                    sb.Append("""
                            enumValues = [
""");
                    for (var j = 0; j < values.Length; j++)
                    {
                        var value = values[j];
                        sb.Append('"');
                        sb.Append(value);
                        sb.Append('"');
                        if (j != values.Length - 1)
                        {
                            sb.Append(", ");
                        }
                    }

                    sb.Append("""
];
                            goto ReadAsEnumShort;

""");
                }
                else
                {
                    sb.Append($$"""
                            break;

""");
                }
            }
        }

        if (addStandardOptions)
        {
            sb.Append("""
                        case 'h' or '?':
                            key = "PrintHelp";
                            name = "-h";
                            goto ReadAsBooleanShort;
                        case 'V':
                            key = "PrintVersion";
                            name = "-V";
                            goto ReadAsBooleanShort;

""");
        }

        if (unknownOptionBehavior is Allow)
        {
            sb.Append($$"""
                        case { } value:
                            key = new(value, 1);
                            name = $"-{value}";
                            break;
                    }

""");
        }
        else if (unknownOptionBehavior is Preserve)
        {
            sb.Append($$"""
                        default:
                            if (i == 1)
                            {
                                builder.Add(token);
                            }

                            goto Next;
                    }

""");
        }
        else if (unknownOptionBehavior is Prohibit)
        {
            sb.Append($$"""
                        case { } value:
                            ThrowInvalidOption(new(['-', value]));
                            continue;
                    }

""");
        }
        else
        {
            sb.Append($$"""
                        default: 
                            goto Next;
                    }

""");
        }

        if (emitReadRawShortSupport)
        {
            sb.Append($$"""

                    if (++i < span.Length)
                    {
                        options[key] = new(span.Slice(i));
                        break;
                    }
                    else
                    {
                        goto TryReadNext;
                    }

""");
        }

        if (emitBooleanShortFormSupport)
        {
            sb.Append("""

                ReadAsBooleanShort:
                    if (++i < span.Length)
                    {
                        if (TryParseBoolean(span.Slice(i), out var value))
                        {
                            options[key] = value ? "True" : "False";
                            break;
                        }
                        else
                        {
                            options[key] = "True";
                            i--;
                            continue;
                        }
                    }
                    else
                    {
                        goto TryReadNextAsBoolean;
                    }

""");
        }

        if (emitTimeSpanShortFormSupport)
        {
            sb.Append("""

                ReadAsTimeSpanShort:
                    if (++i < span.Length)
                    {
                        if (int.TryParse(span.Slice(i), out var ms))
                        {
                            options[key] = global::System.TimeSpan.FromMilliseconds(ms).ToString();
                            break;
                        }
                        else if (global::System.TimeSpan.TryParse(span.Slice(i), out var value))
                        {
                            options[key] = value.ToString();
                            break;
                        }
                        else
                        {
                            ThrowInvalidOptionValue(name);
                        }
                    }
                    else
                    {
                        goto TryReadNextAsTimeSpan;
                    }

""");
        }

        if (emitEnumShortFormSupport)
        {
            sb.Append("""

                ReadAsEnumShort:
                    if (++i < span.Length)
                    {
                        var value = new string(span.Slice(i));
                        if (ResolveEnumValueName(enumValues, value) is { } exactValue)
                        {
                            options[key] = exactValue;
                            break;
                        }

                        ThrowInvalidOptionValue(name);
                    }
                    else
                    {
                        goto TryReadNextAsEnum;
                    }

""");
        }

        sb.Append("""
                }
            }
            else
            {
                builder.Add(token);
            }


""");
        if (unknownOptionBehavior is Preserve or Ignore)
        {
            sb.Append("""
        Next:

""");
        }

        sb.Append("""
            continue;

""");
        if (emitReadNextRawSupport)
        {
            sb.Append("""

        TryReadNext:
            if (++index < args.Length)
            {
                var value = args[index];
                if (!value.StartsWith('-'))
                {
                    options[key] = value;
                    continue;
                }
            }

            ThrowMissingOptionValue(name);

""");
        }

        if (emitBooleanSupport)
        {
            sb.Append("""

        TryReadNextAsBoolean:
            if (++index < args.Length)
            {
                var value = args[index];
                if (TryParseBoolean(value, out var bvalue))
                {
                    options[key] = bvalue ? "True" : "False";
                    continue;
                }
                
                index--;
            }

            options[key] = "True";
            continue;

""");
        }

        if (emitTimeSpanSupport)
        {
            sb.Append("""

        TryReadNextAsTimeSpan:
            if (++index < args.Length)
            {
                var value = args[index];
                if (int.TryParse(value, out var ms))
                {
                    options[key] = global::System.TimeSpan.FromMilliseconds(ms).ToString();
                    continue;
                }
                else if (global::System.TimeSpan.TryParse(value, out var tvalue))
                {
                    options[key] = tvalue.ToString();
                    continue;
                }
                else
                {
                    ThrowInvalidOptionValue(name);
                }
                
                index--;
            }

            ThrowMissingOptionValue(name);

""");
        }

        if (emitEnumSupport)
        {
            sb.Append("""

        TryReadNextAsEnum:
            if (++index < args.Length)
            {
                var value = args[index];
                if (!value.StartsWith('-'))
                {
                    if (ResolveEnumValueName(enumValues, value) is { } exactValue)
                    {
                        options[key] = exactValue;
                        continue;
                    }

                    ThrowInvalidOptionValue(name);
                }
            }

            ThrowMissingOptionValue(name);

""");
        }

        sb.Append("""
        }

        return (options, builder.ToImmutable());
    }

""");

        if (emitBooleanSupport)
        {
            sb.Append("""

    private static bool TryParseBoolean(global::System.ReadOnlySpan<char> span, out bool value)
    {
        switch (span)
        {
            case ['1']:
                value = true;
                return true;
            case ['0']:
                value = false;
                return true;
            case { Length: >= 4 }:
                return bool.TryParse(span, out value);
            default:
                value = false;
                return false;
        }
    }

""");
        }

        if (emitEnumSupport)
        {
            sb.Append("""

    private static string? ResolveEnumValueName(global::System.ReadOnlySpan<string> valueNames, string name)
    {
        foreach (string str in valueNames)
        {
            if (global::System.StringComparer.OrdinalIgnoreCase.Equals(str, name))
            {
                return str;
            }
        }

        return null;
    }

""");
        }

        if (unknownOptionBehavior is Prohibit)
        {
            sb.Append("""

    [global::System.Diagnostics.CodeAnalysis.DoesNotReturn]
    private static void ThrowInvalidOption(string optionName)
    {
        throw new global::System.InvalidOperationException($"Unknown option '{optionName}'.");
    }

""");
        }

        sb.Append("""

    [global::System.Diagnostics.CodeAnalysis.DoesNotReturn]
    private static void ThrowMissingOptionValue(string optionName)
    {
        throw new global::System.InvalidOperationException($"Missing value for option '{optionName}'.");
    }

""");

        if (emitBooleanSupport || emitTimeSpanSupport || emitEnumSupport)
        {
            sb.Append("""

    [global::System.Diagnostics.CodeAnalysis.DoesNotReturn]
    private static void ThrowInvalidOptionValue(string optionName)
    {
        throw new global::System.InvalidOperationException($"Invalid value for option '{optionName}'.");
    }

""");
        }

        if (generateSynopsis)
        {
            sb.Append(""""

    public static string GetSynopsis()
    {
        return """

"""");

            var maxLen = 0;
            var standardOptionsCount = addStandardOptions ? 2 : 0;
            var lines = new (string Alias, string? Description)[options.Length + standardOptionsCount];
            var span = lines.AsSpan();

            if (addStandardOptions)
            {
                span[0] = ("-h, -?, --help", "Print this help.");
                span[1] = ("-V, --version", "Print application's version information.");
                span = span.Slice(2);
            }

            for (var index = 0; index < options.Length; index++)
            {
                var (name, longAlias, shortAlias, (type, values), description, hint) = options[index];
                var shortAliasPart = shortAlias is not '\0' ? "-" + shortAlias + ", " : null;
                var hintPart = type is not WellKnownType.Boolean ? $" <{hint ?? name.ToLowerInvariant()}>" : null;
                var alias = @$"{shortAliasPart}--{longAlias}{hintPart}";

                if (type is WellKnownType.Enum && values is not [])
                {
                    description = $"{description} Allowed values are {string.Join(", ", values)}.";
                }

                span[index] = (alias, description);
                if (alias.Length > maxLen)
                {
                    maxLen = alias.Length;
                }
            }

            foreach (var (Alias, Description) in lines)
            {
                sb.Append($"""
    {Alias.PadRight(maxLen)}    {Description}

""");
            }

            sb.Append(""""
""";
    }

"""");
        }

        sb.Append('}');

        return sb.ToString();
    }
}