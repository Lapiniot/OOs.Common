using System.Collections.Generic;

namespace System.Net
{
    public interface IAsyncConnectionListener : IAsyncEnumerable<INetworkTransport>, IDisposable {}
}