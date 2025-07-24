using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace OOs.CommandLine.Generators;

[Generator]
public class ProductInfoGenerator : IIncrementalGenerator
{
    private static readonly Dictionary<string, string> mappings = new()
    {
        { "AssemblyCompanyAttribute", "Company" },
        { "AssemblyCopyrightAttribute", "Copyright" },
        { "AssemblyDescriptionAttribute", "Description" },
        { "AssemblyFileVersionAttribute", "FileVersion" },
        { "AssemblyInformationalVersionAttribute", "InformationalVersion" },
        { "AssemblyProductAttribute", "Product" },
        { "AssemblyTitleAttribute", "Title" },
        { "AssemblyVersionAttribute", "Version" },
        { "AssemblyTrademarkAttribute", "Trademark" }
    };

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var sourceDataProvider = context.SyntaxProvider.ForAttributeWithMetadataName(
                fullyQualifiedMetadataName: "OOs.CommandLine.Generators.GenerateProductInfoAttribute",
                predicate: static (_, _) => true,
                transform: GetSourceGenerationContext)
            .WithComparer(ImmutableArrayStructuralComparer<(string, string?)>.Default)
            .WithTrackingName($"{nameof(ProductInfoGenerator)}SourceData");

        context.RegisterSourceOutput(sourceDataProvider, (ctx, source) =>
            ctx.AddSource("ProductInfo.g.cs", SourceText.From(Emit(source), encoding: Encoding.UTF8)));

        context.RegisterPostInitializationOutput(static ctx =>
        {
            var sb = new StringBuilder();
            CodeEmitHelper.AppendFileHeader(sb);
            sb.Append("""

global using global::OOs.CommandLine.Generators;

namespace OOs.CommandLine.Generators
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
    [Microsoft.CodeAnalysis.EmbeddedAttribute]
    internal sealed class GenerateProductInfoAttribute() : global::System.Attribute
    {
    }
}
""");
            ctx.AddSource("GenerateProductInfoAttribute.g.cs", SourceText.From(sb.ToString(), encoding: Encoding.UTF8));
        });
    }

    private static string Emit(ImmutableArray<(string, string?)> source)
    {
        var sb = new StringBuilder();
        CodeEmitHelper.AppendFileHeader(sb);
        sb.AppendLine();
        CodeEmitHelper.AppendGeneratedCodeAttribute(sb);
        sb.Append($$"""

internal static class ProductInfo
{

""");
        foreach (var (name, value) in source)
        {
            sb.Append($$"""
    public const string {{name}} = "{{value}}";

""");
        }

        sb.Append('}');
        return sb.ToString();
    }

    private ImmutableArray<(string, string?)> GetSourceGenerationContext(GeneratorAttributeSyntaxContext context, CancellationToken token)
    {
        var builder = ImmutableArray.CreateBuilder<(string, string?)>();
        foreach (var data in context.TargetSymbol.GetAttributes())
        {
            if (data is
                {
                    AttributeClass:
                    {
                        Name: { } name,
                        ContainingNamespace:
                        {
                            Name: "Reflection",
                            ContainingNamespace:
                            {
                                Name: "System",
                                ContainingNamespace.IsGlobalNamespace: true
                            }
                        }
                    },
                    ConstructorArguments: [{ Type.SpecialType: SpecialType.System_String, Value: string value }]
                } && mappings.TryGetValue(name, out var propName))
            {
                builder.Add((propName, value));
            }
        }

        return builder.ToImmutable();
    }
}