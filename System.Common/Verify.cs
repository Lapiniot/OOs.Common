using System.Runtime.CompilerServices;

namespace System;

public static class Verify
{
    public static void ThrowIfNotInRange(int argument, int minVal, int maxVal, [CallerArgumentExpression("argument")] string argumentName = null)
    {
        if (argument < minVal || argument > maxVal)
        {
            throw new ArgumentOutOfRangeException(argumentName, $"Must be number in the range [{minVal} .. {maxVal}]");
        }
    }

    public static void ThrowIfNullOrEmpty(string argument, [CallerArgumentExpression("argument")] string argumentName = null)
    {
        if (string.IsNullOrEmpty(argument))
        {
            throw new ArgumentException("Cannot be null or empty.", argumentName);
        }
    }

    public static void ThrowIfNullOrEmpty(Array argument, [CallerArgumentExpression("argument")] string argumentName = null)
    {
        ArgumentNullException.ThrowIfNull(argument);

        if (argument.Length is 0)
        {
            throw new ArgumentException("Not empty array is expected.", argumentName);
        }
    }

    public static void ThrowIfNullOrEmpty<T>(IReadOnlyCollection<T> argument, [CallerArgumentExpression("argument")] string argumentName = null)
    {
        ArgumentNullException.ThrowIfNull(argument);

        if (argument.Count is 0)
        {
            throw new ArgumentException("Not empty collection is expected.", argumentName);
        }
    }

    public static void ThrowIfNotPowerOfTwo(int argument, [CallerArgumentExpression("argument")] string argumentName = null)
    {
        if ((argument & (argument - 1)) != 0)
        {
            throw new ArgumentException("Must be value power of two.", argumentName);
        }
    }

    public static void ThrowIfLessThan(int argument, int minVal, [CallerArgumentExpression("argument")] string argumentName = null)
    {
        if (argument < minVal)
        {
            throw new ArgumentOutOfRangeException(argumentName, $"Value must be greater than {minVal}");
        }
    }
}