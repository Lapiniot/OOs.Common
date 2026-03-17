using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace OOs.Threading;

internal static class ThrowHelper
{
    [DoesNotReturn]
    public static void ThrowInvalidState(string state, [CallerMemberName] string? callerName = null) =>
        throw new InvalidOperationException($"Cannot call '{callerName}' in the current state: '{state}'.");

    [DoesNotReturn]
    public static void ThrowSemaphoreFull() => throw new SemaphoreFullException();
}