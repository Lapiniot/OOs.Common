using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace OOs.Common.CommandLine.Generators;

public record struct SourceData(string? Name, string? Namespace, TypeKind Kind,
    Accessibility Accessibility, ImmutableArray<OptionData> Options, bool GenerateSynopsis) :
    IEquatable<SourceData>
{
#pragma warning disable IDE0251 // Make member 'readonly'
    public bool Equals(SourceData other)
#pragma warning restore IDE0251 // Make member 'readonly'
    {
        return EqualityComparer<string?>.Default.Equals(Name, other.Name) &&
            EqualityComparer<string?>.Default.Equals(Namespace, other.Namespace) &&
            EqualityComparer<TypeKind>.Default.Equals(Kind, other.Kind) &&
            EqualityComparer<Accessibility>.Default.Equals(Accessibility, other.Accessibility) &&
            EqualityComparer<bool>.Default.Equals(GenerateSynopsis, other.GenerateSynopsis) &&
            ImmutableArrayStructuralComparer<OptionData>.Instance.Equals(Options, other.Options);
    }

    public override readonly int GetHashCode()
    {
        return HashCode.Combine(
            EqualityComparer<string?>.Default.GetHashCode(Name),
            EqualityComparer<string?>.Default.GetHashCode(Namespace),
            EqualityComparer<TypeKind>.Default.GetHashCode(Kind),
            EqualityComparer<Accessibility>.Default.GetHashCode(Accessibility),
            EqualityComparer<bool>.Default.GetHashCode(GenerateSynopsis),
            ImmutableArrayStructuralComparer<OptionData>.Instance.GetHashCode(Options));
    }
}

public record struct OptionData(string Name, string Alias, char ShortAlias, SpecialType Type, string? Description, string? Hint);