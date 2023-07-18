using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System;

internal static class ThrowHelper
{
    [DoesNotReturn]
    public static void ThrowStringCannotBeNullOrEmpty(string argumentName) =>
            throw new ArgumentException("String cannot be null or empty.", argumentName);

    [DoesNotReturn]
    public static void ThrowMemoryCannotBeEmpty(string argumentName) =>
        throw new ArgumentException("Memory block cannot be empty.", argumentName);

    [DoesNotReturn]
    public static void ThrowArrayCannotBeEmpty(string argumentName) =>
        throw new ArgumentException("Array cannot be empty.", argumentName);

    [DoesNotReturn]
    public static void ThrowCollectionCannotBeEmpty(string argumentName) =>
        throw new ArgumentException("Collection cannot be empty.", argumentName);

    [DoesNotReturn]
    public static void ThrowMustBePowerOfTwo(string argumentName) =>
        throw new ArgumentException("Must be value power of two.", argumentName);

    [DoesNotReturn]
    public static void ThrowValueMustBeGreaterThan(string argumentName, int comparand) =>
        throw new ArgumentOutOfRangeException(argumentName, $"Value must be greater than {comparand}.");

    [DoesNotReturn]
    public static void ThrowValueMustBeGreaterThan(string argumentName, double comparand) =>
        throw new ArgumentOutOfRangeException(argumentName, $"Value must be greater than {comparand}.");

    [DoesNotReturn]
    public static void ThrowValueMustBeGreaterThanOrEqual(string argumentName, int comparand) =>
        throw new ArgumentOutOfRangeException(argumentName, $"Value must be greater than or equal to {comparand}.");

    [DoesNotReturn]
    public static void ThrowValueMustBeInRange(string argumentName, int minComparand, int maxComparand) =>
        throw new ArgumentOutOfRangeException(argumentName, $"Value must be in range [{minComparand} .. {maxComparand}].");

    [DoesNotReturn]
    public static void ThrowInvalidState([CallerMemberName] string callerName = null) =>
        throw new InvalidOperationException($"Cannot call '{callerName}' in the current state.");

    [DoesNotReturn]
    public static void ThrowInvalidOperation() => throw new InvalidOperationException();

    [DoesNotReturn]
    public static void ThrowSemaphoreFull() => throw new SemaphoreFullException();
}