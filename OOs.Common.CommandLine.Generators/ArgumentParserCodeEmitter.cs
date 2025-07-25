using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;

#pragma warning disable CA1308 // Normalize strings to uppercase

namespace OOs.CommandLine.Generators;

public static class ArgumentParserCodeEmitter
{
    internal static string Emit(string namespaceName, string typeName, TypeKind kind, Accessibility accessibility,
        ImmutableArray<OptionGenerationContext> options, bool generateSynopsis, bool addStandardOptions,
        KnownTypes knownTypes)
    {
        var typeAccessibility = accessibility is Accessibility.Public ? "public" : "internal";
        var typeKind = kind is TypeKind.Struct ? "struct" : "class";

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
            string name;
            if (span.StartsWith("--"))
            {
                span = span.Slice(2);

                if (span.IsEmpty)
                {
                    // Special "--" (end of option arguments) marker detected - 
                    // read the rest of args as regular positional arguments
                    builder.AddRange(args.Slice(index + 1));
                    break;
                }


""");

        var i = 0;
        foreach (var option in options)
        {
            if (option is { Name: { } name, Alias: { Length: var len } alias, Type: { } type })
            {
                sb.Append($$"""
                {{(i++ is 0 ? "if" : "else if")}} ((span.Length == {{len}} || span.Length > {{len}} && span[{{len}}] == '=') && span.Slice(0, {{len}}).SequenceEqual("{{alias}}"))
                {

""");
                sb.Append($$"""
                    name = "{{name}}";
                    span = span.Slice({{len}});

""");
                if (type == knownTypes.SystemBoolean)
                {
                    sb.Append($$"""
                                    goto ReadAsBoolean;

                """);
                }
                else if (type == knownTypes.SystemTimeSpan)
                {
                    sb.Append($$"""
                                    goto ReadAsTimeSpan;

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
                    name = "PrintHelp";
                    span = span.Slice(4);
                    goto ReadAsBoolean;
                }
                else if ((span.Length == 7 || span.Length > 7 && span[7] == '=') && span.Slice(0, 7).SequenceEqual("version"))
                {
                    name = "PrintVersion";
                    span = span.Slice(7);
                    goto ReadAsBoolean; 
                }

""");
        }

        sb.Append("""
                else
                {
                    builder.Add(token);
                    continue;
                }

                if (span.StartsWith("="))
                {
                    options[name] = new string(span.Slice(1));
                    continue;
                }
                else
                {
                    goto TryReadNext;
                }

                ReadAsBoolean:
                if (span.StartsWith("="))
                {
                    if (TryParseBoolean(span.Slice(1), out var value))
                    {
                        options[name] = value ? "True" : "False";
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

                ReadAsTimeSpan:
                if (span.StartsWith("="))
                {
                    if (int.TryParse(span.Slice(1), out var ms))
                    {
                        options[name] = global::System.TimeSpan.FromMilliseconds(ms).ToString();
                        continue;
                    }
                    else if (global::System.TimeSpan.TryParse(span.Slice(1), out var value))
                    {
                        options[name] = value.ToString();
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
            if (option is { Name: { } name, ShortAlias: not '\0' and var alias, Type: { } type })
            {
                sb.Append($$"""
                        case '{{alias}}':

""");
                sb.Append($$"""
                            name = "{{name}}";

""");
                if (type == knownTypes.SystemBoolean)
                {
                    sb.Append($$"""
                            goto ReadAsBooleanShort;

""");
                }
                else if (type == knownTypes.SystemTimeSpan)
                {
                    sb.Append($$"""
                            goto ReadAsTimeSpanShort;

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
                            name = "PrintHelp";
                            goto ReadAsBooleanShort;
                        case 'v':
                            name = "PrintVersion";
                            goto ReadAsBooleanShort;

""");
        }

        sb.Append($$"""
                        default: continue;
                    }

                    if (++i < span.Length)
                    {
                        options[name] = new string(span.Slice(i));
                        break;
                    }
                    else
                    {
                        goto TryReadNext;
                    }

                    ReadAsBooleanShort:
                    if (++i < span.Length)
                    {
                        if (TryParseBoolean(span.Slice(i), out var value))
                        {
                            options[name] = value ? "True" : "False";
                            break;
                        }
                        else
                        {
                            options[name] = "True";
                            i--;
                            continue;
                        }
                    }
                    else
                    {
                        goto TryReadNextAsBoolean;
                    }

                    ReadAsTimeSpanShort:
                    if (++i < span.Length)
                    {
                        if (int.TryParse(span.Slice(i), out var ms))
                        {
                            options[name] = global::System.TimeSpan.FromMilliseconds(ms).ToString();
                            break;
                        }
                        else if (global::System.TimeSpan.TryParse(span.Slice(i), out var value))
                        {
                            options[name] = value.ToString();
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
                }
            }
            else
            {
                builder.Add(token);
            }

            continue;

            TryReadNext:
            if (++index < args.Length)
            {
                var value = args[index];
                if (!value.StartsWith('-'))
                {
                    options[name] = value;
                    continue;
                }
            }

            ThrowMissingOptionValue(name);

            TryReadNextAsBoolean:
            if (++index < args.Length)
            {
                var value = args[index];
                if (TryParseBoolean(value, out var bvalue))
                {
                    options[name] = bvalue ? "True" : "False";
                    continue;
                }
                
                index--;
            }

            options[name] = "True";
            continue;

            TryReadNextAsTimeSpan:
            if (++index < args.Length)
            {
                var value = args[index];
                if (int.TryParse(value, out var ms))
                {
                    options[name] = global::System.TimeSpan.FromMilliseconds(ms).ToString();
                    continue;
                }
                else if (global::System.TimeSpan.TryParse(value, out var tvalue))
                {
                    options[name] = tvalue.ToString();
                    continue;
                }
                else
                {
                    ThrowInvalidOptionValue(name);
                }
                
                index--;
            }

            ThrowMissingOptionValue(name);
        }

        return (options, builder.ToImmutable());
    }

    static bool TryParseBoolean(ReadOnlySpan<char> span, out bool value)
    {
        if (span.Length == 1)
        {
            if (span[0] == '1')
            {
                value = true;
                return true;
            }
            else if(span[0] == '0')
            {
                value = false;
                return true;
            }
            
            value = false;
            return false;
        }

        return bool.TryParse(span, out value);
    }

    [global::System.Diagnostics.CodeAnalysis.DoesNotReturn]
    static void ThrowMissingOptionValue(string optionName)
    {
        throw new InvalidOperationException($"Missing value for '{optionName}' option.");
    }

    [global::System.Diagnostics.CodeAnalysis.DoesNotReturn]
    static void ThrowInvalidOptionValue(string optionName)
    {
        throw new InvalidOperationException($"Invalid value for '{optionName}' option.");
    }
""");
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
                span[0] = ("-h, -?, --help", "Print this help");
                span[1] = ("--version", "Print application's version information");
                span = span.Slice(2);
            }

            for (var index = 0; index < options.Length; index++)
            {
                var (name, longAlias, shortAlias, type, description, hint) = options[index];
                var shortAliasPart = shortAlias is not '\0' ? "-" + shortAlias + ", " : null;
                var hintPart = type != knownTypes.SystemBoolean ? $" <{hint ?? name.ToLowerInvariant()}>" : null;
                var alias = @$"{shortAliasPart}--{longAlias}{hintPart}";
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

        sb.Append("""

}
""");

        return sb.ToString();
    }
}