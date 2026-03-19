using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;

#pragma warning disable CA1034 // Nested types should not be visible
#pragma warning disable CA1708 // Identifiers should differ by more than case

namespace OOs;

public static class ThrowHelper
{
    extension(ArgumentOutOfRangeException)
    {
        /// <summary>
        /// Throws <see cref="ArgumentOutOfRangeException"/> if value is not a power of two.
        /// </summary>
        /// <typeparam name="T">The type of the object to validate, derived from <see cref="IBinaryNumber{TSelf}"/>.</typeparam>
        /// <param name="value">The argument to validate as power of two.</param>
        /// <param name="paramName">The name of the parameter with which value corresponds.</param>
        /// <exception cref="ArgumentOutOfRangeException">Throws if value is not a power of two.</exception>
        public static void ThrowIfNotPow2<T>(T value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
            where T : IBinaryNumber<T>
        {
            if (!T.IsPow2(value))
            {
                ArgumentOutOfRangeException.ThrowNotPow2(value, paramName);
            }
        }

        /// <summary>
        /// Unconditionally throws <see cref="ArgumentOutOfRangeException"/> indicating the value is not a power of two.
        /// </summary>
        /// <typeparam name="T">The type of the argument, derived from <see cref="IBinaryNumber{TSelf}"/>.</typeparam>
        /// <param name="value">The argument value.</param>
        /// <param name="paramName">The name of the parameter with which value corresponds.</param>
        /// <exception cref="ArgumentOutOfRangeException" />
        [DoesNotReturn]
        public static void ThrowNotPow2<T>(T value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
            where T : INumberBase<T>
        {
            throw new ArgumentOutOfRangeException(paramName, value, "Value must be a power of two.");
        }
    }

    extension(InvalidOperationException)
    {
        /// <summary>
        /// Unconditionally throws <see cref="InvalidOperationException"/> indicating the object 
        /// is currently not in the valid state for this operation to be invoked.
        /// </summary>
        /// <param name="state">The name of the state object is currently in.</param>
        /// <param name="callerName">The name of the operation currently executing.</param>
        /// <exception cref="InvalidOperationException"></exception>
        [DoesNotReturn]
        public static void ThrowInvalidState(string state, [CallerMemberName] string? callerName = null) =>
            throw new InvalidOperationException($"Cannot call '{callerName}' in the current state: '{state}'.");

        /// <summary>
        /// Unconditionally throws <see cref="InvalidOperationException"/> indicating the object 
        /// is currently not in the valid state for this operation to be invoked.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <exception cref="InvalidOperationException"></exception>
        [DoesNotReturn]
        public static void Throw(string? message = null) =>
            throw new InvalidOperationException(message);
    }
}