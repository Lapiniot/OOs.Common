namespace System.Collections.Generic
{
    public sealed class ComparerAdapter<T> : Comparer<T>
    {
        private readonly Func<T, T, int> compare;

        public ComparerAdapter(Func<T, T, int> compare)
        {
            this.compare = compare ?? throw new ArgumentNullException($"{nameof(compare)} cannot be null");
        }

        public override int Compare(T x, T y)
        {
            return compare(x, y);
        }
    }
}