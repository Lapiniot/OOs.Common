﻿using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace OOs.CommandLine.Generators;

[Generator]
public class ArgumentParserGenerator : IIncrementalGenerator
{
    private const string Prefix = "build_property.";
    private const string EnableOptionName = "EnableArgumentParserGenerator";
    private const string ClassNameOptionName = "ArgumentParserGeneratorClassName";
    private const string NamespaceOptionName = "ArgumentParserGeneratorNamespace";
    private const string RootNamespaceName = "RootNamespace";
    private const string DefaultClassName = "Arguments";
    private const string DefaultNamespaceName = "OOs.CommandLine.Generated";
    private static readonly SymbolDisplayFormat? format = SymbolDisplayFormat.FullyQualifiedFormat.
        WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var optionProvider = context.AnalyzerConfigOptionsProvider.Select(GetGeneratorOptions);

        var knownTypesProvider = context.CompilationProvider.Select(
            static (compilation, _) => KnownTypes.FromCompilation(compilation));

        var sourceDataProvider = context.SyntaxProvider.ForAttributeWithMetadataName(
                fullyQualifiedMetadataName: "OOs.CommandLine.OptionAttribute`1",
                predicate: static (_, _) => true,
                transform: GetSourceGenerationContext)
            .WithComparer(EqualityComparer<SourceGenerationContext>.Default)
            .WithTrackingName($"{nameof(ArgumentParserGenerator)}SourceData");

        var combined = optionProvider.Combine(knownTypesProvider).Combine(sourceDataProvider.Collect());

        context.RegisterSourceOutput(combined, static (ctx, source) =>
        {
            var ((defaults, knownTypes), sources) = source;

            if (!defaults.IsEnabled)
            {
                return;
            }

            foreach (var ((name, ns, kind, accessibility, genSynopsis, addStandardOptions), options) in sources)
            {
                var namespaceName = ns ?? defaults.NamespaceName;
                var typeName = name ?? defaults.ClassName;
                var code = ArgumentParserCodeEmitter.Emit(namespaceName, typeName, kind,
                    accessibility, options, genSynopsis, addStandardOptions, knownTypes);
                var hintName = $"{(!string.IsNullOrWhiteSpace(namespaceName)
                    ? $"{namespaceName}.{typeName}"
                    : typeName)}.g.cs";
                ctx.AddSource(hintName, SourceText.From(code, Encoding.UTF8));
            }
        });

        context.RegisterPostInitializationOutput(ctx =>
        {
            var sb = new StringBuilder();
            CodeEmitHelper.AppendFileHeader(sb);
            sb.Append("""

namespace Microsoft.CodeAnalysis
{
    internal sealed partial class EmbeddedAttribute : global::System.Attribute
    {
    }
}
""");
            ctx.AddSource("Microsoft.CodeAnalysis.EmbeddedAttribute", SourceText.From(sb.ToString(), encoding: Encoding.UTF8));

            sb.Clear();

            CodeEmitHelper.AppendFileHeader(sb);
            sb.Append("""

global using global::OOs.CommandLine.Generators;

namespace OOs.CommandLine.Generators
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Assembly, AllowMultiple = false)]
    [Microsoft.CodeAnalysis.EmbeddedAttribute]
    internal sealed class ArgumentParserGenerationOptionsAttribute() : global::System.Attribute
    {
        public bool GenerateSynopsis { get; set; }
        public bool AddStandardOptions { get; set; }
    }
}
""");
            ctx.AddSource("ArgumentParserGenerationOptionsAttribute.g.cs", SourceText.From(sb.ToString(), encoding: Encoding.UTF8));
        });
    }

    private static GeneratorOptions GetGeneratorOptions(AnalyzerConfigOptionsProvider options, CancellationToken _)
    {
        var globalOptions = options.GlobalOptions;
        return new GeneratorOptions(
            IsEnabled: !globalOptions.TryGetValue($"{Prefix}{EnableOptionName}", out var value) ||
                bool.TryParse(value, out var enabled) && enabled,
            ClassName: globalOptions.TryGetValue($"{Prefix}{ClassNameOptionName}", out value) && !string.IsNullOrEmpty(value)
                ? value
                : DefaultClassName,
            NamespaceName: globalOptions.TryGetValue($"{Prefix}{NamespaceOptionName}", out value) && !string.IsNullOrEmpty(value)
                ? value
                : globalOptions.TryGetValue($"{Prefix}{RootNamespaceName}", out value) && !string.IsNullOrEmpty(value)
                    ? value
                    : DefaultNamespaceName);
    }

    private static SourceGenerationContext GetSourceGenerationContext(GeneratorAttributeSyntaxContext ctx, CancellationToken cancellationToken)
    {
        var builder = ImmutableArray.CreateBuilder<OptionGenerationContext>();
        foreach (var attr in ctx.Attributes)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            if (attr is
                {
                    ConstructorArguments: [{ Value: string name }, { Value: string longAlias }, .. var cargs],
                    NamedArguments: var nargs,
                    AttributeClass:
                    {
                        IsGenericType: true,
                        TypeArguments: [{ MetadataToken: var typeToken }]
                    }
                })
            {
                var shortAlias = cargs is [{ Value: char sa }] ? sa : '\0';
                string? description = null;
                string? hint = null;

                foreach (var item in nargs)
                {
                    switch (item)
                    {
                        case { Key: "ShortAlias", Value.Value: char s }:
                            shortAlias = s; break;
                        case { Key: "Description", Value.Value: string d }:
                            description = d; break;
                        case { Key: "Hint", Value.Value: string h }:
                            hint = h; break;
                    }
                }

                builder.Add(new(name, longAlias, shortAlias, typeToken, description, hint));
            }
        }

        var options = builder.ToImmutable();

        var generateSynopsis = false;
        var addStandardOptions = false;

        foreach (var item in ctx.TargetSymbol.GetAttributes())
        {
            if (item is
                {
                    AttributeClass:
                    {
                        Name: "ArgumentParserGenerationOptionsAttribute",
                        ContainingNamespace:
                        {
                            Name: "Generators",
                            ContainingNamespace:
                            {
                                Name: "CommandLine",
                                ContainingNamespace:
                                {
                                    Name: "OOs",
                                    ContainingNamespace.IsGlobalNamespace: true
                                }
                            }
                        }
                    },
                    NamedArguments: var args
                })
            {
                for (var i = 0; i < args.Length; i++)
                {
                    var arg = args[i];
                    if (arg is { Value: { Type.SpecialType: SpecialType.System_Boolean, Value: bool value }, Key: var key })
                    {
                        switch (key)
                        {
                            case "GenerateSynopsis":
                                generateSynopsis = value;
                                break;
                            case "AddStandardOptions":
                                addStandardOptions = value;
                                break;
                        }
                    }
                }
            }
        }

        return ctx.TargetSymbol is ITypeSymbol
        {
            Name: var typeName,
            ContainingNamespace: var cns,
            DeclaredAccessibility: var accessibility,
            TypeKind: var kind,
        }
            ? new(new(typeName, cns.ToDisplayString(format), kind, accessibility, generateSynopsis, addStandardOptions), options)
            : new(new(null, null, TypeKind.Class, Accessibility.Public, generateSynopsis, addStandardOptions), options);
    }
}

public record struct GeneratorOptions(bool IsEnabled, string ClassName, string NamespaceName);