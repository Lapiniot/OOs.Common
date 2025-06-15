using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace OOs.Common.CommandLine.Generators;

[Generator]
public class ArgumentParserGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var attributes = context.SyntaxProvider.ForAttributeWithMetadataName<SourceData>("OOs.CommandLine.OptionAttribute`1",
            predicate: (node, _) => node is
                CompilationUnitSyntax { AttributeLists.Count: > 0 } or
                ClassDeclarationSyntax { AttributeLists.Count: > 0 } or
                StructDeclarationSyntax { AttributeLists.Count: > 0 },
            transform: (ctx, cancellationToken) =>
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
                                TypeArguments: [{ SpecialType: { } type }]
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

                        builder.Add(new(name, longAlias, shortAlias, type, description, hint));
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
                        generateSynopsis = args.FirstOrDefault(x =>
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
                    ? new(typeName, cns.ToDisplayString(), kind, accessibility, arguments, generateSynopsis)
                    : new("ArgumentParser", "OOs.CommandLine.Generated",
                        Kind: TypeKind.Class, Accessibility: Accessibility.Public, arguments, generateSynopsis);

            }).WithComparer(EqualityComparer<SourceData>.Default);

        context.RegisterSourceOutput(attributes, static (ctx, source) =>
        {
            if (source.Options.Length is 0)
            {
                return;
            }

            var code = ArgumentParserCodeEmitter.Emit(source.Namespace, source.Name, source.Kind,
                source.Accessibility, source.Options, source.GenerateSynopsis);
            ctx.AddSource($"{source.Namespace}.{source.Name}.g.cs", SourceText.From(code, Encoding.UTF8));
        });
    }
}