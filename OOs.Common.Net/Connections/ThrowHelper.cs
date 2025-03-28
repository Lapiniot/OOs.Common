using System.Diagnostics.CodeAnalysis;
using OOs.Net.Connections.Exceptions;

namespace OOs.Net.Connections;

public static class ThrowHelper
{
    private const string QuicNotSupportedMessage = "'QUIC' protocol is not supported on this system.";

    [DoesNotReturn]
    public static void ThrowConnectionClosed(Exception exception) =>
        throw new ConnectionClosedException(exception);

    [DoesNotReturn]
    public static void ThrowHostNotFound(Exception exception) =>
        throw new HostNotFoundException(exception);

    [DoesNotReturn]
    public static void ThrowServerUnavailable(Exception exception) =>
        throw new ServerUnavailableException(exception);

    [DoesNotReturn]
    public static void ThrowQuicNotSupported() =>
        throw new PlatformNotSupportedException(QuicNotSupportedMessage);

    [DoesNotReturn]
    public static T ThrowQuicNotSupported<T>() =>
        throw new PlatformNotSupportedException(QuicNotSupportedMessage);
}