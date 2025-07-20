using System.Collections.Immutable;

namespace OOs.CommandLine.Generators;

internal sealed class ImmutableArrayStructuralComparer<T> : IEqualityComparer<ImmutableArray<T>>
{
    public static readonly ImmutableArrayStructuralComparer<T> Default = new();

    public bool Equals(ImmutableArray<T> x, ImmutableArray<T> y) => x.SequenceEqual(y, EqualityComparer<T>.Default);

    public int GetHashCode(ImmutableArray<T> obj)
    {
        HashCode hashCode = default;
        for (var index = 0; index < obj.Length; index++)
        {
            hashCode.Add(obj[index]);
        }

        return hashCode.ToHashCode();
    }
}