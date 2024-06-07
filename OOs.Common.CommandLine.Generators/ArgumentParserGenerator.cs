using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace OOs.Common.CommandLine.Generators;

[Generator]
public class ArgumentParserGenerator : IIncrementalGenerator
{
    private static readonly DiagnosticDescriptor GeneralWarning = new("CLAPGEN001",
        "General warning", "{0}", nameof(ArgumentParserGenerator), DiagnosticSeverity.Warning, true);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var attributes = context.SyntaxProvider.ForAttributeWithMetadataName("OOs.CommandLine.ArgumentAttribute",
            predicate: (node, _) => node is CompilationUnitSyntax { AttributeLists.Count: > 0 },
            transform: (ctx, _) =>
            {
                var builder = ImmutableArray.CreateBuilder<ArgumentData>();
                foreach (var a in ctx.Attributes)
                {
                    if (a.ConstructorArguments is [{ Value: string name }, { Value: INamedTypeSymbol { SpecialType: var type } }, ..])
                    {
                        if (a.ConstructorArguments is [_, _, { Value: string sname }])
                            builder.Add(new ArgumentData(name, sname, type));
                        else
                            builder.Add(new ArgumentData(name, null, type));
                    }
                }

                return builder.ToImmutable();
            }).WithComparer(ImmutableArrayStructuralComparer<ArgumentData>.Instance);

        var assemblyName = context.CompilationProvider.Select((p, _) => p.AssemblyName);

        context.RegisterSourceOutput(attributes.Combine(assemblyName), static (ctx, source) =>
        {
            var (attributes, assemblyName) = source;
            if (attributes.Length is 0) return;

            // foreach (var item in attributes)
            // {
            //     ctx.ReportDiagnostic(Diagnostic.Create(GeneralWarning, null, [$"AssemblyName: {item}"]));
            // }

            var code = ArgumentParserCodeEmitter.Emit(assemblyName!, "ArgumentParser", attributes);
            ctx.AddSource("ArgumentParser.g.cs", SourceText.From(code, Encoding.UTF8));
        });
    }
}

public record struct ArgumentData(string Name, string? ShortName, SpecialType Type);