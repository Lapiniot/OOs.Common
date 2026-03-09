using System.Collections.Immutable;
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

            foreach (var ((name, ns, kind, accessibility, genOptions), options) in sources)
            {
                var namespaceName = ns ?? defaults.NamespaceName;
                var typeName = name ?? defaults.ClassName;
                var code = ArgumentParserCodeEmitter.Emit(namespaceName, typeName, kind,
                    accessibility, options, genOptions, knownTypes);
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
    /// <summary>
    /// Specifies how unknown command-line options should be handled during argument parsing.
    /// </summary>
    /// <remarks>
    /// This enumeration defines the behavior when the argument parser encounters an unrecognized option.
    /// </remarks>
    
""");
            CodeEmitHelper.AppendGeneratedCodeAttribute(sb);
            sb.Append("""

    internal enum UnknownOptionBehavior
    {
        /// <summary>Allow unknown options to be parsed without error as if they were previously defined (string typed).</summary>
        Allow,
        /// <summary>Ignore unknown options, skipping them without error.</summary>
        Ignore,
        /// <summary>Skip parsing as option, but preserve and treat as regular argument value.</summary>
        Preserve,
        /// <summary>Prohibit unknown options completely, causing an error.</summary>
        Prohibit
    }


    /// <summary>
    /// Specifies options for code generation of command-line argument parsers.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Use this attribute on the target class or struct that represents your command-line arguments, 
    /// or on the assembly to set project-wide defaults.
    /// </para>
    /// </remarks>
    
""");
            CodeEmitHelper.AppendGeneratedCodeAttribute(sb);
            sb.Append("""
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false)]
    [Microsoft.CodeAnalysis.EmbeddedAttribute]
    internal sealed class ArgumentParserGenerationOptionsAttribute() : global::System.Attribute
    {
        /// <summary>Gets or sets a value indicating whether to generate a command-line synopsis.</summary>
        public bool GenerateSynopsis { get; set; }

        /// <summary>Gets or sets a value indicating whether to automatically add standard help and version options.</summary>
        public bool AddStandardOptions { get; set; }

        /// <summary>Gets or sets the behavior for handling unknown command-line options.</summary>
        public UnknownOptionBehavior UnknownOptionBehavior { get; set; }
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
                        case { Key: "ShortAlias", Value.Value: char value }: shortAlias = value; break;
                        case { Key: "Description", Value.Value: string value }: description = value; break;
                        case { Key: "Hint", Value.Value: string value }: hint = value; break;
                    }
                }

                builder.Add(new(name, longAlias, shortAlias, typeToken, description, hint));
            }
        }

        var options = builder.ToImmutable();

        var generateSynopsis = false;
        var addStandardOptions = false;
        var unknownOptionBehavior = UnknownOptionBehavior.Allow;

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
                    switch (args[i])
                    {
                        case { Key: "GenerateSynopsis", Value.Value: bool value }:
                            generateSynopsis = value;
                            break;
                        case { Key: "AddStandardOptions", Value.Value: bool value }:
                            addStandardOptions = value;
                            break;
                        case { Key: "UnknownOptionBehavior", Value.Value: int value }:
                            unknownOptionBehavior = (UnknownOptionBehavior)value;
                            break;
                    }
                }
            }
        }

        var typeGenerationOptions = new TypeGenerationOptions(generateSynopsis, addStandardOptions, unknownOptionBehavior);

        return ctx.TargetSymbol is ITypeSymbol
        {
            Name: var typeName,
            ContainingNamespace: var cns,
            DeclaredAccessibility: var accessibility,
            TypeKind: var kind,
        }
            ? new(new(typeName, cns.ToDisplayString(format), kind, accessibility, typeGenerationOptions), options)
            : new(new(null, null, TypeKind.Class, Accessibility.Public, typeGenerationOptions), options);
    }
}

public record struct GeneratorOptions(bool IsEnabled, string ClassName, string NamespaceName);