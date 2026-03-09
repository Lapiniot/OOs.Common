using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace OOs.CommandLine.Generators;

public readonly record struct SourceGenerationContext(TypeGenerationContext Context,
    ImmutableArray<OptionGenerationContext> Options) :
    IEquatable<SourceGenerationContext>
{
    public bool Equals(SourceGenerationContext other)
    {
        return EqualityComparer<TypeGenerationContext>.Default.Equals(Context, other.Context) &&
            ImmutableArrayStructuralComparer<OptionGenerationContext>.Default.Equals(Options, other.Options);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(
            EqualityComparer<TypeGenerationContext>.Default.GetHashCode(Context),
            ImmutableArrayStructuralComparer<OptionGenerationContext>.Default.GetHashCode(Options));
    }
}

public enum UnknownOptionBehavior
{
    Allow,
    Ignore,
    Preserve,
    Prohibit
}

public readonly record struct TypeGenerationContext(string? Name, string? Namespace, TypeKind Kind,
    Accessibility Accessibility, TypeGenerationOptions Options);

public readonly record struct TypeGenerationOptions(bool GenerateSynopsis,
    bool AddStandardOptions, UnknownOptionBehavior UnknownOptionBehavior);

public readonly record struct OptionGenerationContext(string Name, string Alias, char ShortAlias,
    int Type, string? Description, string? Hint);