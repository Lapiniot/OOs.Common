namespace System.Collections.Generic;

public sealed class EqualityComparerAdapter<T>(Func<T, T, bool> equals) : EqualityComparer<T>
{
    public override bool Equals(T x, T y) => equals(x, y);

    public override int GetHashCode(T obj) => obj.GetHashCode();
}