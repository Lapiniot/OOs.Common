namespace System
{
    public interface IIdentityPool<T>
    {
        T Rent();
        void Return(in T identity);
    }
}