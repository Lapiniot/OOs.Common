using System.Collections.Immutable;

namespace OOs.Common.CommandLine.Generators;

internal sealed class ImmutableArrayStructuralComparer<T> : IEqualityComparer<ImmutableArray<T>>
{
    public static readonly ImmutableArrayStructuralComparer<T> Instance = new();

    public bool Equals(ImmutableArray<T> x, ImmutableArray<T> y) => x.SequenceEqual(y, EqualityComparer<T>.Default);

    public int GetHashCode(ImmutableArray<T> obj)
    {
        var hash = 0;
        foreach (var x in obj)
        {
            hash = HashCode.Combine(hash, EqualityComparer<T>.Default.GetHashCode(x));
        }

        return hash;
    }
}