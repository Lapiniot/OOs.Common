namespace System.Net
{
    public interface IConnectionListener : IObservable<INetworkTransport>, IDisposable
    {
        void Start();
        void Stop();
    }
}