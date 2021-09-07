namespace System.Collections.Generic;

public sealed class ComparerAdapter<T> : Comparer<T>
{
    private readonly Func<T, T, int> compare;

    public ComparerAdapter(Func<T, T, int> compare)
    {
        ArgumentNullException.ThrowIfNull(compare);
        this.compare = compare;
    }

    public override int Compare(T x, T y)
    {
        return compare(x, y);
    }
}