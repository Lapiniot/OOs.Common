using System.Collections.Generic;
using System.Net.Connections;

namespace System.Net.Listeners
{
    public interface IConnectionListener : IAsyncEnumerable<INetworkConnection> {}
}