﻿using System.Collections.Generic;

namespace System.Net
{
    public interface IConnectionListener : IAsyncEnumerable<INetworkTransport>, IAsyncDisposable {}
}