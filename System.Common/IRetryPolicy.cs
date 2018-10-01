﻿using System.Threading;
using System.Threading.Tasks;

namespace System
{
    public interface IRetryPolicy
    {
        Task RetryAsync(Func<CancellationToken, Task> asyncFunc, CancellationToken cancellationToken = default);
        Task<T> RetryAsync<T>(Func<CancellationToken, Task<T>> asyncFunc, CancellationToken cancellationToken = default);
    }
}