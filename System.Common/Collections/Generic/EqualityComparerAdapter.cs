namespace System.Collections.Generic;

public class EqualityComparerAdapter<T> : EqualityComparer<T>
{
    private readonly Func<T, T, bool> equals;

    public EqualityComparerAdapter(Func<T, T, bool> equals)
    {
        this.equals = equals;
    }

    public override bool Equals(T x, T y)
    {
        return equals(x, y);
    }

    public override int GetHashCode(T obj)
    {
        return obj.GetHashCode();
    }
}