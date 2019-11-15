using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Connections
{
    public abstract class NetworkConnection : ActivityObject, INetworkConnection
    {
        public abstract ValueTask<int> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken);

        public abstract ValueTask<int> SendAsync(Memory<byte> buffer, CancellationToken cancellationToken);

        #region Implementation of IConnectedObject

        public bool IsConnected => IsRunning;

        public Task ConnectAsync(CancellationToken cancellationToken = default)
        {
            return StartActivityAsync(cancellationToken);
        }

        public Task DisconnectAsync()
        {
            return StopActivityAsync();
        }

        #endregion
    }
}