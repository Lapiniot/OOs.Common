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
        var attributes = context.SyntaxProvider.ForAttributeWithMetadataName("OOs.CommandLine.OptionAttribute`1",
            predicate: (node, _) => node is CompilationUnitSyntax { AttributeLists.Count: > 0 },
            transform: (ctx, _) =>
            {
                var builder = ImmutableArray.CreateBuilder<ArgumentData>();
                foreach (var a in ctx.Attributes)
                {
                    if (a is
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
                            builder.Add(new ArgumentData(name, longAlias, shortAlias, type));
                        else if (nargs is [{ Key: "ShortAlias", Value.Value: char shortAlias1 }, ..])
                            builder.Add(new ArgumentData(name, longAlias, shortAlias1, type));
                        else if (nargs is [_, { Key: "ShortAlias", Value.Value: char shortAlias2 }, ..])
                            builder.Add(new ArgumentData(name, longAlias, shortAlias2, type));
                        else
                            builder.Add(new ArgumentData(name, longAlias, '\0', type));
                    }
                }

                return builder.ToImmutable();
            }).WithComparer(ImmutableArrayStructuralComparer<ArgumentData>.Instance);

        var assemblyName = context.CompilationProvider.Select((p, _) => p.AssemblyName);

        context.RegisterSourceOutput(attributes.Combine(assemblyName), static (ctx, source) =>
        {
            var (attributes, assemblyName) = source;
            if (attributes.Length is 0) return;
            var code = ArgumentParserCodeEmitter.Emit(assemblyName!, "ArgumentParser", attributes);
            ctx.AddSource("ArgumentParser.g.cs", SourceText.From(code, Encoding.UTF8));
        });
    }
}

public record struct ArgumentData(string Name, string Alias, char ShortAlias, SpecialType Type);