using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System;

public static class Verify
{
    public static void ThrowIfNullOrEmpty(string argument, [CallerArgumentExpression("argument")] string argumentName = null)
    {
        if (string.IsNullOrEmpty(argument))
        {
            ThrowStringCannotBeNullOrEmpty(argumentName);
        }
    }

    public static void ThrowIfEmpty(ReadOnlyMemory<byte> argument, [CallerArgumentExpression("argument")] string argumentName = null)
    {
        if (argument.IsEmpty)
        {
            ThrowMemoryCannotBeEmpty(argumentName);
        }
    }

    public static void ThrowIfNullOrEmpty(Array argument, [CallerArgumentExpression("argument")] string argumentName = null)
    {
        ArgumentNullException.ThrowIfNull(argument);

        if (argument.Length is 0)
        {
            ThrowArrayCannotBeEmpty(argumentName);
        }
    }

    public static void ThrowIfNullOrEmpty<T>(IReadOnlyCollection<T> argument, [CallerArgumentExpression("argument")] string argumentName = null)
    {
        ArgumentNullException.ThrowIfNull(argument);

        if (argument.Count is 0)
        {
            ThrowCollectionCannotBeEmpty(argumentName);
        }
    }

    public static void ThrowIfNotPowerOfTwo(int argument, [CallerArgumentExpression("argument")] string argumentName = null)
    {
        if ((argument & (argument - 1)) != 0)
        {
            ThrowMustBePowerOfTwo(argumentName);
        }
    }

    /// <summary>
    /// Throws <see cref="ArgumentOutOfRangeException" /> if <paramref name="argument" /> is less than <paramref name="comparand" />
    /// </summary>
    /// <param name="argument">Argument to check</param>
    /// <param name="comparand">Min. value to compare with</param>
    /// <param name="argumentName">Argument name</param>
    /// <exception cref="ArgumentOutOfRangeException">If <paramref name="argument" /> is less than <paramref name="comparand" /></exception>
    public static void ThrowIfLess(int argument, int comparand, [CallerArgumentExpression("argument")] string argumentName = null)
    {
        if (argument < comparand)
        {
            ThrowValueMustBeGreaterThanOrEqual(argumentName, comparand);
        }
    }

    /// <summary>
    /// Throws <see cref="ArgumentOutOfRangeException" /> if <paramref name="argument" /> is less then or equal to <paramref name="comparand" />
    /// </summary>
    /// <param name="argument">Argument to check</param>
    /// <param name="comparand">Min. value to compare with</param>
    /// <param name="argumentName">Argument name</param>
    /// <exception cref="ArgumentOutOfRangeException">If <paramref name="argument" /> is less than or equal <paramref name="comparand" /></exception>
    public static void ThrowIfLessOrEqual(int argument, int comparand, [CallerArgumentExpression("argument")] string argumentName = null)
    {
        if (argument <= comparand)
        {
            ThrowValueMustBeGreaterThan(argumentName, comparand);
        }
    }

    /// <summary>
    /// Throws <see cref="ArgumentOutOfRangeException" /> if <paramref name="argument" /> is not in range [<paramref name="minVal" /> .. <paramref name="maxVal" />]
    /// </summary>
    /// <param name="argument">Argument to check</param>
    /// <param name="minVal">Min. value to compare with</param>
    /// <param name="maxVal">Max. value to compare with</param>
    /// <param name="argumentName">Argument name</param>
    public static void ThrowIfNotInRange(int argument, int minVal, int maxVal, [CallerArgumentExpression("argument")] string argumentName = null)
    {
        if (argument < minVal || argument > maxVal)
        {
            ThrowValueMustBeInRange(argumentName, minVal, maxVal);
        }
    }

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
    public static void ThrowValueMustBeGreaterThanOrEqual(string argumentName, int comparand) =>
        throw new ArgumentOutOfRangeException(argumentName, $"Value must be greater than or equal to {comparand}.");

    [DoesNotReturn]
    public static void ThrowValueMustBeInRange(string argumentName, int minComparand, int maxComparand) =>
        throw new ArgumentOutOfRangeException(argumentName, $"Value must be in range [{minComparand} .. {maxComparand}].");
}