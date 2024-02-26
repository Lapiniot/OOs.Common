using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace OOs;

public static class ThrowHelper
{
    [DoesNotReturn]
    public static void ThrowMustBePowerOfTwo(string argumentName) =>
        throw new ArgumentException("Must be value power of two.", argumentName);

    [DoesNotReturn]
    public static void ThrowInvalidState([CallerMemberName] string callerName = null) =>
        throw new InvalidOperationException($"Cannot call '{callerName}' in the current state.");

    [DoesNotReturn]
    public static void ThrowInvalidState(string state, [CallerMemberName] string callerName = null) =>
    throw new InvalidOperationException($"Cannot call '{callerName}' in the current state: '{state}'.");

    [DoesNotReturn]
    public static void ThrowInvalidOperation() => throw new InvalidOperationException();

    [DoesNotReturn]
    public static void ThrowSemaphoreFull() => throw new SemaphoreFullException();
}