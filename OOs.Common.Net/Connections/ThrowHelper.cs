using System.Diagnostics.CodeAnalysis;
using OOs.Net.Connections.Exceptions;

namespace OOs.Net.Connections;

internal static class ThrowHelper
{
    [DoesNotReturn]
    public static void ThrowConnectionClosed(Exception exception) =>
            throw new ConnectionClosedException(exception);

    [DoesNotReturn]
    public static void ThrowHostNotFound(Exception exception) =>
        throw new HostNotFoundException(exception);

    [DoesNotReturn]
    public static void ThrowServerUnavailable(Exception exception) =>
        throw new ServerUnavailableException(exception);
}