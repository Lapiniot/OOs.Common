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

public readonly record struct TypeGenerationContext(string? Name, string? Namespace, TypeKind Kind,
    Accessibility Accessibility, TypeGenerationOptions Options);

public readonly record struct TypeGenerationOptions(bool GenerateSynopsis,
    bool AddStandardOptions, UnknownOptionBehavior UnknownOptionBehavior);

public readonly record struct OptionGenerationContext(string Name, string Alias, char ShortAlias,
    OptionTypeContext TypeContext, string? Description, string? Hint);

public readonly record struct OptionTypeContext(WellKnownType KnownType, ImmutableArray<string> AllowedValues) :
    IEquatable<OptionTypeContext>
{
    public bool Equals(OptionTypeContext other) =>
        EqualityComparer<WellKnownType>.Default.Equals(KnownType, other.KnownType) &&
        ImmutableArrayStructuralComparer<string>.Default.Equals(AllowedValues, other.AllowedValues);

    public override int GetHashCode() => HashCode.Combine(
        KnownType.GetHashCode(),
        ImmutableArrayStructuralComparer<string>.Default.GetHashCode(AllowedValues));
}

public enum WellKnownType
{
    None,
    Boolean,
    TimeSpan,
    Enum
}

public enum UnknownOptionBehavior
{
    Allow,
    Ignore,
    Preserve,
    Prohibit
}