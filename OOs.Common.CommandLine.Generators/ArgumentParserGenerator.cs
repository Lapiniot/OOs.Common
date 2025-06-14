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
                            ConstructorArguments: [{ Value: string name }, { Value: string longAlias }, ..] cargs,
                            NamedArguments: var nargs,
                            AttributeClass:
                            {
                                IsGenericType: true,
                                TypeArguments: [{ SpecialType: { } type }]
                            }
                        })
                    {
                        if (cargs is [_, _, { Value: char shortAlias }])
                            builder.Add(new(name, longAlias, shortAlias, type));
                        else if (nargs is [{ Key: "ShortAlias", Value.Value: char shortAlias1 }, ..])
                            builder.Add(new(name, longAlias, shortAlias1, type));
                        else if (nargs is [_, { Key: "ShortAlias", Value.Value: char shortAlias2 }, ..])
                            builder.Add(new(name, longAlias, shortAlias2, type));
                        else
                            builder.Add(new(name, longAlias, '\0', type));
                    }
                }

                var arguments = builder.ToImmutable();

                return ctx.TargetSymbol is ITypeSymbol
                {
                    Name: var typeName,
                    ContainingNamespace: var cns,
                    DeclaredAccessibility: var accessibility,
                    TypeKind: var kind,
                }
                    ? new(typeName, cns.ToDisplayString(), kind, accessibility, arguments)
                    : new("ArgumentParser", "OOs.CommandLine.Generated",
                        Kind: TypeKind.Class, Accessibility: Accessibility.Public, arguments);

            }).WithComparer(EqualityComparer<SourceData>.Default);

        context.RegisterSourceOutput(attributes, static (ctx, source) =>
        {
            if (source.Options.Length is 0)
            {
                return;
            }

            var code = ArgumentParserCodeEmitter.Emit(source.Namespace, source.Name, source.Kind, source.Accessibility, source.Options);
            ctx.AddSource($"{source.Namespace}.{source.Name}.g.cs", SourceText.From(code, Encoding.UTF8));
        });
    }
}