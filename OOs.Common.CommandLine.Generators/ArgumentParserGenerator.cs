using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace OOs.Common.CommandLine.Generators;

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
                transform: GetSourceData)
            .WithComparer(EqualityComparer<SourceData>.Default)
            .WithTrackingName($"{nameof(ArgumentParserGenerator)}SourceData");

        var combined = optionProvider.Combine(knownTypesProvider).Combine(sourceDataProvider.Collect());

        context.RegisterSourceOutput(combined, static (ctx, source) =>
        {
            var ((defaults, knownTypes), sources) = source;

            if (!defaults.IsEnabled)
            {
                return;
            }

            foreach (var (name, ns, kind, accessibility, options, genSynopsis) in sources)
            {
                var namespaceName = ns ?? defaults.NamespaceName;
                var typeName = name ?? defaults.ClassName;
                var code = ArgumentParserCodeEmitter.Emit(namespaceName, typeName, kind,
                    accessibility, options, genSynopsis, knownTypes);
                ctx.AddSource($"{namespaceName}.{typeName}.g.cs", SourceText.From(code, Encoding.UTF8));
            }
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

    private static SourceData GetSourceData(GeneratorAttributeSyntaxContext ctx, CancellationToken cancellationToken)
    {
        var builder = ImmutableArray.CreateBuilder<OptionData>();
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

        var arguments = builder.ToImmutable();

        var generateSynopsis = false;
        foreach (var item in ctx.TargetSymbol.GetAttributes())
        {
            if (item is
                {
                    AttributeClass:
                    {
                        Name: "ArgumentParserGenerationOptionsAttribute",
                        ContainingNamespace:
                        {
                            Name: "CommandLine",
                            ContainingNamespace:
                            {
                                Name: "OOs",
                                ContainingNamespace.IsGlobalNamespace: true
                            }
                        }
                    },
                    NamedArguments: var args
                })
            {
                generateSynopsis = args.FirstOrDefault(static x =>
                    x.Key == "GenerateSynopsis" &&
                    x.Value.Type?.SpecialType == SpecialType.System_Boolean).Value.Value is true;
                break;
            }
        }

        return ctx.TargetSymbol is ITypeSymbol
        {
            Name: var typeName,
            ContainingNamespace: var cns,
            DeclaredAccessibility: var accessibility,
            TypeKind: var kind,
        }
            ? new(typeName, cns.ToDisplayString(format), kind, accessibility, arguments, generateSynopsis)
            : new(null, null, TypeKind.Class, Accessibility.Public, arguments, generateSynopsis);
    }
}

public record struct GeneratorOptions(bool IsEnabled, string ClassName, string NamespaceName);